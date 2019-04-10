using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  public class PromotionCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Promotion;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var targetCharacterIdParameter = options.FirstOrDefault(p => p.Type == 1);
      var messageParameter = options.FirstOrDefault(p => p.Type == 2);

      if (targetCharacterIdParameter == null || messageParameter == null)
      {
        await game.CharacterLogAsync("登用のパラメータが足りません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }

      var targetCharacterOptional = await repo.Character.GetByIdAsync((uint)targetCharacterIdParameter.NumberValue);
      if (!targetCharacterOptional.HasData)
      {
        await game.CharacterLogAsync($"ID: {targetCharacterIdParameter.NumberValue} の武将は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      if (targetCharacterOptional.Data.HasRemoved)
      {
        await game.CharacterLogAsync($"<character>{targetCharacterOptional.Data.Name}</character> に登用を送ろうとしましたが、すでに放置削除されています");
        return;
      }

      var targetCountryOptional = await repo.Country.GetAliveByIdAsync(targetCharacterOptional.Data.CountryId);
      if (targetCountryOptional.HasData)
      {
        await game.CharacterLogAsync($"<character>{targetCharacterOptional.Data.Name}</character> に登用を送ろうとしましたが、すでに <country>{targetCountryOptional.Data.Name}</country> に仕官しています");
        return;
      }

      var iconOptional = (await repo.Character.GetCharacterAllIconsAsync(character.Id)).GetMainOrFirst();
      if (!iconOptional.HasData)
      {
        await game.CharacterLogAsync($"<character>{targetCharacterOptional.Data.Name}</character> に登用を送ろうとしましたが、あなたのメインアイコンが設定されていません");
        return;
      }

      character.Contribution += 30;
      character.AddPopularityEx(50);
      var message = await ChatService.PostChatMessageAsync(repo, new ChatMessage
      {
        CharacterIconId = iconOptional.Data.Id,
        Message = messageParameter.StringValue,
      }, character, ChatMessageType.Promotion, character.Id, targetCharacterOptional.Data.Id);

      await game.CharacterLogAsync($"<character>{targetCharacterOptional.Data.Name}</character> に登用を送りました");

      // ストリーミングするデータに登用文のIDが必要
      await repo.SaveChangesAsync();
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(message), targetCharacterOptional.Data.Id);
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(message), character.Id);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
      var targetCharacterId = options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfParameterError).NumberValue;
      var message = options.FirstOrDefault(p => p.Type == 2).Or(ErrorCode.LackOfParameterError).StringValue;

      if (string.IsNullOrWhiteSpace(message))
      {
        ErrorCode.LackOfCommandParameter.Throw();
      }

      if (message.Length > 400)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("message", message.Length, 1, 400));
      }

      var country = await repo.Country.GetAliveByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
      var targetCharacter = await repo.Character.GetByIdAsync((uint)targetCharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
      if (targetCharacter.HasRemoved)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }
      if (targetCharacter.CountryId > 0)
      {
        var targetCountry = await repo.Country.GetAliveByIdAsync(targetCharacter.CountryId);
        if (targetCountry.HasData)
        {
          ErrorCode.InvalidOperationError.Throw();
        }
      }

      // 入力
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
