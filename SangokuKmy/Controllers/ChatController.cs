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

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [SangokuKmyErrorFilter]
  [AuthenticationFilter]
  public class ChatController : Controller, IAuthenticationDataReceiver
  {
    public AuthenticationData AuthData { private get; set; }

    [AuthenticationFilter]
    [HttpGet("chat/country")]
    public async Task<ApiArrayData<ChatMessage>> GetCountryChatMessagesAsync(
      [FromBody] GetChatMessageParameter param)
    {
      IEnumerable<ChatMessage> messages;
      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        messages = await repo.ChatMessage.GetCountryMessagesAsync(chara.CountryId, param.SinceId, param.Count);
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

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        message = await this.PostChatMessageAsync(repo, param, chara, ChatMessageType.SelfCountry, chara.CountryId);
        await repo.SaveChangesAsync();
      }

      await StatusStreaming.Default.SendCountryAsync(ApiData.From(message), chara.CountryId);
    }

    [AuthenticationFilter]
    [HttpGet("chat/global")]
    public async Task<ApiArrayData<ChatMessage>> GetGlobalChatMessagesAsync(
      [FromBody] GetChatMessageParameter param)
    {
      IEnumerable<ChatMessage> messages;
      using (var repo = MainRepository.WithRead())
      {
        messages = await repo.ChatMessage.GetGlobalMessagesAsync(param.SinceId, param.Count);
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

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        message = await this.PostChatMessageAsync(repo, param, chara, ChatMessageType.Global);
        await repo.SaveChangesAsync();
      }

      await StatusStreaming.Default.SendAllAsync(ApiData.From(message));
    }

    [AuthenticationFilter]
    [HttpPost("chat/character/{id}")]
    public async Task PostCharacterChatMessageAsync(
      [FromRoute] uint id,
      [FromBody] ChatMessage param)
    {
      ChatMessage message;
      Character chara;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var to = await repo.Character.GetByIdAsync(id).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);

        message = await this.PostChatMessageAsync(repo, param, chara, ChatMessageType.Private, chara.Id, id);
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

        message = await this.PostChatMessageAsync(repo, param, chara, ChatMessageType.OtherCountry, chara.CountryId, id);
        await repo.SaveChangesAsync();
      }

      await StatusStreaming.Default.SendCountryAsync(ApiData.From(message), chara.CountryId);
      await StatusStreaming.Default.SendCountryAsync(ApiData.From(message), id);
    }

    public struct GetChatMessageParameter
    {
      [JsonProperty("sinceId")]
      public uint SinceId { get; set; }
      [JsonProperty("count")]
      public int Count { get; set; }
    }

    private async Task<ChatMessage> PostChatMessageAsync(MainRepository repo, ChatMessage param, Character chara, ChatMessageType type, uint typeData = default, uint typeData2 = default)
    {
      ChatMessage message;

      message = new ChatMessage
      {
        CharacterId = chara.Id,
        Character = new CharacterChatData(chara),
        CharacterCountryId = chara.CountryId,
        Posted = DateTime.Now,
        Message = param.Message,
        Type = type,
        TypeData = typeData,
        TypeData2 = typeData2,
      };
      if (param.CharacterIconId > 0)
      {
        message.CharacterIconId = param.CharacterIconId;
        message.CharacterIcon = await repo.Character.GetCharacterIconByIdAsync(message.CharacterIconId);
      }
      else
      {
        var icons = await repo.Character.GetCharacterAllIconsAsync(chara.Id);
        var icon = icons.FirstOrDefault(i => i.IsMain) ?? icons.FirstOrDefault();
        if (icon == null)
        {
          ErrorCode.CharacterIconNotFoundError.Throw();
        }
        message.CharacterIconId = icon.Id;
        message.CharacterIcon = icon;
      }

      await repo.ChatMessage.AddMessageAsync(message);
      return message;
    }
  }
}
