using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;

#nullable enable

namespace SangokuKmy.Models.Commands
{
  public class SoldierTrainingAllCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.SoldierTrainingAll;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      if (character.Money < 50)
      {
        await game.CharacterLogAsync("金が足りません。<num>50</num> 必要です");
        return;
      }

      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (townOptional.HasData)
      {
        var town = townOptional.Data;
        if (town.CountryId != character.CountryId)
        {
          await game.CharacterLogAsync("<town>" + town.Name + "</town>で訓練しようとしましたが、自国の都市ではありません");
          return;
        }

        var add = (int)(character.Leadership / 6.0f + RandomService.Next(0, character.Leadership / 6));
        if (add < 1)
        {
          add = 1;
        }

        var charas = await repo.Town.GetCharactersAsync(town.Id);
        foreach (var chara in charas)
        {
          chara.Proficiency = (short)Math.Min(100, character.Proficiency + add);
        }

        // 経験値、金の増減
        character.Money -= 50;
        character.Contribution += 30;
        character.AddLeadershipEx(50);
        await game.CharacterLogAsync("訓練を <num>+" + add + "</num> 上げました");
      }
      else
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates);
    }
  }
}
