using Microsoft.AspNetCore.Mvc;
using SangokuKmy.Common;
using SangokuKmy.Filters;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [ServiceFilter(typeof(SangokuKmyErrorFilterAttribute))]
  [AuthenticationFilter]
  public class OnlineController : Controller, IAuthenticationDataReceiver
  {
    public AuthenticationData AuthData { private get; set; }

    [HttpGet("online")]
    public async Task<IEnumerable<CharacterOnline>> GetAllAsync()
    {
      return await OnlineService.GetAsync();
    }

    [HttpPut("online/{status}")]
    public async Task SetStatusAsync(
      [FromRoute] string status = null)
    {
      var s = status == "active" ? OnlineStatus.Active : status == "inactive" ? OnlineStatus.Inactive : OnlineStatus.Offline;
      if (s == OnlineStatus.Offline)
      {
        ErrorCode.InvalidParameterError.Throw();
      }

      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        await OnlineService.SetAsync(chara, s);
      }
    }
  }
}
