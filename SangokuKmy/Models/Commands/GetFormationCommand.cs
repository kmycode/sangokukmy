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
  /// 陣形取得
  /// </summary>
  public class GetFormationCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.GetFormation;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var formationTypeOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();

      if (!formationTypeOptional.HasData)
      {
        await game.CharacterLogAsync("陣形獲得のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
      }
      else if (character.Money < 1000)
      {
        await game.CharacterLogAsync("陣形獲得の金が足りません");
      }
      else
      {
        var countryOptional = await repo.Country.GetAliveByIdAsync(character.CountryId);
        var type = (FormationType)formationTypeOptional.Data.NumberValue;
        var info = FormationTypeInfoes.Get(type);

        if (!info.HasData)
        {
          await game.CharacterLogAsync($"ID: {(short)type} の陣形が見つかりません。<emerge>管理者にお問い合わせください</emerge>");
          return;
        }
        if (!info.Data.CanGetByCommand)
        {
          await game.CharacterLogAsync($"陣形 {info.Data.Name} は、コマンドからは獲得できません");
          return;
        }
        if (info.Data.RequiredPoint > character.FormationPoint)
        {
          await game.CharacterLogAsync($"陣形 {info.Data.Name} の獲得に必要なポイントが足りません。<num>{info.Data.RequiredPoint}</num> 必要ですが、現在の手持ちは <num>{character.FormationPoint}</num> しかありません");
          return;
        }

        var formations = await repo.Character.GetFormationsAsync(character.Id);
        if (formations.Any(f => f.Type == type))
        {
          await game.CharacterLogAsync($"陣形 {info.Data.Name} は、すでに獲得しています");
          return;
        }
        if (info.Data.SubjectAppear != null && !info.Data.SubjectAppear(formations))
        {
          await game.CharacterLogAsync($"陣形 {info.Data.Name} の獲得に必要な条件を満たしていません");
          return;
        }

        if (countryOptional.HasData)
        {
          character.Contribution += 30;
        }
        character.AddLeadershipEx(50);
        character.Money -= 1000;
        character.FormationPoint -= info.Data.RequiredPoint;

        var formation = new Formation
        {
          Type = type,
          CharacterId = character.Id,
        };
        await repo.Character.AddFormationAsync(formation);
        await repo.SaveChangesAsync();
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(formation), character.Id);
        await game.CharacterLogAsync($"陣形 {info.Data.Name} を獲得しました");
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
