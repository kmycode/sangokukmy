﻿using System;
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
      Optional<Country> country;
      IEnumerable<TownForAnonymous> towns;
      Town town;
      using (var repo = MainRepository.WithRead())
      {
        system = await repo.System.GetAsync();
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        maplogs = await repo.MapLog.GetNewestAsync(5);
        importantMaplogs = await repo.MapLog.GetImportantNewestAsync(5);
        country = await repo.Country.GetByIdAsync(chara.CountryId);
        towns = await repo.Town.GetAllForAnonymousAsync();
        town = await repo.Town.GetByIdAsync(chara.TownId).GetOrErrorAsync(ErrorCode.InternalDataNotFoundError, new { entityType = "town", });
      }

      // HTTPヘッダを設定する
      this.Response.Headers.Add("Content-Type", "text/event-stream; charset=UTF-8");

      // 送信する初期データをリストアップ
      var sendData = new List<object> {
        ApiData.From(chara),
        ApiData.From(system.GameDateTime), };
      foreach (var item in maplogs.Select(ml => ApiData.From(ml)))
      {
        sendData.Add(item);
      }
      foreach (var item in importantMaplogs.Select(ml => ApiData.From(ml)))
      {
        sendData.Add(item);
      }
      foreach (var item in towns.Select(tw => ApiData.From(tw)))
      {
        sendData.Add(item);
      }
      sendData.Add(ApiData.From(town));
      if (country.HasData) sendData.Add(ApiData.From(country.Data));

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
        await StatusStreaming.Default.SendAll(ApiData.From(ApiDateTime.FromDateTime(DateTime.Now)));
      }
    }
  }
}
