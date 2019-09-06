using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  /// <summary>
  /// 陣形変更
  /// </summary>
  public class ChangeFormationCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.ChangeFormation;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var formationTypeOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();

      if (!formationTypeOptional.HasData)
      {
        await game.CharacterLogAsync("陣形変更のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
      }
      else
      {
        var countryOptional = await repo.Country.GetAliveByIdAsync(character.CountryId);
        if (!countryOptional.HasData)
        {
          await game.CharacterLogAsync("陣形変更は、無所属の武将は実行できません");
          return;
        }

        var type = (FormationType)formationTypeOptional.Data.NumberValue;
        var info = FormationTypeInfoes.Get(type);

        if (!info.HasData)
        {
          await game.CharacterLogAsync($"ID: {(short)type} の陣形が見つかりません。<emerge>管理者にお問い合わせください</emerge>");
          return;
        }
        if (type == character.FormationType)
        {
          await game.CharacterLogAsync("すでにその陣形になっています");
          return;
        }

        var formations = await repo.Character.GetFormationsAsync(character.Id);
        if (type != FormationType.Normal && !formations.Any(f => f.Type == type))
        {
          await game.CharacterLogAsync($"陣形 {info.Data.Name} はまだ獲得していません");
          return;
        }

        character.Contribution += 30;
        character.AddLeadershipEx(50);
        character.FormationType = type;
        await game.CharacterLogAsync($"陣形を {info.Data.Name} に変更しました");
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var type = (FormationType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var info = FormationTypeInfoes.Get(type);
      if (!info.HasData)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
