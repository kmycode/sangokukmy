using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SangokuKmy.Common;
using SangokuKmy.Filters;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
using SangokuKmy.Models.Services;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [ServiceFilter(typeof(SangokuKmyErrorFilterAttribute))]
  [AuthenticationFilter]
  public class ChatController : Controller, IAuthenticationDataReceiver
  {
    public AuthenticationData AuthData { private get; set; }

    [AuthenticationFilter]
    [HttpGet("chat/country")]
    public async Task<ApiArrayData<ChatMessage>> GetCountryChatMessagesAsync(
      [FromQuery] uint sinceId = default,
      [FromQuery] int count = default)
    {
      IEnumerable<ChatMessage> messages;
      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        messages = await repo.ChatMessage.GetCountryMessagesAsync(chara.CountryId, sinceId, count);
      }
      return ApiData.From(messages);
    }

    [AuthenticationFilter]
    [HttpPost("chat/country")]
    public async Task PostCountryChatMessageAsync(
      [FromBody] ChatMessage param)
    {
      ChatMessage message;
      Character chara;

      if (string.IsNullOrEmpty(param.Message) || param.Message?.Length > 400)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("message", param.Message.Length, 1, 400));
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        message = await ChatService.PostChatMessageAsync(repo, param, chara, ChatMessageType.SelfCountry, chara.CountryId);
        await repo.SaveChangesAsync();
      }

      await StatusStreaming.Default.SendCountryAsync(ApiData.From(message), chara.CountryId);
    }

    [AuthenticationFilter]
    [HttpGet("chat/global")]
    public async Task<ApiArrayData<ChatMessage>> GetGlobalChatMessagesAsync(
      [FromQuery] uint sinceId = default,
      [FromQuery] int count = default)
    {
      IEnumerable<ChatMessage> messages;
      using (var repo = MainRepository.WithRead())
      {
        messages = await repo.ChatMessage.GetGlobalMessagesAsync(sinceId, count);
      }
      return ApiData.From(messages);
    }

    [AuthenticationFilter]
    [HttpPost("chat/global")]
    public async Task PostGlobalChatMessageAsync(
      [FromBody] ChatMessage param)
    {
      ChatMessage message;
      Character chara;

      if (string.IsNullOrEmpty(param.Message) || param.Message?.Length > 400)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("message", param.Message.Length, 1, 400));
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        message = await ChatService.PostChatMessageAsync(repo, param, chara, ChatMessageType.Global);
        await repo.SaveChangesAsync();
      }

      await StatusStreaming.Default.SendAllAsync(ApiData.From(message));
    }

    [AuthenticationFilter]
    [HttpGet("chat/character")]
    public async Task<ApiArrayData<ChatMessage>> GetPrivateChatMessagesAsync(
      [FromQuery] uint sinceId = default,
      [FromQuery] int count = default)
    {
      IEnumerable<ChatMessage> messages;
      using (var repo = MainRepository.WithRead())
      {
        messages = await repo.ChatMessage.GetPrivateMessagesAsync(this.AuthData.CharacterId, sinceId, count);
      }
      return ApiData.From(messages);
    }

    [AuthenticationFilter]
    [HttpPost("chat/character/{id}")]
    public async Task PostPrivateChatMessageAsync(
      [FromRoute] uint id,
      [FromBody] ChatMessage param)
    {
      ChatMessage message;
      Character chara;

      if (string.IsNullOrEmpty(param.Message) || param.Message?.Length > 400)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("message", param.Message.Length, 1, 400));
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var to = await repo.Character.GetByIdAsync(id).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        if (chara.Id == to.Id)
        {
          // 自分に対して個人宛は送れない
          ErrorCode.MeaninglessOperationError.Throw();
        }

        message = await ChatService.PostChatMessageAsync(repo, param, chara, ChatMessageType.Private, chara.Id, id);
        await repo.SaveChangesAsync();
      }

      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(message), new uint[] { chara.Id, id, });
    }

    [AuthenticationFilter]
    [HttpPost("chat/country/{id}")]
    public async Task PostOtherCountryChatMessageAsync(
      [FromRoute] uint id,
      [FromBody] ChatMessage param)
    {
      ChatMessage message;
      Character chara;

      if (string.IsNullOrEmpty(param.Message) || param.Message?.Length > 400)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("message", param.Message.Length, 1, 400));
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var to = await repo.Country.GetAliveByIdAsync(id).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var country = await repo.Country.GetAliveByIdAsync(id).GetOrErrorAsync(ErrorCode.CountryNotFoundError);

        var posts = (await repo.Country.GetPostsAsync(chara.CountryId)).Where(p => p.CharacterId == chara.Id);
        if (!posts.Any(p => p.Type == CountryPostType.Monarch || p.Type == CountryPostType.Warrior))
        {
          ErrorCode.NotPermissionError.Throw();
        }

        message = await ChatService.PostChatMessageAsync(repo, param, chara, ChatMessageType.OtherCountry, chara.CountryId, id);
        await repo.SaveChangesAsync();
      }

      await StatusStreaming.Default.SendCountryAsync(ApiData.From(message), chara.CountryId);
      await StatusStreaming.Default.SendCountryAsync(ApiData.From(message), id);
    }

    /// <summary>
    /// 登用文を承諾または拒否する
    /// </summary>
    [HttpPost("chat/promotion")]
    public async Task SetPromotionStatusAsync(
      [FromBody] ChatMessage message)
    {
      Character newCharacter = null;
      CharacterLog charalog = null;
      CharacterLog senderCharalog = null;
      MapLog maplog = null;
      Town newTown = null;
      Town oldTown = null;
      CountryMessage commanders = null;
      IEnumerable<uint> oldTownCharacters = null;
      IEnumerable<uint> newTownCharacters = null;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var system = await repo.System.GetAsync();
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        var old = await repo.ChatMessage.GetByIdAsync(message.Id).GetOrErrorAsync(ErrorCode.InvalidParameterError);
        var country = await repo.Country.GetAliveByIdAsync(chara.CountryId);

        if (country.HasData)
        {
          // 無所属以外は実行できない
          ErrorCode.InvalidOperationError.Throw();
        }
        if (old.Type != ChatMessageType.Promotion)
        {
          // 登用文ではない
          ErrorCode.InvalidOperationError.Throw();
        }

        if (message.Type == ChatMessageType.PromotionAccepted)
        {
          var sender = await repo.Character.GetByIdAsync(old.TypeData).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);

          if (sender.CountryId != old.CharacterCountryId)
          {
            // 登用出した人の国が滅亡している
            ErrorCode.CountryNotFoundError.Throw();
          }

          var senderCountry = await repo.Country.GetAliveByIdAsync(sender.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
          oldTown = await repo.Town.GetByIdAsync(chara.TownId).GetOrErrorAsync(ErrorCode.TownNotFoundError);
          newTown = await repo.Town.GetByIdAsync(senderCountry.CapitalTownId).GetOrErrorAsync(ErrorCode.TownNotFoundError);
          oldTownCharacters = (await repo.Town.GetCharactersWithIconAsync(oldTown.Id)).Select(c => c.Character.Id);
          newTownCharacters = (await repo.Town.GetCharactersWithIconAsync(newTown.Id)).Select(c => c.Character.Id);
          commanders = (await repo.Country.GetMessageAsync(sender.CountryId, CountryMessageType.Commanders)).Data;

          var reinforcements = await repo.Reinforcement.GetByCharacterIdAsync(chara.Id);
          if (reinforcements.Any(r => r.Status == ReinforcementStatus.Active))
          {
            ErrorCode.NotPermissionError.Throw();
          }

          await CharacterService.ChangeTownAsync(repo, senderCountry.CapitalTownId, chara);
          await CharacterService.ChangeCountryAsync(repo, senderCountry.Id, new Character[] { chara, });

          charalog = new CharacterLog
          {
            CharacterId = chara.Id,
            DateTime = DateTime.Now,
            GameDateTime = system.GameDateTime,
            Message = $"<character>{sender.Name}</character> の登用に応じ、 <country>{senderCountry.Name}</country> に仕官しました",
          };
          senderCharalog = new CharacterLog
          {
            CharacterId = old.TypeData,
            DateTime = DateTime.Now,
            GameDateTime = system.GameDateTime,
            Message = $"<character>{chara.Name}</character> があなたの登用に応じ、 <country>{senderCountry.Name}</country> に仕官しました",
          };
          maplog = new MapLog
          {
            Date = DateTime.Now,
            ApiGameDateTime = system.GameDateTime,
            IsImportant = false,
            EventType = EventType.PromotionAccepted,
            Message = $"<character>{chara.Name}</character> は <country>{senderCountry.Name}</country> に仕官しました",
          };

          old.Character = new CharacterChatData(sender);
          old.ReceiverName = chara.Name;
          old.Type = message.Type;
          newCharacter = chara;
        }
        else if (message.Type == ChatMessageType.PromotionRefused)
        {
          var sender = await repo.Character.GetByIdAsync(old.TypeData).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);

          charalog = new CharacterLog
          {
            CharacterId = chara.Id,
            DateTime = DateTime.Now,
            GameDateTime = system.GameDateTime,
            Message = $"<character>{sender.Name}</character> の登用を断りました",
          };
          senderCharalog = new CharacterLog
          {
            CharacterId = old.TypeData,
            DateTime = DateTime.Now,
            GameDateTime = system.GameDateTime,
            Message = $"<character>{chara.Name}</character> は、あなたの登用を断りました",
          };

          old.Character = new CharacterChatData(sender);
          old.ReceiverName = chara.Name;
          old.Type = message.Type;
        }
        else
        {
          ErrorCode.InvalidParameterError.Throw();
        }

        if (maplog != null)
        {
          await repo.MapLog.AddAsync(maplog);
        }
        if (charalog != null)
        {
          await repo.Character.AddCharacterLogAsync(charalog);
        }
        if (senderCharalog != null)
        {
          await repo.Character.AddCharacterLogAsync(senderCharalog);
        }

        message = old;
        await repo.SaveChangesAsync();
      }

      if (newCharacter != null)
      {
        StatusStreaming.Default.UpdateCache(new Character[] { newCharacter, });
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(newCharacter), newCharacter.Id);
      }
      if (charalog != null)
      {
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(charalog), charalog.CharacterId);
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(message), charalog.CharacterId);
      }
      if (senderCharalog != null)
      {
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(senderCharalog), senderCharalog.CharacterId);
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(message), senderCharalog.CharacterId);
      }
      if (maplog != null)
      {
        await StatusStreaming.Default.SendAllAsync(ApiData.From(maplog));
        await AnonymousStreaming.Default.SendAllAsync(ApiData.From(maplog));
      }
    }
  }
}
