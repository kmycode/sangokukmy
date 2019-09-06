using System;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Services
{
  public static class ChatService
  {
    public static async Task<ChatMessage> PostChatMessageAsync(MainRepository repo, ChatMessage param, Character chara, ChatMessageType type, uint typeData = default, uint typeData2 = default)
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

      if (message.Type == ChatMessageType.Private || message.Type == ChatMessageType.Promotion || message.Type == ChatMessageType.PromotionRefused || message.Type == ChatMessageType.PromotionAccepted || message.Type == ChatMessageType.PromotionDenied)
      {
        var receiver = await repo.Character.GetByIdAsync(message.TypeData2).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        message.ReceiverName = receiver.Name;
      }
      else if (message.Type == ChatMessageType.OtherCountry)
      {
        var receiver = await repo.Country.GetByIdAsync(message.TypeData2).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        message.ReceiverName = receiver.Name;
      }

      await repo.ChatMessage.AddMessageAsync(message);
      return message;
    }

    public static async Task DenyCountryPromotions(MainRepository repo, Country country)
    {
      var promotions = (await repo.ChatMessage.GetPromotionMessagesAsync(country.Id)).Where(p => p.Type == ChatMessageType.Promotion);
      foreach (var p in promotions)
      {
        p.Type = ChatMessageType.PromotionDenied;
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(p), p.TypeData);
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(p), p.TypeData2);
      }
    }
  }
}
