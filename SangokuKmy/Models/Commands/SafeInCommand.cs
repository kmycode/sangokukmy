using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Models.Common;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using System.Linq;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  public class SafeInCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.SafeIn;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      if (character.CountryId == 0)
      {
        await game.CharacterLogAsync($"国庫に金を納入しようとしましたが、無所属は実行できません");
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
        await game.CharacterLogAsync($"国庫に金を納入しようとしましたが、あなたの所属国 <country>{country.Name}</country> はすでに滅亡しています");
        return;
      }

      var policies = await repo.Country.GetPoliciesAsync(country.Id);

      var max = Config.CountrySafeMax;
      if (policies.Any(p => p.Type == CountryPolicyType.UndergroundStorage))
      {
        max *= 2;
      }

      var money = options.FirstOrDefault(o => o.Type == 1)?.NumberValue ?? 0;
      if (money > character.Money)
      {
        await game.CharacterLogAsync($"国庫に金 <num>{money}</num> を納入しようとしましたが、所持金が足りません");
        return;
      }

      if (country.SafeMoney + money > max)
      {
        money = max - country.SafeMoney;
        if (money < 0)
        {
          // 支配とか災害とかで上限が下がることがある
          money = 0;
        }
      }
      country.SafeMoney += money;
      character.Money -= money;
      character.Contribution += money / 1000;

      await game.CharacterLogAsync($"国庫に金 <num>{money}</num> を納入しました");
      await StatusStreaming.Default.SendCountryAsync(ApiData.From(country), country.Id);
      return;
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
      var country = await repo.Country.GetAliveByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
      var money = options.FirstOrDefault(o => o.Type == 1).Or(ErrorCode.LackOfParameterError).NumberValue;

      if (money <= 0 || money > Config.PaySafeMax)
      {
        ErrorCode.InvalidParameterError.Throw();
      }
      if (!(await repo.Country.GetPoliciesAsync(country.Id)).Any(p => p.Type == CountryPolicyType.Storage))
      {
        ErrorCode.InvalidOperationError.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
