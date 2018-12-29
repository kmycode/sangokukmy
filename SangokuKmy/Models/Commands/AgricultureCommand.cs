using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Commands
{
  /// <summary>
  /// 農業開発
  /// </summary>
  public class AgricultureCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Agriculture;

    public override Task ExecuteAsync(MainRepository repo, uint characterId, IEnumerable<CharacterCommandParameter> options)
    {
      throw new NotImplementedException();
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates);
    }
  }
}
