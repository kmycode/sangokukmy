using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  public class SafeOutCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.SafeOut;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      if (character.CountryId == 0)
      {
        await game.CharacterLogAsync($"国庫から金を搬出しようとしましたが、無所属は実行できません");
        return;
      }

      var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync($"ID: <num>{character.CountryId}</num> の国は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }

      var country = countryOptional.Data;
      if (country.HasOverthrown)
      {
        await game.CharacterLogAsync($"国庫から金を搬出しようとしましたが、あなたの所属国 <country>{country.Name}</country> はすでに滅亡しています");
        return;
      }

      var money = options.FirstOrDefault(o => o.Type == 2)?.NumberValue ?? 0;
      var targetId = options.FirstOrDefault(o => o.Type == 1)?.NumberValue ?? 0;

      var targetOptional = await repo.Character.GetByIdAsync((uint)targetId);
      if (!targetOptional.HasData || targetOptional.Data.HasRemoved)
      {
        await game.CharacterLogAsync($"国庫から金を搬出しようとしましたが、搬出先の武将がいないか、すでに削除されています");
        return;
      }
      if (targetOptional.Data.CountryId != character.CountryId)
      {
        await game.CharacterLogAsync($"国庫から金を搬出しようとしましたが、搬出先の武将は <country>{country.Name}</country> に所属していません");
        return;
      }

      if (country.SafeMoney < money)
      {
        money = country.SafeMoney;
      }
      country.SafeMoney -= money;
      targetOptional.Data.Money += money;
      character.Contribution += 30;
      character.AddIntellectEx(50);
      character.SkillPoint++;

      await game.CharacterLogAsync($"国庫から金 <num>{money}</num> を搬出し、<character>{targetOptional.Data.Name}</character> に与えました");
      await game.CharacterLogByIdAsync(targetOptional.Data.Id, $"<character>{character.Name}</character> より、国庫から金 <num>{money}</num> を受け取りました");
      await StatusStreaming.Default.SendCountryAsync(ApiData.From(country), country.Id);
      return;
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
      var country = await repo.Country.GetAliveByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
      var money = options.FirstOrDefault(o => o.Type == 2).Or(ErrorCode.LackOfParameterError).NumberValue;
      var target = options.FirstOrDefault(o => o.Type == 1).Or(ErrorCode.LackOfParameterError).NumberValue;
      var targetChara = await repo.Character.GetByIdAsync((uint)target).GetOrErrorAsync(ErrorCode.InvalidCommandParameter);

      if (money <= 0 || money > Config.PaySafeMax)
      {
        ErrorCode.InvalidParameterError.Throw();
      }

      var myPosts = await repo.Country.GetCharacterPostsAsync(characterId);
      if (!myPosts.Any(p => p.Type.CanSafe()))
      {
        ErrorCode.NotPermissionError.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
