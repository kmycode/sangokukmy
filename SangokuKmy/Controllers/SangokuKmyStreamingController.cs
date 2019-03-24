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
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Services;

namespace SangokuKmy.Controllers
{
  [Route("api/v1/streaming")]
  [ServiceFilter(typeof(SangokuKmyErrorFilterAttribute))]
  public class SangokuKmyStreamingController : Controller, IAuthenticationDataReceiver
  {
    public AuthenticationData AuthData { private get; set; }

    [AuthenticationFilter]
    [HttpGet("status")]
    public async Task StatusStreamingAsync()
    {
      SystemData system;
      Character chara;
      Country country;
      IEnumerable<MapLog> maplogs;
      IEnumerable<MapLog> importantMaplogs;
      IEnumerable<CharacterLog> characterLogs;
      IEnumerable<CountryForAnonymous> countries;
      IEnumerable<TownForAnonymous> towns;
      IEnumerable<Town> myTowns;
      IEnumerable<ScoutedTown> scoutedTowns;
      IEnumerable<ChatMessage> chatMessages;
      IEnumerable<CountryAlliance> alliances;
      IEnumerable<CountryWar> wars;
      IEnumerable<TownWar> townWars;
      IEnumerable<ThreadBbsItem> countryBbsItems;
      IEnumerable<ThreadBbsItem> globalBbsItems;
      IEnumerable<Reinforcement> reinforcements;
      IEnumerable<CharacterOnline> onlines = await OnlineService.GetAsync();
      IEnumerable<CountryMessage> countryMessages;
      IEnumerable<CharacterSoldierType> solidierTypes;
      using (var repo = MainRepository.WithRead())
      {
        system = await repo.System.GetAsync();
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        maplogs = await repo.MapLog.GetNewestAsync(50);
        importantMaplogs = await repo.MapLog.GetImportantNewestAsync(50);
        characterLogs = await repo.Character.GetCharacterLogsAsync(this.AuthData.CharacterId, 50);
        country = (await repo.Country.GetByIdAsync(chara.CountryId)).Data;
        countries = await repo.Country.GetAllForAnonymousAsync();
        chatMessages = (await repo.ChatMessage.GetCountryMessagesAsync(chara.CountryId, uint.MaxValue, 50))
          .Concat(await repo.ChatMessage.GetGlobalMessagesAsync(uint.MaxValue, 50))
          .Concat(await repo.ChatMessage.GetPrivateMessagesAsync(chara.Id, uint.MaxValue, 50))
          .Concat(await repo.ChatMessage.GetPromotionMessagesAsync(chara.Id, uint.MaxValue, 50));
        alliances = (await repo.CountryDiplomacies.GetAllPublicAlliancesAsync())
          .Concat(await repo.CountryDiplomacies.GetCountryAllAlliancesAsync(chara.CountryId));
        wars = await repo.CountryDiplomacies.GetAllWarsAsync();
        townWars = await repo.CountryDiplomacies.GetAllTownWarsAsync();
        countryBbsItems = await repo.ThreadBbs.GetCountryBbsByCountryIdAsync(chara.CountryId);
        globalBbsItems = await repo.ThreadBbs.GetGlobalBbsAsync();
        reinforcements = await repo.Reinforcement.GetByCharacterIdAsync(chara.Id);
        countryMessages = await repo.Country.GetMessagesAsync(chara.CountryId);
        solidierTypes = await repo.CharacterSoldierType.GetByCharacterIdAsync(chara.Id);

        var allTowns = await repo.Town.GetAllAsync();
        towns = allTowns.Select(tw => new TownForAnonymous(tw));
        myTowns = allTowns.Where(tw => tw.CountryId == chara.CountryId || chara.TownId == tw.Id);
        scoutedTowns = await repo.ScoutedTown.GetByScoutedCountryIdAsync(chara.CountryId);
      }

      // HTTPヘッダを設定する
      this.Response.Headers.Add("Content-Type", "text/event-stream; charset=UTF-8");
      this.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");

      // 送信する初期データをリストアップ
      var sendData = Enumerable.Empty<object>()
        .Concat(solidierTypes.Select(s => ApiData.From(s)))
        .Concat(new object[]
        {
          ApiData.From(chara),
          ApiData.From(system),
        })
        .Concat(maplogs.Select(ml => ApiData.From(ml)))
        //.Concat(importantMaplogs.Select(ml => ApiData.From(ml)))
        .Concat(characterLogs.Select(cl => ApiData.From(cl)))
        .Concat(countries.Select(c => ApiData.From(c)))
        .Concat(towns.Select(tw => ApiData.From(tw)))
        .Concat(myTowns.Select(tw => ApiData.From(tw)))
        .Concat(scoutedTowns.Select(st => ApiData.From(st)))
        .Concat(chatMessages.Select(cm => ApiData.From(cm)))
        .Concat(alliances.Select(ca => ApiData.From(ca)))
        .Concat(wars.Select(cw => ApiData.From(cw)))
        .Concat(townWars.Select(tw => ApiData.From(tw)))
        .Concat(countryBbsItems.Select(b => ApiData.From(b)))
        .Concat(globalBbsItems.Select(b => ApiData.From(b)))
        .Concat(reinforcements.Select(r => ApiData.From(r)))
        .Concat(onlines.Select(o => ApiData.From(o)))
        .Concat(countryMessages.Select(c => ApiData.From(c)))
        .ToList();
      sendData.Add(ApiData.From(new ApiSignal
      {
        Type = SignalType.EndOfStreamingInitializeData,
      }));

      // 初期データを送信する
      var initializeData = new StringBuilder();
      foreach (var obj in sendData)
      {
        initializeData.Append(JsonConvert.SerializeObject(obj));
        initializeData.Append("\n");
      }
      if (country != null)
      {
        initializeData.Append(JsonConvert.SerializeObject(ApiData.From(country)));
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

      // オフラインになったことを通知
      await OnlineService.SetAsync(chara, OnlineStatus.Offline);
    }
    
    [HttpGet("anonymous")]
    public async Task AnonymousStreamingAsync()
    {
      SystemData system;
      IEnumerable<MapLog> maplogs;
      IEnumerable<MapLog> importantMaplogs;
      IEnumerable<CharacterUpdateLog> updateLogs;
      IEnumerable<TownForAnonymous> towns;
      IEnumerable<CountryForAnonymous> countries;
      IEnumerable<CountryMessage> countryMessages;
      IEnumerable<CharacterOnline> onlines = await OnlineService.GetAsync();
      using (var repo = MainRepository.WithRead())
      {
        system = await repo.System.GetAsync();
        maplogs = await repo.MapLog.GetNewestAsync(20);
        importantMaplogs = await repo.MapLog.GetImportantNewestAsync(20);
        updateLogs = await repo.Character.GetCharacterUpdateLogsAsync(20);
        towns = await repo.Town.GetAllForAnonymousAsync();
        countries = await repo.Country.GetAllForAnonymousAsync();
        countryMessages = await repo.Country.GetAllMessagesByTypeAsync(CountryMessageType.Solicitation);
      }

      // HTTPヘッダを設定する
      this.Response.Headers.Add("Content-Type", "text/event-stream; charset=UTF-8");
      this.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");

      // マップログをマージ
      var allMaplogs = importantMaplogs
        .Concat(maplogs
          .Where(im => !importantMaplogs.Any(ml => ml.Id == im.Id)))
        .OrderBy(im => im.Date);

      // 送信する初期データをリストアップ
      var sendData = Enumerable.Empty<object>()
        .Concat(new object[] { ApiData.From(system), })
        .Concat(allMaplogs.Select(ml => ApiData.From(ml)))
        .Concat(updateLogs.Select(ul => ApiData.From(ul)))
        .Concat(towns.Select(t => ApiData.From(t)))
        .Concat(countries.Select(c => ApiData.From(c)))
        .Concat(onlines.Select(o => ApiData.From(o)))
        .Concat(countryMessages.Select(c => ApiData.From(c)))
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
      AnonymousStreaming.Default.Add(this.Response, () => isRemoved = true);
      while (!isRemoved)
      {
        await Task.Delay(5000);
      }
    }
  }
}
