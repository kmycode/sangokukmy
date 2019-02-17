using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  public class GatherCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Gather;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (!townOptional.HasData)
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }

      var town = townOptional.Data;
      var unitData = await repo.Unit.GetByMemberIdAsync(character.Id);
      if (!unitData.Unit.HasData || !unitData.Member.HasData)
      {
        await game.CharacterLogAsync("集合しようとしましたが、部隊に所属していません");
        return;
      }

      var unit = unitData.Unit.Data;
      var unitLeader = unitData.Member.Data;
      if (unitLeader.Post != UnitMemberPostType.Leader)
      {
        await game.CharacterLogAsync("集合しようとしましたが、部隊長ではありません");
        return;
      }

      var members = (await repo.Unit.GetMembersAsync(unit.Id)).Where(m => m.CharacterId != character.Id);
      var memberIds = members.Select(m => m.CharacterId);
      if (members.Any())
      {
        var memberCharas = await repo.Character.GetByIdAsync(members.Select(m => m.CharacterId));
        foreach (var memberChara in memberCharas)
        {
          memberChara.TownId = town.Id;
          await game.CharacterLogByIdAsync(memberChara.Id, "部隊 <unit>" + unit.Name + "</unit> の隊長 <character>" + character.Name + "</character> の指示で、<town>" + town.Name + "</town> に集合しました");
          await StatusStreaming.Default.SendCharacterAsync(new IApiData[] {
            ApiData.From(town),
            ApiData.From(memberChara),
            ApiData.From(new ApiSignal { Type = SignalType.UnitGathered, }),
          }, memberChara.Id);
        }
      }

      character.AddLeadershipEx(50);
      character.Contribution += 10;
      await game.CharacterLogAsync("<number>" + members.Count() + "</number> 名を <town>" + town.Name + "</town> に集合させました");
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates);
    }
  }
}
