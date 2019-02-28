using SangokuKmy.Common;
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
    private static Random rand = new Random(DateTime.Now.Millisecond);

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

      var nearTown = borderTowns[rand.Next(0, borderTowns.Length)];
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
      }.Where(t => !towns.Any(tt => tt.X == t.Item1 && tt.Y == t.Item2)).ToArray();
      var townPosition = townPositions[rand.Next(0, townPositions.Length)];

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
          LastUpdated = system.CurrentMonthStartDateTime.AddSeconds(rand.Next(0, Config.UpdateTime)),
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

      return country;
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

      var target = await repo.Country.GetAliveByIdAsync(aroundTowns[rand.Next(0, aroundTowns.Count())].CountryId);
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

    private static short GetNotUsingCountryColor(IEnumerable<Country> countries)
    {
      var usedCountryColors = countries
        .Where(c => !c.HasOverthrown)
        .Select(c => c.CountryColorId);
      var notUsingCountryColors = Enumerable
        .Range(1, Config.CountryColorMax)
        .Select(n => (short)n)
        .Except(usedCountryColors)
        .ToArray();
      if (!notUsingCountryColors.Any())
      {
        return 0;
      }

      return notUsingCountryColors[rand.Next(0, notUsingCountryColors.Length)];
    }

    public static async Task<bool> CreateTerroristCountryAsync(MainRepository repo, Func<EventType, string, bool, Task> mapLogAsync)
    {
      var system = await repo.System.GetAsync();
      var countryColor = GetNotUsingCountryColor(await repo.Country.GetAllAsync());
      if (countryColor == 0)
      {
        return false;
      }

      var names = new string[] { "南蛮", "烏丸", "羌", "山越", };
      var name = names[rand.Next(0, names.Length)];

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
      var country = await CreateCountryAsync(repo, system, town.Data, CharacterAiType.TerroristBattler, CharacterAiType.TerroristWallBattler, CharacterAiType.TerroristCivilOfficial, CharacterAiType.TerroristCivilOfficial, CharacterAiType.TerroristPatroller);
      country.CountryColorId = countryColor;
      country.Name = name;

      await mapLogAsync(EventType.AppendTerrorists, $"<town>{town.Data.Name}</town> に異民族が出現し、<country>{country.Name}</country> を建国しました", true);
      await repo.SaveChangesAsync();

      await StatusStreaming.Default.SendAllAsync(ApiData.From(town.Data));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(town.Data));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(country));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(country));

      await CreateWarIfNotWarAsync(repo, country, town.Data, mapLogAsync);

      return true;
    }
  }
}
