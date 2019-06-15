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
    public static async Task SendWarAndSaveAsync(MainRepository repo, CountryWar war)
    {
      await repo.CountryDiplomacies.SetWarAsync(war);

      // 戦争を周りに通知
      var country1 = await repo.Country.GetAliveByIdAsync(war.RequestedCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
      var country2 = await repo.Country.GetAliveByIdAsync(war.InsistedCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
      var mapLog = new MapLog
      {
        ApiGameDateTime = (await repo.System.GetAsync()).GameDateTime,
        Date = DateTime.Now,
        EventType = EventType.WarInReady,
        IsImportant = true,
        Message = "<country>" + country1.Name + "</country> は、<date>" + war.StartGameDate.ToString() + "</date> より <country>" + country2.Name + "</country> へ侵攻します",
      };
      await repo.MapLog.AddAsync(mapLog);

      await repo.SaveChangesAsync();

      await StatusStreaming.Default.SendAllAsync(ApiData.From(war));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(mapLog));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(mapLog));
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

      if (country.PolicyPoint < info.Data.GetRequestedPoint(oldStatus))
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
      country.PolicyPoint -= info.Data.GetRequestedPoint(oldStatus);
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

    public static int GetSecretaryMax(IEnumerable<CountryPolicyType> policies)
    {
      return policies.GetSumOfValues(CountryPolicyEffectType.Secretary);
    }

    public static int GetCountrySafeMax(IEnumerable<CountryPolicyType> policies)
    {
      return policies.GetSumOfValues(CountryPolicyEffectType.CountrySafeMax);
    }
  }
}
