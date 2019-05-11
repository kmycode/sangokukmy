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

    public static async Task<bool> SetPolicyAndSaveAsync(MainRepository repo, Country country, CountryPolicyType type, bool isCheckSubjects = true)
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
      var status = old?.Status ?? CountryPolicyStatus.Unadopted;

      if (country.PolicyPoint < info.Data.GetRequestedPoint(status))
      {
        return false;
      }

      if (isCheckSubjects && info.Data.SubjectAppear != null && !info.Data.SubjectAppear(policies.Where(p => p.Status == CountryPolicyStatus.Available).Select(p => p.Type)))
      {
        return false;
      }

      var param = new CountryPolicy
      {
        CountryId = country.Id,
        Status = CountryPolicyStatus.Available,
        Type = type,
      };
      country.PolicyPoint -= info.Data.GetRequestedPoint(status);
      await repo.Country.AddPolicyAsync(param);

      var system = await repo.System.GetAsync();
      var maplog = new MapLog
      {
        EventType = EventType.Policy,
        ApiGameDateTime = system.GameDateTime,
        Date = DateTime.Now,
        IsImportant = false,
        Message = $"<country>{country.Name}</country> は、政策 {info.Data.Name} を採用しました",
      };
      await repo.MapLog.AddAsync(maplog);

      await repo.SaveChangesAsync();

      await StatusStreaming.Default.SendAllAsync(ApiData.From(maplog));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(param));
      await StatusStreaming.Default.SendCountryAsync(ApiData.From(country), country.Id);
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(maplog));

      return true;
    }

    public static int GetSecretaryMax(IEnumerable<CountryPolicyType> policies)
    {
      return policies.Contains(CountryPolicyType.HumanDevelopment) ? 1 : 0;
    }

    public static int GetCountrySafeMax(IEnumerable<CountryPolicyType> policies)
    {
      var count = policies.Count(p => p == CountryPolicyType.Storage || p == CountryPolicyType.UndergroundStorage ||
                                      p == CountryPolicyType.StomachStorage || p == CountryPolicyType.BloodVesselsStorage);
      return Config.CountrySafeMax * count;
    }
  }
}
