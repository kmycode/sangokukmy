using SangokuKmy.Common;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Updates;
using SangokuKmy.Models.Updates.Ai;
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
      var townPosition = MapService.GetNewTownPosition(towns, t => !avoidCountries.Contains(t.CountryId));
      if (townPosition.X < 0 || townPosition.Y < 0)
      {
        return default;
      }

      var town = MapService.CreateTown(TownType.Fortress);
      town.X = townPosition.X;
      town.Y = townPosition.Y;
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
        PolicyPoint = 10000,
      };
      await repo.Country.AddAsync(country);
      await repo.SaveChangesAsync();

      town.CountryId = country.Id;

      await CreateCharacterAsync(repo, types, country.Id, town.Id, system);

      return country;
    }

    public static async Task<IEnumerable<Character>> CreateCharacterAsync(MainRepository repo, IEnumerable<CharacterAiType> types, uint countryId, uint townId, SystemData system)
    {
      var charas = new List<Character>();
      foreach (var type in types)
      {
        var chara = new Character
        {
          AiType = type,
          CountryId = countryId,
          TownId = townId,
          LastUpdated = system.CurrentMonthStartDateTime.AddSeconds(RandomService.Next(0, Config.UpdateTime)),
          LastUpdatedGameDate = system.GameDateTime,
        };
        var ai = AiCharacterFactory.Create(chara);
        ai.Character.From = CharacterFrom.Ai;
        ai.Initialize(system.GameDateTime);
        await repo.Character.AddAsync(chara);

        charas.Add(chara);
      }

      await repo.SaveChangesAsync();

      foreach (var chara in charas)
      {
        await SetIconAsync(repo, chara);

        if (chara.From == CharacterFrom.Ai)
        {
          await repo.Character.AddSkillAsync(new CharacterSkill
          {
            CharacterId = chara.Id,
            Type = CharacterSkillType.Ai1,
            Status = CharacterSkillStatus.Available,
          });

          var formation = new Formation
          {
            CharacterId = chara.Id,
            Level = 1,
            Experience = 0,
            Type = RandomService.Next(new FormationType[] { FormationType.Choda, FormationType.Engetsu, FormationType.Ganko, FormationType.Gyorin, FormationType.Hoen, FormationType.Hoshi, FormationType.Kakuyoku, FormationType.Kojo, FormationType.Koyaku, FormationType.Suiko, }),
          };
          await repo.Character.AddFormationAsync(formation);
          chara.FormationType = formation.Type;
        }
      }

      return charas;
    }

    public static async Task SetIconAsync(MainRepository repo, Character chara)
    {
      var icon = new CharacterIcon
      {
        CharacterId = chara.Id,
        IsAvailable = true,
        IsMain = true,
        Type = CharacterIconType.Default,
        FileName = RandomService.Next(0, 99) + ".gif",
      };
      await repo.Character.AddCharacterIconAsync(icon);
      chara.MainIcon = icon;
    }

    public static async Task<bool> CreateWarIfNotWarAsync(MainRepository repo, GameDateTime? startDate = null)
    {
      var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
      var countries = (await repo.Country.GetAllAsync())
        .Where(c => !c.HasOverthrown)
        .Where(c => c.AiType != CountryAiType.Human && c.AiType != CountryAiType.Managed && c.AiType != CountryAiType.Terrorists)
        .Where(c => !wars.Any(w => w.RequestedCountryId == c.Id || w.InsistedCountryId == c.Id));

      var isCreated = false;
      var allTowns = await repo.Town.GetAllAsync();
      foreach (var country in countries)
      {
        if (await CreateWarIfNotWarAsync(repo, country, startDate: startDate))
        {
          isCreated = true;
        }
      }

      return isCreated;
    }

    public static async Task<IEnumerable<Country>> GetNotWarAroundCountriesAsync(MainRepository repo, Country self)
    {
      var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
      var allTowns = await repo.Town.GetAllAsync();

      var allCountries = await repo.Country.GetAllAsync();
      var warCountries = wars
        .Where(w => w.Status != CountryWarStatus.Stoped && w.Status != CountryWarStatus.None)
        .Where(w => allTowns.GetAroundTowns(allTowns.Where(t => t.CountryId == w.InsistedCountryId)).Any(t => t.CountryId == w.RequestedCountryId))
        .SelectMany(w => new uint[] { w.RequestedCountryId, w.InsistedCountryId, })
        .Distinct()
        .ToArray();
      var notWarCountries = allCountries
        .Select(c => c.Id)
        .Where(c => c != self.Id)
        .Except(warCountries)
        .ToArray();
      if (!notWarCountries.Any())
      {
        return Enumerable.Empty<Country>();
      }

      var aroundTowns = allTowns
        .Where(t => t.CountryId == self.Id)
        .SelectMany(t => allTowns.GetAroundTowns(t).Where(tt => tt.CountryId != self.Id && notWarCountries.Contains(tt.CountryId)))
        .Distinct()
        .ToArray();
      var aroundCountries = aroundTowns
        .Select(t => t.CountryId)
        .Distinct()
        .Join(allCountries, t => t, c => c.Id, (t, c) => c)
        .ToArray();

      return aroundCountries;
    }

    public static async Task<bool> CreateWarIfNotWarAsync(MainRepository repo, Country self, Country target = null, GameDateTime? startDate = null)
    {
      var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
      if (wars.Any(w => w.RequestedCountryId == self.Id || w.InsistedCountryId == self.Id))
      {
        return false;
      }

      var targets = await GetNotWarAroundCountriesAsync(repo, self);
      if (!targets.Any())
      {
        return false;
      }

      if (target == null)
      {
        var targetOptional = await repo.Country.GetAliveByIdAsync(targets.ElementAt(RandomService.Next(0, targets.Count())).Id);
        if (!targetOptional.HasData)
        {
          return false;
        }
        target = targetOptional.Data;
      }
      else
      {
        if (!targets.Any(t => t.Id == target.Id))
        {
          return false;
        }
      }

      return await CreateWarAsync(repo, self, target, startDate);
    }

    public static GameDateTime GetWarStartDateTime(GameDateTime current, AiCountryWarStartDatePolicy policy = AiCountryWarStartDatePolicy.First21)
    {
      var startMonth = 1;
      var startYear = (int)current.Year;
      var first21 = startYear + 24 - startYear % 12;

      // First21
      if (policy == AiCountryWarStartDatePolicy.First21)
      {
        startYear = first21;
      }
      else if (policy == AiCountryWarStartDatePolicy.FirstBetween19And23)
      {
        if (current.Year + 12 < first21 - 13)
        {
          startYear = first21 - 13;
        }
        else if (current.Year + 12 > first21 - 11)
        {
          startYear = first21 - 1;
        }
        else
        {
          startYear = current.Year + 12;
          startMonth = current.Month;
        }
      }
      else if (policy == AiCountryWarStartDatePolicy.HurryUp)
      {
        startYear = current.Year + 12;
        startMonth = current.Month;
      }

      var date = new GameDateTime
      {
        Year = (short)Math.Max(startYear, Config.UpdateStartYear + Config.CountryBattleStopDuring / 12),
        Month = (short)startMonth,
      };
      return date;
    }

    public static async Task<bool> CreateWarAsync(MainRepository repo, Country self, Country target, GameDateTime? startMonth = null)
    {
      if (startMonth == null)
      {
        var current = (await repo.System.GetAsync()).GameDateTime;
        startMonth = GetWarStartDateTime(current, AiCountryWarStartDatePolicy.First21);
      }

      var war = new CountryWar
      {
        RequestedCountryId = self.Id,
        InsistedCountryId = target.Id,
        StartGameDate = (GameDateTime)startMonth,
        Status = CountryWarStatus.InReady,
      };
      repo.AiCountry.ResetStorategyByCountryId(self.Id);

      await CountryService.SendWarAndSaveAsync(repo, war);

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
      await CountryService.SendWarAndSaveAsync(repo, war);

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

    public static async Task<Country> CreateTerroristCountryAsync(MainRepository repo, Func<EventType, string, bool, Task> mapLogAsync)
    {
      var system = await repo.System.GetAsync();
      var countryColor = GetNotUsingCountryColor(await repo.Country.GetAllAsync());
      if (countryColor == 0)
      {
        return null;
      }

      var size = RandomService.Next(3, 10);
      var charas = new List<CharacterAiType>
      {
        CharacterAiType.TerroristBattler,
        CharacterAiType.TerroristCivilOfficial,
        CharacterAiType.TerroristPatroller,
      };
      if (size >= 4)
      {
        charas.Add(CharacterAiType.TerroristWallBattler);
      }
      if (size >= 5)
      {
        charas.Add(CharacterAiType.TerroristBattler);
      }
      if (size >= 6)
      {
        charas.Add(CharacterAiType.TerroristPatroller);
      }
      if (size >= 7)
      {
        charas.Add(CharacterAiType.TerroristRyofu);
      }
      if (size >= 8)
      {
        charas.Add(CharacterAiType.TerroristCivilOfficial);
      }
      if (size >= 9)
      {
        charas.Add(CharacterAiType.TerroristPatroller);
      }

      var names = new string[] { "南蛮", "烏丸", "羌", "山越", "匈奴", "羯", "鮮卑", "氐", "奚", "夷", "俚", };
      var name = names[RandomService.Next(0, names.Length)];
      if (RandomService.Next(0, 7) == 0)
      {
        name = "倭";
        charas.Add(CharacterAiType.TerroristRyofu);
        charas.Add(CharacterAiType.TerroristBattler);
        charas.Add(CharacterAiType.TerroristPatroller);
      }

      var town = await CreateTownAsync(repo, Enumerable.Empty<uint>());
      if (!town.HasData)
      {
        return null;
      }
      town.Data.Name = name;
      town.Data.TownBuilding = TownBuilding.TerroristHouse;

      var country = await CreateCountryAsync(repo, system, town.Data, charas.ToArray());
      country.CountryColorId = countryColor;
      country.Name = name;
      country.AiType = CountryAiType.Terrorists;

      await repo.Country.AddPolicyAsync(new CountryPolicy
      {
        CountryId = country.Id,
        Type = CountryPolicyType.StoneCastle,
        Status = CountryPolicyStatus.Available,
      });
      await repo.Country.AddPolicyAsync(new CountryPolicy
      {
        CountryId = country.Id,
        Type = CountryPolicyType.Shosha,
        Status = CountryPolicyStatus.Available,
      });

      await mapLogAsync(EventType.AppendTerrorists, $"<town>{town.Data.Name}</town> に異民族が出現し、<country>{country.Name}</country> を建国しました", true);
      await repo.SaveChangesAsync();

      await StatusStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(town.Data)));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(town.Data)));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(new CountryForAnonymous(country)));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new CountryForAnonymous(country)));

      return country;
    }

    public static async Task<bool> CreateThiefCountryAsync(MainRepository repo, Func<EventType, string, bool, Task> mapLogAsync)
    {
      var towns = await repo.Town.GetAllAsync();
      var targetTowns = towns.Where(t => t.CountryId == 0).ToArray();
      if (!targetTowns.Any())
      {
        return false;
      }

      return await CreateThiefCountryAsync(repo, targetTowns[RandomService.Next(0, targetTowns.Length)], mapLogAsync);
    }

    public static async Task<bool> CreateThiefCountryAsync(MainRepository repo, Town town, Func<EventType, string, bool, Task> mapLogAsync)
    {
      if (town.CountryId != 0)
      {
        return false;
      }

      var system = await repo.System.GetAsync();
      var countryColor = GetNotUsingCountryColor(await repo.Country.GetAllAsync());
      if (countryColor == 0)
      {
        return false;
      }

      var countries = await repo.Country.GetAllAsync();
      var charas = new List<CharacterAiType>
      {
        CharacterAiType.ThiefBattler,
        CharacterAiType.ThiefBattler,
        CharacterAiType.ThiefWallBattler,
        CharacterAiType.ThiefPatroller,
      };

      var names = new string[] { "赤眉", "緑林", "黄巣", "侯景", }.Where(n => !countries.Where(c => !c.HasOverthrown && c.AiType == CountryAiType.Thiefs).Any(c => c.Name == n)).ToArray();
      if (!names.Any())
      {
        return false;
      }

      var name = names[RandomService.Next(0, names.Length)];
      if (RandomService.Next(0, 18) == 0 && !countries.Any(c => c.Name == "黄巾" && c.AiType == CountryAiType.Thiefs))
      {
        name = "黄巾";
        charas.Add(CharacterAiType.ThiefBattler);
        charas.Add(CharacterAiType.ThiefWallBattler);
        charas.Add(CharacterAiType.ThiefPatroller);
      }

      var country = await CreateCountryAsync(repo, system, town, charas.ToArray());
      country.CountryColorId = countryColor;
      country.Name = name;
      country.AiType = CountryAiType.Thiefs;

      await mapLogAsync(EventType.AppendThiefs, $"<town>{town.Name}</town> の <country>{country.Name}</country> が蜂起し、独自勢力を築きました", true);
      await repo.SaveChangesAsync();

      await StatusStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(town)));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(town)));
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(town), (await repo.Town.GetCharactersAsync(town.Id)).Select(c => c.Id));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(new CountryForAnonymous(country)));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new CountryForAnonymous(country)));

      return true;
    }

    public static async Task<bool> CreateManagedCountryAsync(MainRepository repo, Func<EventType, string, bool, Task> mapLogAsync)
    {
      var towns = await repo.Town.GetAllAsync();
      var targetTowns = towns.Where(t => t.CountryId == 0).ToArray();
      if (!targetTowns.Any())
      {
        return false;
      }

      return await CreateManagedCountryAsync(repo, targetTowns[RandomService.Next(0, targetTowns.Length)], mapLogAsync);
    }

    public static async Task<bool> CreateManagedCountryAsync(MainRepository repo, Town town, Func<EventType, string, bool, Task> mapLogAsync)
    {
      if (town.CountryId != 0)
      {
        return false;
      }

      var system = await repo.System.GetAsync();
      var countryColor = GetNotUsingCountryColor(await repo.Country.GetAllAsync());
      if (countryColor == 0)
      {
        return false;
      }

      var countries = await repo.Country.GetAllAsync();

      var charas = new List<CharacterAiType>
      {
        CharacterAiType.ManagedBattler,
        CharacterAiType.ManagedBattler,
        CharacterAiType.ManagedBattler,
        CharacterAiType.ManagedCivilOfficial,
        CharacterAiType.ManagedPatroller,
      };
      var names = new string[] { "冉魏", "成漢", "北燕", "翟魏", "代", "張楚", "仲", "後秦", "西秦", "五胡夏",
        "西涼", "南斉", "蕭梁", "西魏", "北周", "後晋", "五代後漢", "後周", "前蜀", "後蜀", "荊南", "閩",
        "北漢", "岐", "五代十国燕", "呉越", "曹", "蔡", "春秋陳", "鄭", "衛", "春秋宋", "魯", "中山", "杞",
        "曾", "邾", "滕", "春秋唐", "栄", "単", "沈", "莱", "英", "六", "庸", "邢", "古蜀", "新末梁", "梁",
        "新", "戦国趙", "前趙（五胡漢）", "後趙", "前涼", "後涼", "韓", "戦国魏", "春秋燕",
        "前燕", "後燕", "越", "春秋呉", "十国呉", "東周", "北涼", "南涼", "劉宋", "東魏", "北斉", "後梁", "後唐",
        "武周", "南唐", "十国楚", "南漢", "成家（公孫述）",
        "秦", "前秦", "晋", "西晋", "東晋", "斉", "金", "明", "隋", "陳", "楚", "春秋楚", "夏", "商", "北魏", "元",
        "魏", "蜀漢", "呉", "漢", "唐", "宋", "清", "西周", };

      var availableNames = names.Where(n => !countries.Any(c => c.Name == n)).ToArray();
      if (!availableNames.Any())
      {
        return false;
      }
      var name = availableNames[RandomService.Next(0, availableNames.Length)];

      var country = await CreateCountryAsync(repo, system, town, charas.ToArray());
      country.CountryColorId = countryColor;
      country.Name = name;
      country.AiType = CountryAiType.Managed;

      await mapLogAsync(EventType.Publish, $"<town>{town.Name}</town> で <country>{country.Name}</country> が建国されました", true);
      await repo.SaveChangesAsync();

      var myCharas = (await repo.Country.GetCharactersWithIconsAndCommandsAsync(country.Id)).Where(c => !c.Character.HasRemoved).ToArray();
      var countNum = new string[] { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸", };
      var count = 0;
      foreach (var chara in myCharas)
      {
        chara.Character.Name = $"{country.Name}_{chara.Character.Name}_{countNum[count]}";
        count++;
      }

      var post = new CountryPost
      {
        CountryId = country.Id,
        CharacterId = myCharas[RandomService.Next(0, myCharas.Length)].Character.Id,
        Type = CountryPostType.Monarch,
      };
      await repo.Country.SetPostAsync(post);

      town.SubType = town.Type;
      MapService.UpdateTownType(town, TownType.Large);
      var policies = new List<CountryPolicy>
      {
        new CountryPolicy
        {
          CountryId = country.Id,
          Type = CountryPolicyType.GunKen,
          Status = CountryPolicyStatus.Available,
        },
        new CountryPolicy
        {
          CountryId = country.Id,
          Type = town.SubType == TownType.Agriculture ? CountryPolicyType.AgricultureCountry :
                 town.SubType == TownType.Commercial ? CountryPolicyType.CommercialCountry : CountryPolicyType.WallCountry,
          Status = CountryPolicyStatus.Available,
        },
      };
      foreach (var policy in policies)
      {
        await repo.Country.AddPolicyAsync(policy);
      }

      var warPolicies = new AiCountryWarPolicy[] { AiCountryWarPolicy.Balance, AiCountryWarPolicy.Carefully, AiCountryWarPolicy.GoodFight, };
      var policyTargets = new AiCountryPolicyTarget[] { AiCountryPolicyTarget.Money, AiCountryPolicyTarget.WallAttack, AiCountryPolicyTarget.WallDefend, };
      var seiranPolicies = new AgainstSeiranPolicy[] { AgainstSeiranPolicy.Gonorrhea, AgainstSeiranPolicy.Mindful, AgainstSeiranPolicy.NotCare, AgainstSeiranPolicy.NotCare, };
      var warStyles = new AiCountryWarStyle[] { AiCountryWarStyle.Aggressive, AiCountryWarStyle.Negative, AiCountryWarStyle.Normal, AiCountryWarStyle.NotCare, };
      var unitPolicies = new AiCountryUnitPolicy[] { AiCountryUnitPolicy.NotCare, };
      var unitGatherPolicies = new AiCountryUnitGatherPolicy[] { AiCountryUnitGatherPolicy.Always, AiCountryUnitGatherPolicy.BeforePeopleChanges, };
      var forceDefendPolicies = new AiCountryForceDefendPolicy[] { AiCountryForceDefendPolicy.NotCare, AiCountryForceDefendPolicy.Negative, AiCountryForceDefendPolicy.Medium, AiCountryForceDefendPolicy.Aggressive, };
      var developStyles = new AiCountryDevelopStyle[] { AiCountryDevelopStyle.BorderTownFirst, AiCountryDevelopStyle.HigherTownFirst, AiCountryDevelopStyle.LowerTownFirst, AiCountryDevelopStyle.NotCare, };
      var warTargetPolicies = new AiCountryWarTargetPolicy[] { AiCountryWarTargetPolicy.EqualityStronger, AiCountryWarTargetPolicy.EqualityWeaker, AiCountryWarTargetPolicy.Random, AiCountryWarTargetPolicy.Weakest, };
      var warStartDatePolicies = new AiCountryWarStartDatePolicy[] { AiCountryWarStartDatePolicy.First21, AiCountryWarStartDatePolicy.FirstBetween19And23, AiCountryWarStartDatePolicy.HurryUp, };
      var townWarPolicies = new AiCountryTownWarPolicy[] { AiCountryTownWarPolicy.ExtraAggressive, AiCountryTownWarPolicy.Aggressive, AiCountryTownWarPolicy.Medium, AiCountryTownWarPolicy.Negative, };
      var management = new AiCountryManagement
      {
        CountryId = country.Id,
        WarPolicy = RandomService.Next(warPolicies),
        PolicyTarget = RandomService.Next(policyTargets),
        SeiranPolicy = RandomService.Next(seiranPolicies),
        WarStyle = RandomService.Next(warStyles),
        UnitPolicy = RandomService.Next(unitPolicies),
        UnitGatherPolicy = RandomService.Next(unitGatherPolicies),
        ForceDefendPolicy = RandomService.Next(forceDefendPolicies),
        DevelopStyle = RandomService.Next(developStyles),
        WarTargetPolicy = RandomService.Next(warTargetPolicies),
        WarStartDatePolicy = RandomService.Next(warStartDatePolicies),
        TownWarPolicy = RandomService.Next(townWarPolicies),
      };
      await repo.AiCountry.AddAsync(management);
      await repo.SaveChangesAsync();

      await StatusStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(town)));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(town)));
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(town), (await repo.Town.GetCharactersAsync(town.Id)).Select(c => c.Id));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(new CountryForAnonymous(country)));
      await StatusStreaming.Default.SendAllAsync(myCharas.Select(c => ApiData.From(new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.Anonymous))));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(post));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new CountryForAnonymous(country)));

      return true;
    }

    public static async Task CheckManagedReinforcementsAsync(MainRepository repo, uint countryId)
    {
      var country = await repo.Country.GetByIdAsync(countryId);
      if (!country.HasData || country.Data.AiType != CountryAiType.Managed)
      {
        return;
      }

      var wars = await repo.CountryDiplomacies.GetAllWarsAsync();

      var allCharacters = await repo.Character.GetAllAliveAsync();
      var warCountries = wars
        .Where(w => w.Status != CountryWarStatus.Stoped && w.Status != CountryWarStatus.None)
        .Where(w => w.IsJoin(countryId))
        .Select(w => w.RequestedCountryId == countryId ? w.InsistedCountryId : w.RequestedCountryId)
        .Distinct()
        .Select(c => new { CountryId = c, CharacterCount = allCharacters.Count(cc => cc.CountryId == c && cc.AiType == CharacterAiType.Human), })
        .ToArray();
      var countryCharacters = allCharacters.Where(c => c.CountryId == countryId && c.AiType.IsManaged());

      if (countryCharacters.Any())
      {
        var requestedReinforcementCount = Math.Max(warCountries.Sum(c => c.CharacterCount) - countryCharacters.Count(c => !c.Name.Contains("援軍")) - 3, 0);
        var currentReinforcements = countryCharacters.Where(c => c.Name.Contains("援軍"));
        if (currentReinforcements.Count() < requestedReinforcementCount)
        {
          // 援軍追加
          var types = new List<CharacterAiType>();
          var count = requestedReinforcementCount - currentReinforcements.Count();
          var battlersCount = countryCharacters.Count(c => c.AiType.ToManagedStandard() == CharacterAiType.ManagedBattler);
          var civilOfficialsCount = countryCharacters.Count(c => c.AiType.ToManagedStandard() == CharacterAiType.ManagedCivilOfficial);
          var patrollersCount = countryCharacters.Count(c => c.AiType.ToManagedStandard() == CharacterAiType.ManagedPatroller);
          for (var i = 0; i < count; i++)
          {
            if (battlersCount + civilOfficialsCount > patrollersCount * 3 + 3)
            {
              types.Add(CharacterAiType.ManagedPatroller);
              patrollersCount++;
            }
            else
            {
              if ((battlersCount + civilOfficialsCount + patrollersCount) % 8 == 0)
              {
                types.Add(CharacterAiType.ManagedWallBattler);
              }
              else
              {
                types.Add(CharacterAiType.ManagedBattler);
              }
              battlersCount++;
            }
          }

          var created = await CreateCharacterAsync(repo, types, countryId, country.Data.CapitalTownId, await repo.System.GetAsync());
          foreach (var chara in created)
          {
            if (chara.AiType == CharacterAiType.ManagedPatroller)
            {
              chara.Popularity = Math.Max(chara.Popularity, countryCharacters.Max(c => c.Popularity));
            }
            else
            {
              chara.Strong = Math.Max(chara.Strong, countryCharacters.Max(c => c.Strong));
              chara.Leadership = Math.Max(chara.Leadership, countryCharacters.Max(c => c.Leadership));
            }
            chara.Money = countryCharacters.Max(c => c.Money);
            chara.Rice = countryCharacters.Max(c => c.Rice);
            chara.Name = $"{country.Data.Name}_援軍{chara.Name}_{chara.Id}";
            await LogService.AddMapLogAsync(repo, false, EventType.ReinforcementActived, $"<character>{chara.Name}</character> が新たに <country>{country.Data.Name}</country> の援軍として加わりました");
          }
        }
        else if (currentReinforcements.Count() > requestedReinforcementCount)
        {
          // 援軍削除
          var count = currentReinforcements.Count() - requestedReinforcementCount;
          var targets = currentReinforcements.OrderByDescending(c => c.Id).Take(count).ToArray();
          foreach (var target in targets)
          {
            await CharacterService.RemoveAsync(repo, target);
            await LogService.AddMapLogAsync(repo, false, EventType.ReinforcementReturned, $"<character>{target.Name}</character> は、<country>{country.Data.Name}</country> の援軍の任を終え消滅しました");
          }
        }
      }
    }

    public static async Task<bool> CreateFarmerCountryAsync(MainRepository repo, Town town, Func<EventType, string, bool, Task> mapLogAsync, bool isForce = false, bool isKokin = false)
    {
      var system = await repo.System.GetAsync();
      var targetCountryOptional = await repo.Country.GetAliveByIdAsync(town.CountryId);

      var defenders = await repo.Town.GetDefendersAsync(town.Id);
      if (defenders.Count > 0)
      {
        if (isForce)
        {
          // 守備を剥がす
          var allRemovedDefenders = new List<TownDefender>();
          var townCharas = await repo.Town.GetCharactersAsync(town.Id);
          foreach (var defender in defenders)
          {
            var removedDefenders = await repo.Town.RemoveDefenderAsync(defender.Character.Id);
            allRemovedDefenders.AddRange(removedDefenders);
          }
          foreach (var d in allRemovedDefenders)
          {
            d.Status = TownDefenderStatus.Losed;
            if (targetCountryOptional.HasData)
            {
              await StatusStreaming.Default.SendCountryAsync(ApiData.From(d), targetCountryOptional.Data.Id);
              await StatusStreaming.Default.SendCharacterAsync(ApiData.From(d), townCharas.Where(tc => tc.CountryId != targetCountryOptional.Data.Id).Select(tc => tc.Id));
            }
            else
            {
              await StatusStreaming.Default.SendCharacterAsync(ApiData.From(d), townCharas.Select(tc => tc.Id));
            }
          }
        }
        else
        {
          return false;
        }
      }

      if (targetCountryOptional.HasData && await repo.Town.CountByCountryIdAsync(town.CountryId) <= 1)
      {
        if (isForce)
        {
          // 滅亡させる
          await CountryService.OverThrowAsync(repo, targetCountryOptional.Data);
          await mapLogAsync(EventType.Overthrown, "<country>" + targetCountryOptional.Data.Name + "</country> は滅亡しました", true);
        }
        else
        {
          return false;
        }
      }

      var countryColor = GetNotUsingCountryColor(await repo.Country.GetAllAsync());
      if (countryColor == 0)
      {
        return false;
      }

      var charaTypes = new List<CharacterAiType>
      {
        CharacterAiType.FarmerBattler,
        CharacterAiType.FarmerBattler,
        CharacterAiType.FarmerCivilOfficial,
      };
      if (isKokin)
      {
        charaTypes.Add(CharacterAiType.FarmerBattler);
        charaTypes.Add(CharacterAiType.FarmerCivilOfficial);
      }
      var country = await CreateCountryAsync(repo, system, town, charaTypes.ToArray());
      country.CountryColorId = countryColor;
      country.Name = isKokin ? "黄巾" : $"{town.Name}農民団";
      country.AiType = CountryAiType.Farmers;

      var myCharas = (await repo.Country.GetCharactersAsync(country.Id)).Where(c => !c.HasRemoved).ToArray();
      foreach (var chara in myCharas)
      {
        chara.Name = $"{chara.Name}{chara.Id}";
      }

      if (targetCountryOptional.HasData)
      {
        await mapLogAsync(EventType.AppendFarmers, $"<town>{town.Name}</town> の <country>{country.Name}</country> が <country>{targetCountryOptional.Data.Name}</country> に対して蜂起しました", true);
      }
      else
      {
        await mapLogAsync(EventType.AppendFarmers, $"<town>{town.Name}</town> の <country>{country.Name}</country> が蜂起し、独自勢力を築きました", true);
      }
      await repo.SaveChangesAsync();

      await StatusStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(town)));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(town)));
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(town), (await repo.Town.GetCharactersAsync(town.Id)).Select(c => c.Id));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(new CountryForAnonymous(country)));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new CountryForAnonymous(country)));

      if (targetCountryOptional.HasData && !isForce)
      {
        await CreateWarQuicklyAsync(repo, country, targetCountryOptional.Data);
      }

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
        .Where(c => c.AiType == CountryAiType.Farmers)
        .Select(c => c.Id);

      var allTowns = await repo.Town.GetAllAsync();
      var singleTownCountries = allTowns
        .GroupBy(t => t.CountryId)
        .Where(c => c.Count() <= 1)
        .Select(c => c.Key)
        .Where(c => c > 0);
      var towns = allTowns
        .Where(t => !singleTownCountries.Contains(t.CountryId))
        .Where(t => !aiCountries.Contains(t.CountryId))
        .Where(t => t.Security <= 8 && t.People <= 8000)
        .Where(t => !warCountries.Contains(t.CountryId) && !townWars.Select(tt => tt.TownId).Contains(t.Id))
        .ToList();

      // 守備のいる都市は除外
      var removeTowns = new List<Town>();
      foreach (var town in towns)
      {
        if ((await repo.Town.GetDefendersAsync(town.Id)).Count > 0 ||
            (town.CountryId > 0 && await repo.Town.CountByCountryIdAsync(town.CountryId) <= 1))
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

    private static async Task<IEnumerable<uint>> GetWaringCountries(MainRepository repo)
    {
      var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
      var warCountries = wars
        .Where(w => w.Status != CountryWarStatus.Stoped && w.Status != CountryWarStatus.None)
        .SelectMany(w => new uint[] { w.RequestedCountryId, w.InsistedCountryId, })
        .Distinct();
      return warCountries;
    }
  }
}
