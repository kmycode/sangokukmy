﻿using Microsoft.SqlServer.Server;
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
    public static async Task OverThrowAsync(MainRepository repo, Country country, Country winnerCountry)
    {
      var system = await repo.System.GetAsync();
      country.HasOverthrown = true;
      country.OverthrownGameDate = system.GameDateTime;

      await LogService.AddMapLogAsync(repo, true, EventType.Overthrown, $"<country>{country.Name}</country> は滅亡しました");

      var targetCountryCharacters = await repo.Character.RemoveCountryAsync(country.Id);
      repo.Unit.RemoveUnitsByCountryId(country.Id);
      repo.Reinforcement.RemoveByCountryId(country.Id);
      repo.ChatMessage.RemoveByCountryId(country.Id);
      repo.CountryDiplomacies.RemoveByCountryId(country.Id);
      repo.Country.RemoveDataByCountryId(country.Id);

      // 玉璽
      if (winnerCountry != null)
      {
        if (country.GyokujiStatus != CountryGyokujiStatus.NotHave)
        {
          if (winnerCountry.GyokujiStatus == CountryGyokujiStatus.NotHave)
          {
            winnerCountry.IntGyokujiGameDate = system.IntGameDateTime;
            await LogService.AddMapLogAsync(repo, true, EventType.Gyokuji, $"<country>{winnerCountry.Name} は玉璽を手に入れました");
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
      await LogService.AddMapLogAsync(repo, true, EventType.Unified, "大陸は、<country>" + country.Name + "</country> によって統一されました");
      await ResetService.RequestResetAsync(repo, country.Id);

      await repo.SaveChangesAsync();
      var system = await repo.System.GetAsync();
      await PushNotificationService.SendAllAsync(repo, "統一", $"{country.Name} は、大陸を統一しました。ゲームは {system.ResetGameDateTime.ToString()} にリセットされます");
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
            mapLog.Message = "<country>" + country1.Name + "</country> は、<date>" + war.StartGameDate.ToString() + "</date> より <country>" + country2.Name + "</country> へ侵攻します";
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
      if (type == CountryPolicyType.TotalMobilizationWall || type == CountryPolicyType.TotalMobilizationWall2)
      {
        foreach (var town in await repo.Town.GetByCountryIdAsync(country.Id))
        {
          town.Technology = town.TechnologyMax;
          town.Wall = town.WallMax;
          await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(town), repo);
        }
        await LogService.AddMapLogAsync(repo, true, EventType.Policy, $"<country>{country.Name}</country> は、城壁作業員総動員を発動しました");
      }
      if (type == CountryPolicyType.Austerity || type == CountryPolicyType.Austerity2)
      {
        foreach (var chara in await repo.Country.GetCharactersAsync(country.Id))
        {
          chara.Money += 20_0000;
          await CharacterService.StreamCharacterAsync(repo, chara);
        }
        await LogService.AddMapLogAsync(repo, true, EventType.Policy, $"<country>{country.Name}</country> は、緊縮財政を発動しました");
      }
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
                  c == CharacterAiType.SecretaryScouter ? 1 : 0);
    }

    public static int GetCountrySafeMax(IEnumerable<CountryPolicyType> policies)
    {
      return policies.GetSumOfValues(CountryPolicyEffectType.CountrySafeMax);
    }
  }
}
