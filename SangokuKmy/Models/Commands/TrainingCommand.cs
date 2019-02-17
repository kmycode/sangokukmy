using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Commands
{
  /// <summary>
  /// 能力強化
  /// </summary>
  public class TrainingCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Training;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var trainingTypeOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();

      if (!trainingTypeOptional.HasData)
      {
        await game.CharacterLogAsync("能力強化のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
      }
      else if (character.Money < 50)
      {
        await game.CharacterLogAsync("能力強化の金が足りません");
      }
      else
      {
        var trainingType = (TrainingType)trainingTypeOptional.Data.NumberValue;
        var name = string.Empty;
        var isError = false;

        switch (trainingType)
        {
          case TrainingType.Strong:
            character.AddStrongEx(100);
            name = "武力";
            break;
          case TrainingType.Intellect:
            character.AddIntellectEx(100);
            name = "知力";
            break;
          case TrainingType.Leadership:
            character.AddLeadershipEx(100);
            name = "統率";
            break;
          case TrainingType.Popularity:
            character.AddPopularityEx(100);
            name = "人望";
            break;
          default:
            await game.CharacterLogAsync("能力強化のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
            isError = true;
            break;
        }

        if (!isError)
        {
          character.Money -= 50;
          await game.CharacterLogAsync(name + "経験値 を <num>+100</num> 強化しました");
        }
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var trainingType = (TrainingType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      if ((int)trainingType < 1 || (int)trainingType > 4)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }

  public enum TrainingType : int
  {
    Strong = 1,
    Intellect = 2,
    Leadership = 3,
    Popularity = 4,
  }
}
