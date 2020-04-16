using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using SangokuKmy.Common;
using SangokuKmy.Filters;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Streamings;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [ServiceFilter(typeof(SangokuKmyErrorFilterAttribute))]
  [AuthenticationFilter]
  public class MuteController : Controller, IAuthenticationDataReceiver
  {
    public AuthenticationData AuthData { private get; set; }

    [HttpPost("mutes")]
    public async Task AddMuteAsync([FromBody] Mute mute)
    {
      IList<ChatMessage> chats = null;
      IEnumerable<Character> admins = null;

      if (mute == null)
      {
        ErrorCode.LackOfParameterError.Throw();
      }
      if (mute.Type == MuteType.None)
      {
        await this.RemoveMuteAsync(mute);
        return;
      }
      if (!((mute.TargetCharacterId != 0 && mute.ChatMessageId == 0 && mute.ThreadBbsItemId == 0) ||
            (mute.TargetCharacterId == 0 && mute.ChatMessageId != 0 && mute.ThreadBbsItemId == 0) ||
            (mute.TargetCharacterId == 0 && mute.ChatMessageId == 0 && mute.ThreadBbsItemId != 0)))
      {
        ErrorCode.InvalidParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var mutes = await repo.Mute.GetCharacterAsync(chara.Id);

        var target = mutes.FirstOrDefault(m => m.Id == mute.Id ||
                                              (m.TargetCharacterId != 0 && m.TargetCharacterId == mute.TargetCharacterId) ||
                                              (m.ChatMessageId != 0 && m.ChatMessageId == mute.ChatMessageId) ||
                                              (m.ThreadBbsItemId != 0 && m.ThreadBbsItemId == mute.ThreadBbsItemId));
        if (target != null)
        {
          ErrorCode.MeaninglessOperationError.Throw();
        }

        var targetMessage = string.Empty;
        var targetCharacter = default(Character);
        var targetType = string.Empty;
        if (mute.ChatMessageId != 0)
        {
          var item = await repo.ChatMessage.GetByIdAsync(mute.ChatMessageId).GetOrErrorAsync(ErrorCode.InvalidOperationError);
          targetMessage = item.Message;
          targetCharacter = await repo.Character.GetByIdAsync(item.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
          targetType = item.Type == ChatMessageType.Global ? "全国宛" :
                       item.Type == ChatMessageType.OtherCountry ? "他国宛" :
                       item.Type == ChatMessageType.Private ? "個宛" :
                       item.Type == ChatMessageType.Promotion ? "登用" :
                       item.Type == ChatMessageType.PromotionAccepted ? "登用" :
                       item.Type == ChatMessageType.PromotionDenied ? "登用" :
                       item.Type == ChatMessageType.PromotionRefused ? "登用" :
                       item.Type == ChatMessageType.SelfCountry ? "国宛" : "未定義";
        }
        else if (mute.ThreadBbsItemId != 0)
        {
          var item = await repo.ThreadBbs.GetByIdAsync(mute.ThreadBbsItemId).GetOrErrorAsync(ErrorCode.InvalidOperationError);
          targetMessage = item.Text;
          targetCharacter = await repo.Character.GetByIdAsync(item.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
          targetType = item.Type == BbsType.CountryBbs ? "会議室" : "全国会議室";
        }

        mute.CharacterId = chara.Id;
        await repo.Mute.AddAsync(mute);
        await repo.SaveChangesAsync();

        if (mute.Type == MuteType.Reported)
        {
          admins = await repo.Character.GetAdministratorsAsync();
          chats = new List<ChatMessage>();
          foreach (var admin in admins)
          {
            chats.Add(await ChatService.PostChatMessageAsync(repo, new ChatMessage
            {
              Message = $"[r][s]【報告】[-s][-r]\n\nMute ID: {mute.Id}\n\n武将名: {targetCharacter?.Name}\n\n{targetType}: {targetMessage}\n\n追加でメッセージがある場合は、続けて個宛してください（右下の再送ボタンより送れます）",
            }, chara, ChatMessageType.Private, chara.Id, admin.Id));
          }
          await repo.SaveChangesAsync();
        }
      }

      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(mute), mute.CharacterId);
      if (admins != null && chats != null)
      {
        await StatusStreaming.Default.SendCharacterAsync(chats.Select(c => ApiData.From(c)), admins.Select(a => a.Id).Append(this.AuthData.CharacterId));
      }
    }

    [HttpDelete("mutes")]
    public async Task RemoveMuteAsync([FromBody] Mute mute)
    {
      IList<ChatMessage> chats = null;
      IEnumerable<Character> admins = null;

      if (mute == null)
      {
        ErrorCode.LackOfParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var mutes = await repo.Mute.GetCharacterAsync(chara.Id);

        var target = mutes.FirstOrDefault(m => m.Id == mute.Id ||
                                              (m.TargetCharacterId != 0 && m.TargetCharacterId == mute.TargetCharacterId) ||
                                              (m.ChatMessageId != 0 && m.ChatMessageId == mute.ChatMessageId) ||
                                              (m.ThreadBbsItemId != 0 && m.ThreadBbsItemId == mute.ThreadBbsItemId));
        if (target == null)
        {
          ErrorCode.InvalidOperationError.Throw();
        }

        mute = target;
        repo.Mute.Remove(target);

        if (target.Type == MuteType.Reported)
        {
          admins = await repo.Character.GetAdministratorsAsync();
          chats = new List<ChatMessage>();
          foreach (var admin in admins)
          {
            chats.Add(await ChatService.PostChatMessageAsync(repo, new ChatMessage
            {
              Message = $"[b][s]【報告解除】[-s][-b]\n\nMute ID: {mute.Id}",
            }, chara, ChatMessageType.Private, admin.Id, admin.Id));
          }
        }

        await repo.SaveChangesAsync();
      }

      mute.Type = MuteType.None;
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(mute), mute.CharacterId);
      if (admins != null && chats != null)
      {
        await StatusStreaming.Default.SendCharacterAsync(chats.Select(c => ApiData.From(c)), admins.Select(a => a.Id));
      }
    }

    [HttpPut("mutes/keywords")]
    public async Task SetMuteKeywordsAsync([FromBody] MuteKeyword keyword)
    {
      if (keyword == null)
      {
        ErrorCode.LackOfParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        keyword.CharacterId = this.AuthData.CharacterId;

        var old = await repo.Mute.GetCharacterKeywordAsync(chara.Id);
        if (old.HasData)
        {
          old.Data.Keywords = keyword.Keywords;
        }
        else
        {
          await repo.Mute.AddAsync(keyword);
        }

        await repo.SaveChangesAsync();
      }

      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(keyword), keyword.CharacterId);
    }
  }
}
