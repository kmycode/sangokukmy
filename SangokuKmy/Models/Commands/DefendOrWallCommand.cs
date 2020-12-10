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
  /// <summary>
  /// 城の守備
  /// </summary>
  public class DefendOrWallCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.DefendOrWall;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var command = CharacterCommandType.Defend;
      var defenders = await repo.Town.GetDefendersAsync(character.TownId);
      if (defenders.Any(d => d.Character.Id == character.Id))
      {
        command = CharacterCommandType.Wall;
      }

      await Commands.ExecuteAsync(command, repo, character, Enumerable.Empty<CharacterCommandParameter>(), game);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var skills = await repo.Character.GetSkillsAsync(characterId);
      if (!skills.AnySkillEffects(CharacterSkillEffectType.Command, (int)this.Type))
      {
        ErrorCode.NotSkillError.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
