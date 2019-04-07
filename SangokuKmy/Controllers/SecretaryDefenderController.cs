using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SangokuKmy.Common;
using SangokuKmy.Filters;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [ServiceFilter(typeof(SangokuKmyErrorFilterAttribute))]
  [AuthenticationFilter]
  public class SecretaryDefenderController : Controller, IAuthenticationDataReceiver
  {
    public AuthenticationData AuthData { private get; set; }

    [HttpPut("secretary/{id}")]
    public async Task UpdateSecretaryAsync([FromRoute] uint id, [FromBody] UpdateSecretaryParameter param)
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var country = await repo.Country.GetAliveByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var posts = await repo.Country.GetPostsAsync(chara.CountryId);
        var myPosts = posts.Where(p => p.CharacterId == chara.Id);
        if (!myPosts.Any(p => p.Type == CountryPostType.Monarch || p.Type == CountryPostType.Warrior))
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var secretary = await repo.Character.GetByIdAsync(id).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        if (secretary.AiType != CharacterAiType.SecretaryDefender)
        {
          ErrorCode.InvalidParameterError.Throw();
        }

        switch (param.Type)
        {
          case "1":
          case "2":
          case "3":
          case "4":
            secretary.AliasId = param.Type;
            break;
          default:
            ErrorCode.InvalidParameterError.Throw();
            break;
        }

        if (param.UnitId != 0)
        {
          var unit = await repo.Unit.GetByIdAsync(param.UnitId).GetOrErrorAsync(ErrorCode.UnitNotFoundError);
          if (unit.CountryId != chara.CountryId)
          {
            ErrorCode.NotPermissionError.Throw();
          }

          var member = new UnitMember
          {
            CharacterId = id,
            Post = UnitMemberPostType.Normal,
            UnitId = unit.Id,
          };
          await repo.Unit.SetMemberAsync(member);
        }

        await repo.SaveChangesAsync();
      }
    }

    public struct UpdateSecretaryParameter
    {
      [JsonProperty("type")]
      public string Type { get; set; }

      [JsonProperty("unitId")]
      public uint UnitId { get; set; }
    }
  }
}
