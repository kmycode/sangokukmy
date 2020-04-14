using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using SangokuKmy.Common;
using SangokuKmy.Filters;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [ServiceFilter(typeof(SangokuKmyErrorFilterAttribute))]
  public class PushNotificationController : Controller, IAuthenticationDataReceiver
  {
    public AuthenticationData AuthData { private get; set; }

    [HttpGet("nkey/entry/{id}/{platform}/{key}")]
    public async Task<string> EntryAsync(
      [FromRoute] uint id = default,
      [FromRoute] string platform = default,
      [FromRoute] string key = default)
    {
      if (string.IsNullOrWhiteSpace(key))
      {
        ErrorCode.LackOfParameterError.Throw();
      }

      var pl = platform == "android" ? PushNotificationPlatform.Android : platform == "ios" ? PushNotificationPlatform.iOS : PushNotificationPlatform.Undefined;
      if (pl == PushNotificationPlatform.Undefined)
      {
        ErrorCode.InvalidParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(id).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var keys = await repo.PushNotificationKey.GetAsync(id);
        if (keys.Any(k => k.Key == key))
        {
          ErrorCode.MeaninglessOperationError.Throw();
        }

        await repo.PushNotificationKey.AddAsync(new PushNotificationKey
        {
          CharacterId = id,
          Platform = pl,
          Key = key,
        });
        await repo.SaveChangesAsync();
      }

      return "success";
    }
  }
}
