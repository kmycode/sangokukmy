using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SangokuKmy.Filters;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Data;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using SangokuKmy.Streamings;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Controllers
{
  [Route("api/v1/streaming")]
  [SangokuKmyErrorFilter]
  public class SangokuKmyStreamingController : Controller, IAuthenticationDataReceiver
  {
    public AuthenticationData AuthData { private get; set; }

    [AuthenticationFilter]
    [HttpGet("status")]
    public async Task StatusStreamingAsync()
    {
      Character chara;
      using (var repo = new MainRepository())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
      }

      // HTTPヘッダを設定する
      this.Response.Headers.Add("Content-Type", "text/event-stream; charset=UTF-8");

      await this.Response.WriteAsync(JsonConvert.SerializeObject(ApiData.From(chara)) + "\n");

      // ストリーミング対象に追加し、対象から外れるまで待機する
      var isRemoved = false;
      StatusStreaming.Default.Add(this.Response, this.AuthData, chara, () => isRemoved = true);
      while (!isRemoved)
      {
        await Task.Delay(5000);
        await StatusStreaming.Default.SendAll(ApiData.From(ApiDateTime.FromDateTime(DateTime.Now)));
      }
    }
  }
}
