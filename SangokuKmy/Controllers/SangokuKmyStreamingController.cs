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
using System.Collections;

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
      IEnumerable<CharacterForAnonymous> allCharas;
      IEnumerable<MapLog> maplogs;
      IEnumerable<MapLog> importantMaplogs;
      IEnumerable<CharacterLog> characterLogs;
      IEnumerable<CountryForAnonymous> countries;
      IEnumerable<TownForAnonymous> towns;
      IEnumerable<Town> myTowns;
      IEnumerable<TownDefender> defenders;
      IEnumerable<TownSubBuilding> subBuildings;
      IEnumerable<ScoutedTown> scoutedTowns;
      IEnumerable<ChatMessage> chatMessages;
      IEnumerable<CountryAlliance> alliances;
      IEnumerable<CountryWar> wars;
      IEnumerable<TownWar> townWars;
      IEnumerable<CountryPolicy> policies;
      IEnumerable<ThreadBbsItem> countryBbsItems;
      IEnumerable<ThreadBbsItem> globalBbsItems;
      IEnumerable<Reinforcement> reinforcements;
      IEnumerable<CharacterOnline> onlines = await OnlineService.GetAsync();
      IEnumerable<CountryMessage> countryMessages;
      IEnumerable<Formation> formations;
      IEnumerable<CharacterItem> items;
      IEnumerable<CharacterSkill> skills;
      IEnumerable<CommandMessage> commandMessages;
      IEnumerable<CharacterCommand> otherCharacterCommands;
      IEnumerable<AiCharacterManagement> aiCharacterManagements;
      IEnumerable<DelayEffect> delayEffects;
      IEnumerable<Mute> mutes;
      IEnumerable<CharacterRegularlyCommand> regularlyCommands;
      MuteKeyword muteKeyword;
      ChatMessageRead read;
      BlockAction blockAction;
      ApiSignal onlineInfo = null;
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
          .Concat(await repo.ChatMessage.GetGlobalMessagesAsync(uint.MaxValue, 0, 50))
          .Concat(await repo.ChatMessage.GetGlobalMessagesAsync(uint.MaxValue, 1, 50))
          .Concat(await repo.ChatMessage.GetPrivateMessagesAsync(chara.Id, uint.MaxValue, 50))
          .Concat(await repo.ChatMessage.GetPromotionMessagesAsync(chara.Id, uint.MaxValue, 50));
        alliances = (await repo.CountryDiplomacies.GetAllPublicAlliancesAsync())
          .Concat(await repo.CountryDiplomacies.GetCountryAllAlliancesAsync(chara.CountryId));
        wars = await repo.CountryDiplomacies.GetAllWarsAsync();
        townWars = await repo.CountryDiplomacies.GetAllTownWarsAsync();
        policies = await repo.Country.GetPoliciesAsync(country?.Id ?? 0);
        countryBbsItems = await repo.ThreadBbs.GetCountryBbsByCountryIdAsync(chara.CountryId);
        globalBbsItems = await repo.ThreadBbs.GetGlobalBbsAsync();
        reinforcements = await repo.Reinforcement.GetByCharacterIdAsync(chara.Id);
        countryMessages = await repo.Country.GetMessagesAsync(chara.CountryId);
        defenders = (await repo.Town.GetAllDefendersAsync()).OrderByDescending(d => d.Id);
        formations = await repo.Character.GetFormationsAsync(chara.Id);
        items = await repo.CharacterItem.GetAllAsync();
        skills = await repo.Character.GetSkillsAsync(chara.Id);
        commandMessages = await repo.CharacterCommand.GetMessagesAsync(chara.CountryId);
        aiCharacterManagements = await repo.Character.GetManagementByHolderCharacterIdAsync(chara.Id);
        delayEffects = await repo.DelayEffect.GetByCharacterIdAsync(chara.Id);
        mutes = await repo.Mute.GetCharacterAsync(chara.Id);
        muteKeyword = (await repo.Mute.GetCharacterKeywordAsync(chara.Id)).Data ?? new MuteKeyword
        {
          Keywords = string.Empty,
        };
        read = await repo.ChatMessage.GetReadByCharacterIdAsync(chara.Id);
        blockAction = (await repo.BlockAction.GetAsync(chara.Id, BlockActionType.StopCommandByMonarch)).Data;
        regularlyCommands = await repo.CharacterCommand.GetRegularlyCommandsAsync(chara.Id);

        var countryCharacters = await repo.Country.GetCharactersWithIconsAndCommandsAsync(chara.CountryId);
        otherCharacterCommands = countryCharacters.SelectMany(c => c.Commands);

        var allTowns = await repo.Town.GetAllAsync();
        towns = allTowns.Select(tw => new TownForAnonymous(tw));
        myTowns = allTowns.Where(tw => tw.CountryId == chara.CountryId || chara.TownId == tw.Id);
        scoutedTowns = await repo.ScoutedTown.GetByScoutedCountryIdAsync(chara.CountryId);
        subBuildings = (await repo.Town.GetSubBuildingsAsync()).Where(s => myTowns.Any(ms => ms.Id == s.TownId));

        var allCharasData = await repo.Character.GetAllAliveWithIconAsync();
        allCharas = allCharasData.Where(c => c.Character.Id != chara.Id).Where(c => c.Character.CountryId == chara.CountryId || c.Character.AiType != CharacterAiType.SecretaryScouter).Select(c =>
          new CharacterForAnonymous(c.Character, c.Icon,
            (c.Character.CountryId == chara.CountryId && c.Character.TownId == chara.TownId) ? CharacterShareLevel.SameTownAndSameCountry :
            c.Character.CountryId == chara.CountryId ? CharacterShareLevel.SameCountry :
            c.Character.TownId == chara.TownId ? CharacterShareLevel.SameTown :
            myTowns.Any(t => c.Character.TownId == t.Id) ? CharacterShareLevel.SameCountryTownOtherCountry :
            CharacterShareLevel.Anonymous));

        if (chara.CountryId != 0)
        {
          var posts = await repo.Country.GetPostsAsync(chara.CountryId);
          if (!posts.Any(p => p.Type == CountryPostType.Monarch && p.CharacterId == chara.Id))
          {
            countryMessages = countryMessages.Where(m => m.Type != CountryMessageType.Unified);
          }
        }

        // オンライン状態の通知
        var onlineData = await repo.Character.CountAllCharacterOnlineMonthAsync();
        var myOnline = onlineData.FirstOrDefault(h => h.CharacterId == chara.Id);
        if (myOnline.CharacterId != default)
        {
          var rank = onlineData
            .OrderByDescending(h => h.Count)
            .Select((h, i) => new { h.CharacterId, h.Count, Rank = i + 1, })
            .FirstOrDefault(h => h.CharacterId == chara.Id);
          if (rank != null)
          {
            onlineInfo = new ApiSignal
            {
              Type = SignalType.CharacterOnline,
              Data = new { count = rank.Count, rank = rank.Rank, },
            };
          }
        }
      }

      // HTTPヘッダを設定する
      this.Response.Headers.Add("Content-Type", "text/event-stream; charset=UTF-8");
      this.Response.Headers.Add("Cache-Control", "no-cache, no-store, must-revalidate");

      // 送信する初期データをリストアップ
      var sendData = Enumerable.Empty<object>()
        .Concat(new object[]
        {
          ApiData.From(system),
          ApiData.From(chara),
        })
        .Concat(allCharas.Select(c => ApiData.From(c)))
        .Concat(maplogs.Select(ml => ApiData.From(ml)))
        //.Concat(importantMaplogs.Select(ml => ApiData.From(ml)))
        .Concat(characterLogs.Select(cl => ApiData.From(cl)))
        .Concat(countries.Select(c => ApiData.From(c)))
        .Concat(towns.Select(tw => ApiData.From(tw)))
        .Concat(myTowns.Select(tw => ApiData.From(tw)))
        .Concat(scoutedTowns.Select(st => ApiData.From(st)))
        .Concat(defenders.Where(d => myTowns.Any(t => t.Id == d.TownId) || d.TownId == chara.TownId).Select(d => ApiData.From(d)))
        .Concat(mutes.Select(m => ApiData.From(m)))
        .Append(ApiData.From(muteKeyword))
        .Concat(chatMessages.Select(cm => ApiData.From(cm)))
        .Concat(alliances.Select(ca => ApiData.From(ca)))
        .Concat(wars.Select(cw => ApiData.From(cw)))
        .Concat(townWars.Select(tw => ApiData.From(tw)))
        .Concat(policies.Select(p => ApiData.From(p)))
        .Concat(countryBbsItems.Select(b => ApiData.From(b)))
        .Concat(globalBbsItems.Select(b => ApiData.From(b)))
        .Concat(reinforcements.Select(r => ApiData.From(r)))
        .Concat(onlines.Select(o => ApiData.From(o)))
        .Concat(countryMessages.Select(c => ApiData.From(c)))
        .Concat(formations.Select(f => ApiData.From(f)))
        .Concat(items.Select(i => ApiData.From(i)))
        .Concat(skills.Select(s => ApiData.From(s)))
        .Concat(commandMessages.Select(m => ApiData.From(m)))
        .Concat(otherCharacterCommands.Select(c => ApiData.From(c)))
        .Concat(aiCharacterManagements.Select(m => ApiData.From(m)))
        .Concat(delayEffects.Select(d => ApiData.From(d)))
        .Concat(subBuildings.Select(s => ApiData.From(s)))
        .Concat(regularlyCommands.Select(c => ApiData.From(c)))
        .ToList();
      if (onlineInfo != null)
      {
        sendData.Add(ApiData.From(onlineInfo));
      }
      if (blockAction != null)
      {
        sendData.Add(ApiData.From(new ApiSignal
        {
          Type = SignalType.StopCommand,
        }));
      }
      sendData.Add(ApiData.From(new ApiSignal
      {
        Type = SignalType.EndOfStreamingInitializeData,
      }));
      sendData.Add(ApiData.From(read));

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
