using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SangokuKmy.Common;
using SangokuKmy.Filters;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Streamings;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [SangokuKmyErrorFilter]
  public class EntryController : Controller
  {
    [HttpPost("entry")]
    public async Task<ApiData<AuthenticationData>> Entry(
      [FromBody] EntryData param)
    {
      var ip = this.HttpContext.Connection.RemoteIpAddress?.ToString();
      using (var repo = MainRepository.WithReadAndWrite())
      {
        await EntryService.EntryAsync(repo, ip, param.Character, param.Icon, param.Password, param.Country, param.InvitationCode);
        await repo.SaveChangesAsync();

        var authData = await AuthenticationService.WithIdAndPasswordAsync(repo, param.Character.AliasId, param.Password);
        return ApiData.From(authData);
      }
    }

    [HttpGet("entry/data")]
    public async Task<EntryExtraData> GetCountryExtraData()
    {
      var ext = new EntryExtraData();

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var system = await repo.System.GetAsync();
        var countries = await repo.Country.GetAllAsync();
        var countryExtras = new List<EntryExtraData.CountryExtraData>();
        foreach (var country in countries)
        {
          var extra = new EntryExtraData.CountryExtraData
          {
            CountryId = country.Id,
          };

          if (country.IntEstablished + Config.CountryBattleStopDuring > system.IntGameDateTime)
          {
            var characterCount = await repo.Country.CountCharactersAsync(country.Id);
            extra.IsJoinLimited = characterCount >= Config.CountryJoinMaxOnLimited;
          }

          countryExtras.Add(extra);
        }

        ext.CountryData = countryExtras;
        ext.AttributeMax = EntryService.GetAttributeMax(system.GameDateTime);
        ext.AttributeSumMax = EntryService.GetAttributeSumMax(system.GameDateTime);

        return ext;
      }
    }

    public class EntryData
    {
      [JsonProperty("character")]
      public Character Character { get; set; }
      [JsonProperty("password")]
      public string Password { get; set; }
      [JsonProperty("icon")]
      public CharacterIcon Icon { get; set; }
      [JsonProperty("country")]
      public Country Country { get; set; }
      [JsonProperty("invitationCode")]
      public string InvitationCode { get; set; }
    }
  }
}
