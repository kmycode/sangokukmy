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
using System.Text;

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
      SystemData system;
      Character chara;
      IEnumerable<MapLog> maplogs;
      IEnumerable<MapLog> importantMaplogs;
      IEnumerable<CharacterLog> characterLogs;
      IEnumerable<Country> countries;
      IEnumerable<TownForAnonymous> towns;
      IEnumerable<Town> myTowns;
      using (var repo = MainRepository.WithRead())
      {
        system = await repo.System.GetAsync();
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        maplogs = await repo.MapLog.GetNewestAsync(50);
        importantMaplogs = await repo.MapLog.GetImportantNewestAsync(50);
        characterLogs = await repo.Character.GetCharacterLogsAsync(this.AuthData.CharacterId, 50);
        countries = await repo.Country.GetAllAsync();

        var allTowns = await repo.Town.GetAllAsync();
        towns = allTowns.Select(tw => new TownForAnonymous(tw));
        myTowns = allTowns.Where(tw => tw.CountryId == chara.CountryId);
      }

      // HTTPヘッダを設定する
      this.Response.Headers.Add("Content-Type", "text/event-stream; charset=UTF-8");
      this.Response.Headers.Add("Cache-Control", "no-cache");

      // 送信する初期データをリストアップ
      var sendData = Enumerable.Empty<object>()
        .Concat(new object[] { ApiData.From(chara), ApiData.From(system.GameDateTime), })
        .Concat(maplogs.Select(ml => ApiData.From(ml)))
        .Concat(importantMaplogs.Select(ml => ApiData.From(ml)))
        .Concat(characterLogs.Select(cl => ApiData.From(cl)))
        .Concat(countries.Select(c => ApiData.From(c)))
        .Concat(towns.Select(tw => ApiData.From(tw)))
        .Concat(myTowns.Select(tw => ApiData.From(tw)))
        .ToList();

      // 初期データを送信する
      var initializeData = new StringBuilder();
      foreach (var obj in sendData)
      {
        initializeData.Append(JsonConvert.SerializeObject(obj));
        initializeData.Append("\n");
      }
      await this.Response.WriteAsync(initializeData.ToString());

      // ストリーミング対象に追加し、対象から外れるまで待機する
      var isRemoved = false;
      StatusStreaming.Default.Add(this.Response, this.AuthData, chara, () => isRemoved = true);
      while (!isRemoved)
      {
        await Task.Delay(5000);
      }
    }
  }
}
