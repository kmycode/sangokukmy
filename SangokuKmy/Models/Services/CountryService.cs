using Microsoft.SqlServer.Server;
using SangokuKmy.Common;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Services
{
  public static class CountryService
  {
    public static async Task OverThrowAsync(MainRepository repo, Country country, Country winnerCountry, bool isLog = true)
    {
      var system = await repo.System.GetAsync();
      country.HasOverthrown = true;
      country.OverthrownGameDate = system.GameDateTime;

      if (isLog)
      {
        await LogService.AddMapLogAsync(repo, true, EventType.Overthrown, $"<country>{country.Name}</country> は滅亡しました");
      }

      // 戦争勝利ボーナス
      /*
      var wars = (await repo.CountryDiplomacies.GetAllWarsAsync()).Where(w => w.IsJoin(country.Id));
      foreach (var war in wars)
      {
        var targetId = war.GetEnemy(country.Id);
        var target = winnerCountry?.Id == targetId ? winnerCountry : (await repo.Country.GetAliveByIdAsync(targetId)).Data;
        if (target != null)
        {
          var characterCount = war.RequestedCountryId == country.Id ? war.RequestedCountryCharacterMax : war.InsistedCountryCharacterMax;
          target.SafeMoney += characterCount * 16_0000;
          target.SafeMoney = Math.Min(target.SafeMoney, GetCountrySafeMax((await repo.Country.GetPoliciesAsync(target.Id)).Select(p => p.Type)));
          await StatusStreaming.Default.SendCountryAsync(ApiData.From(target), target.Id);
        }
      }
      */

      var targetCountryCharacters = await repo.Character.RemoveCountryAsync(country.Id);
      repo.Unit.RemoveUnitsByCountryId(country.Id);
      repo.Reinforcement.RemoveByCountryId(country.Id);
      repo.ChatMessage.RemoveByCountryId(country.Id);
      repo.CountryDiplomacies.RemoveByCountryId(country.Id);
      repo.Country.RemoveDataByCountryId(country.Id);

      // 玉璽
      if (winnerCountry != null)
      {
        if (country.GyokujiStatus != CountryGyokujiStatus.NotHave && country.GyokujiStatus != CountryGyokujiStatus.Refused)
        {
          if (winnerCountry.GyokujiStatus == CountryGyokujiStatus.NotHave || winnerCountry.GyokujiStatus == CountryGyokujiStatus.Refused)
          {
            winnerCountry.IntGyokujiGameDate = system.IntGameDateTime;
            if (!system.IsBattleRoyaleMode)
            {
              await LogService.AddMapLogAsync(repo, true, EventType.Gyokuji, $"<country>{winnerCountry.Name}</country> は玉璽を手に入れました");
            }
          }

          if (winnerCountry.GyokujiStatus != CountryGyokujiStatus.HasGenuine)
          {
            winnerCountry.GyokujiStatus = country.GyokujiStatus;
          }
          country.GyokujiStatus = CountryGyokujiStatus.NotHave;
          await StatusStreaming.Default.SendAllExceptForCountryAsync(ApiData.From(new CountryForAnonymous(winnerCountry)), winnerCountry.Id);
          await StatusStreaming.Default.SendCountryAsync(ApiData.From(winnerCountry), winnerCountry.Id);
        }
      }

      await StatusStreaming.Default.SendAllAsync(ApiData.From(country));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(country));
      await AiService.CheckManagedReinforcementsAsync(repo, country.Id);

      // 援軍データをいじって、無所属武将一覧で援軍情報を表示できるようにする
      var reinforcements = await repo.Reinforcement.GetByCountryIdAsync(country.Id);
      foreach (var rein in reinforcements.Where(r => r.RequestedCountryId == country.Id))
      {
        rein.RequestedCountryId = 0;
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(rein), rein.CharacterId);
      }

      // 滅亡国武将に通知
      var commanders = new CountryMessage
      {
        Type = CountryMessageType.Commanders,
        Message = string.Empty,
        CountryId = 0,
      };
      foreach (var targetCountryCharacter in await repo.Country.GetCharactersAsync(country.Id))
      {
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(targetCountryCharacter), targetCountryCharacter.Id);
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(commanders), targetCountryCharacter.Id);
      }
      await PushNotificationService.SendCountryAsync(repo, "滅亡", "あなたの国は滅亡しました。どこかの国に仕官するか、登用に応じることでゲームを続行できます", country.Id);

      // 登用分を無効化
      await ChatService.DenyCountryPromotions(repo, country);

      StatusStreaming.Default.UpdateCache(targetCountryCharacters);
      await repo.SaveChangesAsync();

      var allTowns = await repo.Town.GetAllAsync();
      var allCountries = await repo.Country.GetAllAsync();
      var townAiMap = allTowns.Join(allCountries, t => t.CountryId, c => c.Id, (t, c) => new { CountryId = c.Id, c.AiType, });
      var humanCountry = townAiMap.FirstOrDefault(t => t.AiType != CountryAiType.Terrorists);
      if (allTowns.All(t => t.CountryId > 0) &&
        townAiMap.All(t => t.CountryId == humanCountry.CountryId || t.AiType == CountryAiType.Terrorists))
      {
        if (!system.IsWaitingReset)
        {
          var unifiedCountry = humanCountry != null ? allCountries.FirstOrDefault(c => c.Id == humanCountry.CountryId) : allCountries.FirstOrDefault(c => !c.HasOverthrown);
          if (unifiedCountry != null)
          {
            await UnifyCountryAsync(repo, unifiedCountry);
          }
        }
      }
    }

    public static async Task UnifyCountryAsync(MainRepository repo, Country country)
    {
      if (country != null)
      {
        await LogService.AddMapLogAsync(repo, true, EventType.Unified, "大陸は、<country>" + country.Name + "</country> によって統一されました");
        await ResetService.RequestResetAsync(repo, country.Id);
      }
      else
      {
        await ResetService.RequestResetAsync(repo, 0);
      }

      await repo.SaveChangesAsync();
      var system = await repo.System.GetAsync();

      if (country != null)
      {
        await PushNotificationService.SendAllAsync(repo, "統一", $"{country.Name} は、大陸を統一しました。ゲームは {system.ResetGameDateTime.ToString()} にリセットされます");
      }
    }

    public static async Task SendWarAndSaveAsync(MainRepository repo, CountryWar war)
    {
      MapLog mapLog = null;

      await repo.CountryDiplomacies.SetWarAsync(war);

      if ((war.Status == CountryWarStatus.InReady && war.RequestedStopCountryId == 0) || war.Status == CountryWarStatus.Stoped)
      {
        // 戦争を周りに通知
        var country1 = await repo.Country.GetAliveByIdAsync(war.RequestedCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var country2 = await repo.Country.GetAliveByIdAsync(war.InsistedCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        mapLog = new MapLog
        {
          ApiGameDateTime = (await repo.System.GetAsync()).GameDateTime,
          Date = DateTime.Now,
          IsImportant = true,
        };
        if (war.Status == CountryWarStatus.InReady)
        {
          if (war.RequestedStopCountryId == 0)
          {
            mapLog.EventType = EventType.WarInReady;
            if (war.Mode == CountryWarMode.Religion)
            {
              mapLog.Message = "<country>" + country1.Name + "</country> は、<date>" + war.StartGameDate.ToString() + "</date> より <country>" + country2.Name + "</country> と宗教戦争を開始します";
            }
            else
            {
              mapLog.Message = "<country>" + country1.Name + "</country> は、<date>" + war.StartGameDate.ToString() + "</date> より <country>" + country2.Name + "</country> へ侵攻します";
            }
            await PushNotificationService.SendCountryAsync(repo, "宣戦布告", $"{war.StartGameDate.ToString()} より {country2.Name} と戦争します", country1.Id);
            await PushNotificationService.SendCountryAsync(repo, "宣戦布告", $"{war.StartGameDate.ToString()} より {country1.Name} と戦争します", country2.Id);
          }
        }
        else if (war.Status == CountryWarStatus.Stoped)
        {
          mapLog.EventType = EventType.WarStopped;
          mapLog.Message = "<country>" + country1.Name + "</country> と <country>" + country2.Name + "</country> の戦争は停戦しました";
          await PushNotificationService.SendCountryAsync(repo, "停戦", $"{country2.Name} との戦争は停戦しました", country1.Id);
          await PushNotificationService.SendCountryAsync(repo, "停戦", $"{country1.Name} との戦争は停戦しました", country2.Id);
        }
        await repo.MapLog.AddAsync(mapLog);

      }

      await repo.SaveChangesAsync();

      await StatusStreaming.Default.SendAllAsync(ApiData.From(war));
      if (mapLog != null)
      {
        await StatusStreaming.Default.SendAllAsync(ApiData.From(mapLog));
        await AnonymousStreaming.Default.SendAllAsync(ApiData.From(mapLog));
      }
    }

    public static async Task SendTownWarAndSaveAsync(MainRepository repo, TownWar war)
    {
      await repo.CountryDiplomacies.SetTownWarAsync(war);
      await repo.SaveChangesAsync();
      await StatusStreaming.Default.SendCountryAsync(ApiData.From(war), war.RequestedCountryId);
    }

    public static async Task<IReadOnlyList<uint>> GetPenaltyCountriesAsync(MainRepository repo, CountryWar assumptionWar = null)
    {
      return Enumerable.Empty<uint>().ToArray();

      /*
      var reinforcements = (await repo.Reinforcement.GetAllAsync()).Where(r => r.Status == ReinforcementStatus.Active);
      var characters = (await repo.Character.GetAllAliveAsync()).Where(c => c.DeleteTurn < 200 && (c.AiType == CharacterAiType.Human || c.AiType == CharacterAiType.Administrator));
      var countries = (await repo.Country.GetAllAsync()).Where(c => !c.HasOverthrown);
      var wars = (await repo.CountryDiplomacies.GetAllWarsAsync())
        .Where(w => !countries.Any(c => c.Id == w.RequestedCountryId && c.AiType == CountryAiType.Farmers))
        .Where(w => w.Status != CountryWarStatus.Stoped && w.Status != CountryWarStatus.None);
      if (assumptionWar != null)
      {
        wars = wars
          .Where(w => !(w.IsJoin(assumptionWar.InsistedCountryId) && w.IsJoin(assumptionWar.RequestedCountryId)))
          .Append(assumptionWar)
          .Where(w => w.Status != CountryWarStatus.Stoped && w.Status != CountryWarStatus.None);
      }

      var penaltyWarCountries = new List<uint>();

      // 複数国による同時宣戦を行っている国を抽出
      var cooperateWarCountries = wars
        .GroupBy(w => w.InsistedCountryId)
        .Where(g => g.Count() >= 2)
        .SelectMany(g => g.Select(w => w.RequestedCountryId))
        .Distinct();

      // 過剰援軍にペナルティ
      var reinforcementsOfCountry = characters
        .GroupBy(c => c.CountryId)
        .GroupJoin(reinforcements, c => c.Key, r => r.RequestedCountryId, (c, rs) => new { CountryId = c.Key, Reinforcements = rs, Characters = c, });
      foreach (var data in reinforcementsOfCountry)
      {
        IEnumerable<uint> GetEnemies(IEnumerable<uint> countryIds)
        {
          return wars
            .Where(w => countryIds.Any(c => w.IsJoin(c)))
            .SelectMany(w => new uint[] { w.InsistedCountryId, w.RequestedCountryId, })
            .Except(countryIds)
            .Distinct();
        }

        // 敵（自分が相手する国）
        var countries1 = GetEnemies(new uint[] { data.CountryId, });

        // 敵の敵（味方）
        var countries2 = GetEnemies(countries1);

        // 敵の敵の敵（味方の敵）
        var countries3 = GetEnemies(countries2);

        var warTargetCountries = countries3
          .Join(reinforcementsOfCountry, a => a, b => b.CountryId, (aa, bb) => bb);
        var mySideCountries = countries2
          .Join(reinforcementsOfCountry, a => a, b => b.CountryId, (aa, bb) => bb);

        if (warTargetCountries.Any() && mySideCountries.Any())
        {
          var warTargetsCharacterCount = warTargetCountries.Sum(c => c.Characters.Count());
          var warTargetsReinforcementCount = warTargetCountries.Sum(c => c.Reinforcements.Count());
          var mySideCharacterCount = mySideCountries.Sum(c => c.Characters.Count());
          var mySideReinforcementCount = mySideCountries.Sum(c => c.Reinforcements.Count());

          var isPenalty = false;

          // 単純な過剰援軍
          if (mySideReinforcementCount > 0 && mySideCharacterCount > warTargetsCharacterCount + 3)
          {
            isPenalty = true;
          }
          // 複数国同時布告による事実上の過剰援軍
          else if (cooperateWarCountries.Contains(data.CountryId) &&
            mySideCharacterCount > warTargetsCharacterCount + 5)
          {
            isPenalty = true;
          }

          if (isPenalty)
          {
            penaltyWarCountries.Add(data.CountryId);
          }
        }
      }

      return penaltyWarCountries;
      */
    }

    public static async Task<bool> IsLargeCountryPenaltyAsync(MainRepository repo, uint countryId)
    {
      var towns = await repo.Town.GetAllAsync();
      var countries = await repo.Country.GetAllAliveAsync();
      var characters = (await repo.Character.GetAllAliveAsync()).Where(c => c.AiType == CharacterAiType.Human);
      var townCountries = towns
        .GroupBy(t => t.CountryId)
        .Select(t => new { Country = countries.FirstOrDefault(c => c.Id == t.Key), TownsCount = t.Count(), CharactersCount = characters.Count(c => c.CountryId == t.Key), })
        .Where(c => c.Country != null && c.Country.AiType == CountryAiType.Human)
        .ToArray();

      if (townCountries.Count() < 3)
      {
        return false;
      }

      var check1 = false;
      var check2 = false;

      if (townCountries.OrderByDescending(t => t.TownsCount).First().Country.Id != countryId)
      {
      }
      else
      {
        var max1 = townCountries.First().TownsCount;
        var average1 = townCountries.Skip(1).Average(t => t.TownsCount);
        if (townCountries.Count() == 3)
        {
          check1 = max1 > average1 * 2.4;
        }
        else
        {
          check1 = max1 > average1 * 2;
        }
      }

      if (townCountries.OrderByDescending(t => t.CharactersCount).First().Country.Id != countryId)
      {
      }
      else
      {
        var max1 = townCountries.First().CharactersCount;
        var average1 = townCountries.Skip(1).Average(t => t.CharactersCount);
        check2 = max1 > average1 * 1.6;
      }

      return check1 || check2;
    }

    public static async Task<bool> SetPolicyAndSaveAsync(MainRepository repo, Country country, CountryPolicyType type, CountryPolicyStatus status = CountryPolicyStatus.Available, bool isCheckSubjects = true)
    {
      var info = CountryPolicyTypeInfoes.Get(type);
      if (!info.HasData)
      {
        return false;
      }

      var policies = await repo.Country.GetPoliciesAsync(country.Id);
      var old = policies.FirstOrDefault(p => p.Type == type);
      if (old != null && old.Status == CountryPolicyStatus.Available)
      {
        return false;
      }
      var oldStatus = old?.Status ?? CountryPolicyStatus.Unadopted;

      if (status == CountryPolicyStatus.Available && country.PolicyPoint < info.Data.GetRequestedPoint(oldStatus))
      {
        return false;
      }

      if (isCheckSubjects && info.Data.SubjectAppear != null && !info.Data.SubjectAppear(policies.Where(p => p.Status == CountryPolicyStatus.Available).Select(p => p.Type)))
      {
        return false;
      }

      var system = await repo.System.GetAsync();
      var param = new CountryPolicy
      {
        CountryId = country.Id,
        Status = status,
        Type = type,
        GameDate = system.GameDateTime.Year >= Config.UpdateStartYear ? system.GameDateTime : new GameDateTime { Year = Config.UpdateStartYear, Month = 1, },
      };
      if (status == CountryPolicyStatus.Available)
      {
        country.PolicyPoint -= info.Data.GetRequestedPoint(oldStatus);

        if (info.Data.AvailableDuring > 0)
        {
          status = param.Status = CountryPolicyStatus.Availabling;
        }

        // 採用した瞬間に効果を発揮する政策
        await RunCountryPolicyAsync(repo, country, type);
      }
      if (old != null)
      {
        repo.Country.RemovePolicy(old);
      }
      await repo.Country.AddPolicyAsync(param);

      await repo.SaveChangesAsync();

      await StatusStreaming.Default.SendCountryAsync(ApiData.From(param), country.Id);
      await StatusStreaming.Default.SendCountryAsync(ApiData.From(country), country.Id);
      await StatusStreaming.Default.SendAllAsync(ApiData.From(new CountryForAnonymous(country)));

      foreach (CountryPolicyType boostType in info.Data.Effects.Where(e => e.Type == CountryPolicyEffectType.BoostWith).Select(e => e.Value))
      {
        var boostInfo = CountryPolicyTypeInfoes.Get(boostType);
        if (boostInfo.HasData)
        {
          await SetPolicyAndSaveAsync(repo, country, boostType, CountryPolicyStatus.Boosted);
        }
      }

      return true;
    }

    private static async Task RunCountryPolicyAsync(MainRepository repo, Country country, CountryPolicyType type)
    {
      if (type == CountryPolicyType.TotalMobilization || type == CountryPolicyType.TotalMobilization2)
      {
        foreach (var chara in await repo.Country.GetCharactersAsync(country.Id))
        {
          chara.SoldierNumber = chara.Leadership;
          chara.Proficiency = 100;
          var formation = await repo.Character.GetFormationAsync(chara.Id, chara.FormationType);
          formation.Experience += 5000;
          await CharacterService.StreamCharacterAsync(repo, chara);
          await StatusStreaming.Default.SendCharacterAsync(ApiData.From(formation), chara.Id);
        }
        await LogService.AddMapLogAsync(repo, true, EventType.Policy, $"<country>{country.Name}</country> は、国民総動員を発動しました");
      }
      else if (type == CountryPolicyType.TotalMobilizationWall || type == CountryPolicyType.TotalMobilizationWall2)
      {
        foreach (var town in await repo.Town.GetByCountryIdAsync(country.Id))
        {
          town.Technology = town.TechnologyMax;
          town.Wall = town.WallMax;
          await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(town), repo);
        }
        await LogService.AddMapLogAsync(repo, true, EventType.Policy, $"<country>{country.Name}</country> は、城壁作業員総動員を発動しました");
      }
      else if (type == CountryPolicyType.Austerity || type == CountryPolicyType.Austerity2)
      {
        foreach (var chara in await repo.Country.GetCharactersAsync(country.Id))
        {
          chara.Money += 20_0000;
          await CharacterService.StreamCharacterAsync(repo, chara);
        }
        await LogService.AddMapLogAsync(repo, true, EventType.Policy, $"<country>{country.Name}</country> は、緊縮財政を発動しました");
      }

      var info = CountryPolicyTypeInfoes.Get(type);
      if (info.HasData && info.Data.Effects.Any(e => e.Type == CountryPolicyEffectType.SubBuildingSizeMax))
      {
        country.TownSubBuildingExtraSpace +=
          (short)info.Data.Effects
            .Where(e => e.Type == CountryPolicyEffectType.SubBuildingSizeMax)
            .Sum(e => e.Value);
      }
    }

    public static async Task<IReadOnlyList<Character>> FilterCountryCharactersAsync(MainRepository repo, uint countryId, CountryCommanderSubject subject, uint subjectData, uint subjectData2)
    {
      var charas = await repo.Country.GetCharactersAsync(countryId);
      IEnumerable<Character> targets = Enumerable.Empty<Character>();
      if (subject == CountryCommanderSubject.All)
      {
        targets = charas;
      }
      if (subject == CountryCommanderSubject.Attribute)
      {
        targets = charas.Where(c => c.GetCharacterType() == (CharacterType)subjectData);
      }
      if (subject == CountryCommanderSubject.From)
      {
        var from = (CharacterFrom)subjectData;
        if (from == CharacterFrom.Confucianism || from == CharacterFrom.Taoism || from == CharacterFrom.Buddhism)
        {
          targets = charas.Where(c => c.From == CharacterFrom.Confucianism || c.From == CharacterFrom.Taoism || c.From == CharacterFrom.Buddhism);
        }
        else
        {
          targets = charas.Where(c => c.From == from);
        }
      }
      if (subject == CountryCommanderSubject.Private)
      {
        targets = charas.Where(c => c.Id == subjectData);
      }
      if (subject == CountryCommanderSubject.ExceptForReinforcements ||
        subject == CountryCommanderSubject.ContainsMyReinforcements ||
        subject == CountryCommanderSubject.OriginalCountryCharacters)
      {
        targets = Enumerable.Empty<Character>();
        if (subject == CountryCommanderSubject.ExceptForReinforcements ||
          subject == CountryCommanderSubject.OriginalCountryCharacters)
        {
          var reinforcements = (await repo.Reinforcement.GetByCountryIdAsync(countryId))
            .Where(r => r.Status == ReinforcementStatus.Active && r.RequestedCountryId == countryId);
          targets = targets.Concat(charas.Where(c => !reinforcements.Any(r => r.CharacterId == c.Id)));
        }
        if (subject == CountryCommanderSubject.ContainsMyReinforcements ||
          subject == CountryCommanderSubject.OriginalCountryCharacters)
        {
          var reinforcements = (await repo.Reinforcement.GetByCountryIdAsync(countryId))
            .Where(r => r.Status == ReinforcementStatus.Active && r.CharacterCountryId == countryId);
          foreach (var cid in reinforcements.Select(r => r.CharacterId))
          {
            var character = await repo.Character.GetByIdAsync(cid);
            if (character.HasData)
            {
              targets = targets.Append(character.Data);
            }
          }
        }
      }
      if (subject == CountryCommanderSubject.Post)
      {
        var posts = await repo.Country.GetPostsAsync(countryId);
        targets = charas.Join(posts.Where(p => p.Type == (CountryPostType)subjectData), c => c.Id, p => p.CharacterId, (c, p) => c);
      }

      return targets.ToArray();
    }

    public static int GetSecretaryMax(IEnumerable<CountryPolicyType> policies)
    {
      return policies.GetSumOfValues(CountryPolicyEffectType.Secretary);
    }

    public static int GetCurrentSecretaryPoint(IEnumerable<CharacterAiType> currentSecretaries)
    {
      return currentSecretaries
        .Sum(c => c == CharacterAiType.SecretaryPatroller ? 2 :
                  c == CharacterAiType.SecretaryPioneer ? 1 :
                  c == CharacterAiType.SecretaryUnitGather ? 1 :
                  c == CharacterAiType.SecretaryUnitLeader ? 1 :
                  c == CharacterAiType.SecretaryScouter ? 1 :
                  c == CharacterAiType.SecretaryEvangelist ? 1 :
                  c == CharacterAiType.SecretaryTrader ? 2 : 0);
    }

    public static int GetCountrySafeMax(IEnumerable<CountryPolicyType> policies)
    {
      return policies.GetSumOfValues(CountryPolicyEffectType.CountrySafeMax);
    }
  }
}
