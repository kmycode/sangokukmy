using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SangokuKmy.Common;
using SangokuKmy.Filters;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;
using System.Collections.Generic;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [ServiceFilter(typeof(SangokuKmyErrorFilterAttribute))]
  [AuthenticationFilter]
  public class CountryPolicyController : Controller, IAuthenticationDataReceiver
  {
    private readonly ILogger _logger;
    public AuthenticationData AuthData { private get; set; }

    public CountryPolicyController(ILogger<CountryPolicyController> logger)
    {
      this._logger = logger;
    }

    [HttpGet("country/{id}/policies")]
    public async Task<IEnumerable<CountryPolicy>> GetCountryAllPoliciesAsync(
      [FromRoute] uint id = 0)
    {
      using (var repo = MainRepository.WithRead())
      {
        var country = await repo.Country.GetAliveByIdAsync(id).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var policies = await repo.Country.GetPoliciesAsync(id);
        return policies;
      }
    }

    [HttpPost("country/policies")]
    public async Task SetCountryPolicyAsync(
      [FromBody] CountryPolicy param)
    {
      MapLog maplog;
      Country country;

      var info = CountryPolicyTypeInfoes.Get(param.Type);
      if (!info.HasData)
      {
        ErrorCode.InvalidParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var system = await repo.System.GetAsync();

        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        country = await repo.Country.GetAliveByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var posts = await repo.Country.GetPostsAsync(chara.CountryId);
        var myPost = posts.FirstOrDefault(p => p.CharacterId == chara.Id);
        if (myPost == null || !myPost.Type.CanPolicy())
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var policies = await repo.Country.GetPoliciesAsync(chara.CountryId);
        var old = policies.FirstOrDefault(p => p.Type == param.Type);
        if (old != null && old.Status == CountryPolicyStatus.Available)
        {
          ErrorCode.MeaninglessOperationError.Throw();
        }
        var status = old?.Status ?? CountryPolicyStatus.Unadopted;

        if (country.PolicyPoint < info.Data.GetRequestedPoint(status))
        {
          ErrorCode.InvalidOperationError.Throw();
        }

        if (info.Data.SubjectAppear != null && !info.Data.SubjectAppear(policies.Where(p => p.Status == CountryPolicyStatus.Available).Select(p => p.Type)))
        {
          ErrorCode.InvalidOperationError.Throw();
        }

        param.CountryId = chara.CountryId;
        param.Status = CountryPolicyStatus.Available;
        country.PolicyPoint -= info.Data.GetRequestedPoint(status);
        await repo.Country.AddPolicyAsync(param);

        maplog = new MapLog
        {
          EventType = EventType.Policy,
          ApiGameDateTime = system.GameDateTime,
          Date = DateTime.Now,
          IsImportant = false,
          Message = $"<country>{country.Name}</country> は、政策 {info.Data.Name} を採用しました",
        };
        await repo.MapLog.AddAsync(maplog);

        await repo.SaveChangesAsync();
      }

      await StatusStreaming.Default.SendAllAsync(ApiData.From(maplog));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(param));
      await StatusStreaming.Default.SendCountryAsync(ApiData.From(country), country.Id);
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(maplog));
    }
  }
}
