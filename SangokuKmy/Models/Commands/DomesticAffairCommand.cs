using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Commands
{
  /// <summary>
  /// 内政コマンド
  /// </summary>
  public abstract class DomesticAffairCommand : Command
  {
    public override Task ExecuteAsync(MainRepository repo, uint characterId, IEnumerable<CharacterCommandParameter> options)
    {
      throw new NotImplementedException();
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates);
    }
  }

  /// <summary>
  /// 農業開発
  /// </summary>
  public class AgricultureCommand : DomesticAffairCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.Agriculture;
  }

  /// <summary>
  /// 商業発展
  /// </summary>
  public class CommercialCommand : DomesticAffairCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.Commercial;
  }

  /// <summary>
  /// 技術開発
  /// </summary>
  public class TechnologyCommand : DomesticAffairCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.Technology;
  }

  /// <summary>
  /// 城壁強化
  /// </summary>
  public class WallCommand : DomesticAffairCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.Wall;
  }

  /// <summary>
  /// 守兵増強
  /// </summary>
  public class WallGuardCommand : DomesticAffairCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.WallGuard;
  }
}
