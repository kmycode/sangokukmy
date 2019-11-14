using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;

namespace SangokuKmy.Models.Commands
{
  public class ResearchFormationCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.ResearchFormation;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      // 内政値に加算する
      // $kgat += int($klea/6 + rand($klea/6));
      var current = character.Proficiency;
      var add = (int)(character.Leadership / 10.0f + RandomService.Next(0, character.Leadership / 20));
      if (add < 1)
      {
        add = 1;
      }
      character.FormationPoint = (short)Math.Min(100, character.Proficiency + add);

      // 経験値、金の増減
      character.Money -= 50;
      character.AddLeadershipEx(50);
      if (character.CountryId > 0)
      {
        character.Contribution += 10;
        character.SkillPoint++;
      }

      await game.CharacterLogAsync("陣形ポイントを <num>+" + add + "</num> 上げました");
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates);
    }
  }
}
