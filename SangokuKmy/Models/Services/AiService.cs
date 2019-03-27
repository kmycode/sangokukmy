﻿using SangokuKmy.Common;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Updates;
using SangokuKmy.Streamings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Services
{
  public static class AiService
  {
    private static async Task<Optional<Town>> CreateTownAsync(MainRepository repo, IEnumerable<uint> avoidCountries)
    {
      var towns = await repo.Town.GetAllAsync();
      var borderTowns = towns
        .Where(t => !avoidCountries.Contains(t.CountryId))
        .Where(t => towns.GetAroundTowns(t).Count() < 8)
        .ToArray();
      if (!borderTowns.Any())
      {
        return default;
      }

      var nearTown = borderTowns[RandomService.Next(0, borderTowns.Length)];
      var townPositions = new Tuple<int, int>[]
      {
        new Tuple<int, int>(nearTown.X - 1, nearTown.Y - 1),
        new Tuple<int, int>(nearTown.X + 0, nearTown.Y - 1),
        new Tuple<int, int>(nearTown.X + 1, nearTown.Y - 1),
        new Tuple<int, int>(nearTown.X - 1, nearTown.Y + 0),
        new Tuple<int, int>(nearTown.X + 1, nearTown.Y + 0),
        new Tuple<int, int>(nearTown.X - 1, nearTown.Y + 1),
        new Tuple<int, int>(nearTown.X + 0, nearTown.Y + 1),
        new Tuple<int, int>(nearTown.X + 1, nearTown.Y + 1),
      }
      .Where(t => t.Item1 >= 0 && t.Item1 < 10 && t.Item2 >= 0 && t.Item2 < 10)
      .Where(t => !towns.Any(tt => tt.X == t.Item1 && tt.Y == t.Item2))
      .ToArray();
      var townPosition = townPositions[RandomService.Next(0, townPositions.Length)];

      var town = ResetService.CreateTown(TownType.Fortress);
      town.X = (short)townPosition.Item1;
      town.Y = (short)townPosition.Item2;
      await repo.Town.AddTownsAsync(new Town[] { town, });
      await repo.SaveChangesAsync();
      return town.ToOptional();
    }

    private static async Task<Country> CreateCountryAsync(MainRepository repo, SystemData system, Town town, params CharacterAiType[] types)
    {
      var country = new Country
      {
        IntEstablished = system.IntGameDateTime - Config.CountryBattleStopDuring,
        CapitalTownId = town.Id,
      };
      await repo.Country.AddAsync(country);
      await repo.SaveChangesAsync();

      town.CountryId = country.Id;

      var charas = new List<Character>();
      foreach (var type in types)
      {
        var chara = new Character
        {
          AiType = type,
          CountryId = country.Id,
          TownId = town.Id,
          LastUpdated = system.CurrentMonthStartDateTime.AddSeconds(RandomService.Next(0, Config.UpdateTime)),
          LastUpdatedGameDate = system.GameDateTime,
        };
        var ai = AiCharacterFactory.Create(chara);
        ai.Initialize(system.GameDateTime);
        await repo.Character.AddAsync(chara);

        charas.Add(chara);
      }

      await repo.SaveChangesAsync();

      foreach (var chara in charas)
      {
        await SetIconAsync(repo, chara);
      }

      return country;
    }

    public static async Task SetIconAsync(MainRepository repo, Character chara)
    {
      var icon = new CharacterIcon
      {
        CharacterId = chara.Id,
        IsAvailable = true,
        IsMain = true,
        Type = CharacterIconType.Default,
        FileName = "0.gif",
      };
      await repo.Character.AddCharacterIconAsync(icon);
    }

    public static async Task CreateWarIfNotWarAsync(MainRepository repo, Func<EventType, string, bool, Task> mapLogAsync)
    {
      var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
      var countries = (await repo.Country.GetAllAsync())
        .Where(c => !c.HasOverthrown)
        .Where(c => c.AiType != CountryAiType.Human)
        .Where(c => !wars.Any(w => w.RequestedCountryId == c.Id || w.InsistedCountryId == c.Id));

      var allTowns = await repo.Town.GetAllAsync();
      foreach (var country in countries)
      {
        var towns = await repo.Town.GetByCountryIdAsync(country.Id);
        foreach (var town in towns)
        {
          if (await CreateWarIfNotWarAsync(repo, country, town, mapLogAsync))
          {
            break;
          }
        }
      }
    }

    public static async Task<bool> CreateWarIfNotWarAsync(MainRepository repo, Country self, Town selfTown, Func<EventType, string, bool, Task> mapLogAsync)
    {
      var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
      if (wars.Any(w => w.RequestedCountryId == self.Id || w.InsistedCountryId == self.Id))
      {
        return false;
      }

      var warCountries = wars
        .Where(w => w.Status != CountryWarStatus.Stoped && w.Status != CountryWarStatus.None)
        .SelectMany(w => new uint[] { w.RequestedCountryId, w.InsistedCountryId, })
        .Distinct()
        .ToArray();
      var notWarCountries = (await repo.Country.GetAllAsync())
        .Select(c => c.Id)
        .Except(warCountries)
        .ToArray();
      if (!notWarCountries.Any())
      {
        return false;
      }

      var allTowns = await repo.Town.GetAllAsync();
      var aroundTowns = allTowns.GetAroundTowns(selfTown).Where(t => notWarCountries.Contains(t.CountryId)).ToArray();
      if (!aroundTowns.Any())
      {
        return false;
      }

      var target = await repo.Country.GetAliveByIdAsync(aroundTowns[RandomService.Next(0, aroundTowns.Count())].CountryId);
      if (!target.HasData)
      {
        return false;
      }

      var startMonth = (await repo.System.GetAsync()).GameDateTime;
      startMonth = new GameDateTime
      {
        Year = (short)(startMonth.Year + 24 - startMonth.Year % 12),  // 翌日または翌々日の２１時
        Month = 1,
      };
      var war = new CountryWar
      {
        RequestedCountryId = self.Id,
        InsistedCountryId = target.Data.Id,
        StartGameDate =  startMonth,
        Status = CountryWarStatus.InReady,
      };
      await repo.CountryDiplomacies.SetWarAsync(war);
      await repo.SaveChangesAsync();

      await mapLogAsync(EventType.WarInReady, $"<country>{self.Name}</country> は、<date>{war.StartGameDate.ToString()}</date> より <country>{target.Data.Name}</country> へ侵攻します", true);

      await StatusStreaming.Default.SendAllAsync(ApiData.From(war));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(war));

      return true;
    }

    public static async Task<bool> CreateWarQuicklyAsync(MainRepository repo, Country self, Country target)
    {
      var startMonth = (await repo.System.GetAsync()).GameDateTime;
      var war = new CountryWar
      {
        RequestedCountryId = self.Id,
        InsistedCountryId = target.Id,
        StartGameDate = startMonth,
        Status = CountryWarStatus.InReady,
      };
      await repo.CountryDiplomacies.SetWarAsync(war);
      await repo.SaveChangesAsync();

      await StatusStreaming.Default.SendAllAsync(ApiData.From(war));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(war));

      return true;
    }

    private static short GetNotUsingCountryColor(IEnumerable<Country> countries)
    {
      var usedCountryColors = countries
        .Where(c => !c.HasOverthrown)
        .Select(c => c.CountryColorId);
      var notUsingCountryColors = Enumerable
        .Range(1, Config.CountryColorMax - 1)
        .Select(n => (short)n)
        .Except(usedCountryColors)
        .ToArray();
      if (!notUsingCountryColors.Any())
      {
        return 0;
      }

      return notUsingCountryColors[RandomService.Next(0, notUsingCountryColors.Length)];
    }

    public static async Task<bool> CreateTerroristCountryAsync(MainRepository repo, Func<EventType, string, bool, Task> mapLogAsync)
    {
      var system = await repo.System.GetAsync();
      var countryColor = GetNotUsingCountryColor(await repo.Country.GetAllAsync());
      if (countryColor == 0)
      {
        return false;
      }

      var charas = new List<CharacterAiType>
      {
        CharacterAiType.TerroristBattler,
        CharacterAiType.TerroristBattler,
        CharacterAiType.TerroristRyofu,
        CharacterAiType.TerroristRyofu,
        CharacterAiType.TerroristPatroller,
        CharacterAiType.TerroristPatroller,
        CharacterAiType.TerroristPatroller,
      };

      var names = new string[] { "南蛮", "烏丸", "羌", "山越", };
      var name = names[RandomService.Next(0, names.Length)];
      if (RandomService.Next(0, 12) == 0)
      {
        name = "倭";
        charas.Add(CharacterAiType.TerroristRyofu);
        charas.Add(CharacterAiType.TerroristBattler);
      }

      var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
      var warCountries = wars
        .Where(w => w.Status != CountryWarStatus.Stoped && w.Status != CountryWarStatus.None)
        .SelectMany(w => new uint[] { w.RequestedCountryId, w.InsistedCountryId, })
        .Distinct();

      var town = await CreateTownAsync(repo, warCountries);
      if (!town.HasData)
      {
        return false;
      }
      town.Data.Name = name;

      var country = await CreateCountryAsync(repo, system, town.Data, charas.ToArray());
      country.CountryColorId = countryColor;
      country.Name = name;
      country.AiType = CountryAiType.Terrorists;

      await mapLogAsync(EventType.AppendTerrorists, $"<town>{town.Data.Name}</town> に異民族が出現し、<country>{country.Name}</country> を建国しました", true);
      await repo.SaveChangesAsync();

      await StatusStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(town.Data)));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(town.Data)));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(country));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(country));

      await CreateWarIfNotWarAsync(repo, country, town.Data, mapLogAsync);

      return true;
    }

    public static async Task<bool> CreateFarmerCountryAsync(MainRepository repo, Town town, Func<EventType, string, bool, Task> mapLogAsync)
    {
      var system = await repo.System.GetAsync();
      var targetCountryOptional = await repo.Country.GetAliveByIdAsync(town.CountryId);

      if (!targetCountryOptional.HasData)
      {
        return false;
      }
      if (await repo.Town.CountByCountryIdAsync(town.CountryId) <= 1)
      {
        return false;
      }
      if ((await repo.Town.GetDefendersAsync(town.Id)).Count > 0)
      {
        return false;
      }

      var targetCountry = targetCountryOptional.Data;
      var countryColor = GetNotUsingCountryColor(await repo.Country.GetAllAsync());
      if (countryColor == 0)
      {
        return false;
      }
      
      var country = await CreateCountryAsync(repo, system, town, CharacterAiType.FarmerBattler, CharacterAiType.FarmerBattler, CharacterAiType.FarmerCivilOfficial);
      country.CountryColorId = countryColor;
      country.Name = $"{town.Name}農民団";
      country.AiType = CountryAiType.Farmers;

      await mapLogAsync(EventType.AppendFarmers, $"<town>{town.Name}</town> の <country>{country.Name}</country> が <country>{targetCountry.Name}</country> に対して蜂起しました", true);
      await repo.SaveChangesAsync();

      await StatusStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(town)));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(town)));
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(town), (await repo.Town.GetCharactersAsync(town.Id)).Select(c => c.Id));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(country));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(country));

      await CreateWarQuicklyAsync(repo, country, targetCountry);

      return true;
    }

    public static async Task<bool> CreateFarmerCountryAsync(MainRepository repo, Func<EventType, string, bool, Task> mapLogAsync)
    {
      var system = await repo.System.GetAsync();

      var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
      var townWars = (await repo.CountryDiplomacies.GetAllTownWarsAsync())
        .Where(t => t.Status != TownWarStatus.InReady && t.Status != TownWarStatus.Available);
      var warCountries = wars
        .Where(w => w.Status != CountryWarStatus.Stoped && w.Status != CountryWarStatus.None)
        .Where(w => w.Status != CountryWarStatus.InReady || w.IntStartGameDate - system.IntGameDateTime > 12)
        .SelectMany(w => new uint[] { w.RequestedCountryId, w.InsistedCountryId, })
        .Distinct();
      var aiCountries = (await repo.Country.GetAllAsync())
        .Where(c => !c.HasOverthrown)
        .Where(c => c.AiType != CountryAiType.Human)
        .Select(c => c.Id);

      var allTowns = await repo.Town.GetAllAsync();
      var singleTownCountries = allTowns
        .GroupBy(t => t.CountryId)
        .Where(c => c.Count() <= 1)
        .Select(c => c.Key);
      var towns = allTowns
        .Where(t => !singleTownCountries.Contains(t.CountryId))
        .Where(t => !aiCountries.Contains(t.CountryId))
        .Where(t => t.CountryId > 0 && t.Security <= 0 && t.People <= 5000)
        .Where(t => !warCountries.Contains(t.CountryId) && !townWars.Select(tt => tt.TownId).Contains(t.Id))
        .ToList();

      // 守備のいる都市は除外
      var removeTowns = new List<Town>();
      foreach (var town in towns)
      {
        if ((await repo.Town.GetDefendersAsync(town.Id)).Count > 0 ||
            (await repo.Town.CountByCountryIdAsync(town.CountryId) <= 1))
        {
          removeTowns.Add(town);
        }
      }
      foreach (var town in removeTowns)
      {
        towns.Remove(town);
      }
      if (!towns.Any())
      {
        return default;
      }

      return await CreateFarmerCountryAsync(repo, towns[RandomService.Next(0, towns.Count)], mapLogAsync);
    }
  }
}
