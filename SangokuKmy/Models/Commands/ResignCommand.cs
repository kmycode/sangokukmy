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
using SangokuKmy.Models.Services;

namespace SangokuKmy.Models.Commands
{
  public class ResignCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Resign;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var currentCountryOptional = await repo.Country.GetAliveByIdAsync(character.CountryId);
      if (!currentCountryOptional.HasData)
      {
        await game.CharacterLogAsync("下野は無所属は実行できません");
        return;
      }
      var country = currentCountryOptional.Data;

      var reinforcements = await repo.Reinforcement.GetByCharacterIdAsync(character.Id);
      var reinforcement = reinforcements.FirstOrDefault(r => r.Status == ReinforcementStatus.Active);
      if (reinforcement != null && (await repo.Country.GetAliveByIdAsync(reinforcement.CharacterCountryId)).HasData)
      {
        await game.CharacterLogAsync("援軍は下野を実行できません");
        return;
      }

      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (!townOptional.HasData)
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var town = townOptional.Data;

      BlockAction blockAction = null;
      if (!(await repo.System.GetAsync()).IsWaitingReset)
      {
        character.ResignCount++;
        blockAction = new BlockAction
        {
          CharacterId = character.Id,
          Type = BlockActionType.StopJoin,
          ExpiryDate = DateTime.Now.AddDays(character.ResignCount),
        };
        await repo.BlockAction.AddAsync(blockAction);
      }

      await CharacterService.ChangeCountryAsync(repo, 0, new Character[] { character, });
      if (blockAction != null)
      {
        await game.CharacterLogAsync($"<country>{country.Name}</country> から下野しました。下野回数は <num>{character.ResignCount}</num> 回目です。仕官は以下の日時以降に行えます: {blockAction.ExpiryDate:yyyy/MM/dd HH:mm:ss}");
      }
      else
      {
        await game.CharacterLogAsync("<country>" + country.Name + "</country> から下野しました");
      }
      await game.MapLogAsync(EventType.CharacterResign, "<character>" + character.Name + "</character> は <country>" + country.Name + "</country> から下野しました", false);

      var townCharas = await repo.Town.GetCharactersAsync(town.Id);
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(town), townCharas.Select(tc => tc.Id));
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(country), character.Id);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
      var country = await repo.Country.GetAliveByIdAsync(chara.CountryId);
      if (!country.HasData)
      {
        ErrorCode.NotPermissionError.Throw();
      }

      var commands = await repo.CharacterCommand.GetAllAsync(characterId, (await repo.System.GetAsync()).GameDateTime);
      if (commands.Count(c => c.Type == this.Type) + gameDates.Count() > 3)
      {
        ErrorCode.InvalidOperationError.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates);
    }
  }
}
