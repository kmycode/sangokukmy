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
  public class DefendCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Defend;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (!townOptional.HasData)
      {
        await game.CharacterLogAsync("ID: " + character.TownId + " の都市は存在しません。<emerge>管理者に連絡してください</emerge>");
      }
      else if (character.SoldierNumber <= 0)
      {
        await game.CharacterLogAsync("守備には、最低でも 1 人以上の兵士が必要です");
      }
      else
      {
        var town = townOptional.Data;
        if (town.CountryId != character.CountryId)
        {
          await game.CharacterLogAsync("自国の都市以外を守備することはできません");
        }
        else
        {
          character.Contribution += 25;
          character.AddLeadershipEx(50);
          var def = await repo.Town.SetDefenderAsync(character.Id, town.Id);
          await game.CharacterLogAsync("<town>" + town.Name + "</town> の守備につきました");

          def.Status = TownDefenderStatus.Available;
          var townCharas = await repo.Town.GetCharactersAsync(town.Id);
          await StatusStreaming.Default.SendCountryAsync(ApiData.From(def), character.CountryId);
          await StatusStreaming.Default.SendCharacterAsync(ApiData.From(def), townCharas.Where(tc => tc.Id != character.Id && tc.CountryId != character.CountryId).Select(tc => tc.Id));

          var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
          if (wars.Any(w => (w.InsistedCountryId == character.CountryId || w.RequestedCountryId == character.CountryId) && (w.Status != CountryWarStatus.None && w.Status != CountryWarStatus.Stoped)) &&
            RandomService.Next(0, 128) == 0)
          {
            var info = await ItemService.PickTownHiddenItemAsync(repo, character.TownId, character);
            if (info.HasData)
            {
              await game.CharacterLogAsync($"<town>{town.Name}</town> に隠されたアイテム {info.Data.Name} を手に入れました");
            }
          }
        }
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
