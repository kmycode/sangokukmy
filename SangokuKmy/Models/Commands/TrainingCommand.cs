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
      var system = await repo.System.GetAsync();

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

        if (trainingType == TrainingType.SkillPoint)
        {
          if (character.CountryId != 0)
          {
            await game.CharacterLogAsync("国に仕官している人は技能ポイントを強化できません");
            isError = true;
          }

          if (system.GameDateTime.Year >= Config.UpdateStartYear + Config.CountryBattleStopDuring / 12)
          {
            await game.CharacterLogAsync("戦闘解除後は技能ポイントを強化できません");
            isError = true;
          }
        }
        if (isError)
        {
          return;
        }

        if (trainingType == TrainingType.Any)
        {
          if (character.Strong > character.Intellect && character.Strong > character.Leadership && character.Strong > character.Popularity)
          {
            trainingType = TrainingType.Strong;
          }
          else if (character.Intellect > character.Leadership && character.Intellect > character.Popularity)
          {
            trainingType = TrainingType.Intellect;
          }
          else if (character.Leadership > character.Popularity)
          {
            trainingType = TrainingType.Leadership;
          }
          else
          {
            trainingType = TrainingType.Popularity;
          }
        }

        var skillPoint = RandomService.Next(0, 3) == 0 ? 2 : 1;
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
          case TrainingType.SkillPoint:
            character.SkillPoint += skillPoint;
            name = "技能ポイント";
            break;
          default:
            await game.CharacterLogAsync("能力強化のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
            isError = true;
            break;
        }

        if (!isError)
        {
          character.Money -= 50;
          if (trainingType == TrainingType.SkillPoint)
          {
            await game.CharacterLogAsync(name + " を <num>+" + skillPoint + "</num> 強化しました");
          }
          else
          {
            await game.CharacterLogAsync(name + "経験値 を <num>+100</num> 強化しました");
          }
        }

        if (RandomService.Next(0, 700) == 0)
        {
          var info = await ItemService.PickTownHiddenItemAsync(repo, character.TownId, character);
          if (info.HasData)
          {
            var town = await repo.Town.GetByIdAsync(character.TownId);
            if (town.HasData)
            {
              await game.CharacterLogAsync($"<town>{town.Data.Name}</town> に隠されたアイテム {info.Data.Name} を手に入れました");
            }
          }
        }
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var trainingType = (TrainingType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      if ((int)trainingType < 1 || (int)trainingType > 5)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }

  public enum TrainingType
  {
    Any = 0,
    Strong = 1,
    Intellect = 2,
    Leadership = 3,
    Popularity = 4,
    SkillPoint = 5,
  }
}
