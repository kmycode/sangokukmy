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

namespace SangokuKmy.Models.Commands
{
  public class RiceCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Rice;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var typeParameter = options.FirstOrDefault(p => p.Type == 1);
      var assetsParameter = options.FirstOrDefault(p => p.Type == 2);
      var resultParameter = options.FirstOrDefault(p => p.Type == 3);
      if (typeParameter == null || assetsParameter == null || resultParameter == null)
      {
        await game.CharacterLogAsync($"米売買に必要なパラメータ <num>{string.Join(",", Enumerable.Range(1, 3).Concat(options.Select(p => p.Type)).Distinct())}</num> が足りません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }

      var type = (RiceCommandType)typeParameter.NumberValue;
      var assets = (int)assetsParameter.NumberValue;
      var result = (int)resultParameter.NumberValue;

      if (type == RiceCommandType.MoneyToRice)
      {
        if (character.Money < assets)
        {
          await game.CharacterLogAsync($"<num>{assets}</num> の金を米に交換しようとしましたが、金が足りませんでした");
          return;
        }
        character.Money -= assets;
        character.Rice += result;
        await game.CharacterLogAsync($"<num>{assets}</num> の金を <num>{result}</num> の米に交換しました");
      }
      else if (type == RiceCommandType.RiceToMoney)
      {
        if (character.Rice < assets)
        {
          await game.CharacterLogAsync($"<num>{assets}</num> の米を金に交換しようとしましたが、米が足りませんでした");
          return;
        }
        character.Rice -= assets;
        character.Money += result;
        await game.CharacterLogAsync($"<num>{assets}</num> の米を <num>{result}</num> の金に交換しました");
      }

      character.AddIntellectEx(50);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
      var town = await repo.Town.GetByIdAsync(chara.TownId).GetOrErrorAsync(ErrorCode.InternalDataNotFoundError, new { command = "rice", townId = chara.TownId, });
      var type = (RiceCommandType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var assets = (int)options.FirstOrDefault(p => p.Type == 2).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      
      if (assets <= 0 || assets > Config.RiceBuyMax)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("assets", assets, 1, Config.RiceBuyMax));
      }

      int result;
      if (type == RiceCommandType.MoneyToRice)
      {
        result = (int)((2 - town.RicePrice) * assets);
      }
      else if (type == RiceCommandType.RiceToMoney)
      {
        result = (int)(town.RicePrice * assets);
      }
      else
      {
        ErrorCode.InvalidParameterError.Throw();
        return;
      }

      var optionsWithResult = options.Where(o => o.Type == 1 || o.Type == 2).ToList();
      optionsWithResult.Add(new CharacterCommandParameter
      {
        Type = 3,
        NumberValue = result,
      });

      // 入力
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, optionsWithResult.ToArray());
    }

    private enum RiceCommandType : int
    {
      MoneyToRice = 1,
      RiceToMoney = 2,
    }
  }
}
