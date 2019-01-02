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
  /// 城の守備
  /// </summary>
  public class DefendCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Defend;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, Func<string, Task> loggerAsync)
    {
      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (!townOptional.HasData)
      {
        await loggerAsync("ID: " + character.TownId + " の都市は存在しません。<emerge>管理者に連絡してください</emerge>");
      }
      else if (character.SoldierNumber <= 0)
      {
        await loggerAsync("守備には、最低でも 1 人以上の兵士が必要です");
      }
      else
      {
        var town = townOptional.Data;
        if (town.CountryId != character.CountryId)
        {
          await loggerAsync("自国の都市以外を守備することはできません");
        }
        else
        {
          await repo.Character.SetDefenderAsync(character.Id, town.Id);
          await loggerAsync("<town>" + town.Name + "</town> の守備につきました");
        }
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
