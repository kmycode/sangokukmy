﻿using System;
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
using SangokuKmy.Models.Common;

namespace SangokuKmy.Models.Commands
{
  public class JoinCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Join;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var currentCountryOptional = await repo.Country.GetAliveByIdAsync(character.CountryId);
      if (currentCountryOptional.HasData)
      {
        await game.CharacterLogAsync("仕官は無所属でないと実行できません");
        return;
      }

      var reinforcements = await repo.Reinforcement.GetByCharacterIdAsync(character.Id);
      var reinforcement = reinforcements.FirstOrDefault(r => r.Status == ReinforcementStatus.Active);
      if (reinforcement != null && (await repo.Country.GetAliveByIdAsync(reinforcement.CharacterCountryId)).HasData)
      {
        await game.CharacterLogAsync("援軍は仕官を実行できません");
        return;
      }

      var blockActions = await repo.BlockAction.GetAvailableTypesAsync(character.Id);
      if (blockActions.Contains(BlockActionType.StopJoin) && !(await repo.System.GetAsync()).IsWaitingReset)
      {
        var blockAction = await repo.BlockAction.GetAsync(character.Id, BlockActionType.StopJoin);
        if (blockAction.HasData)
        {
          await game.CharacterLogAsync($"あなたは下野などの理由により仕官を制限されています。期限: {blockAction.Data.ExpiryDate:yyyy/MM/dd HH:mm:ss}");
          return;
        }
      }

      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (!townOptional.HasData)
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }

      var town = townOptional.Data;
      if (town.CountryId == 0)
      {
        await game.CharacterLogAsync("<town>" + town.Name + "</town> で仕官しようとしましたが、その都市は国に支配されていません");
        return;
      }

      var countryOptional = await repo.Country.GetAliveByIdAsync(town.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync("ID:" + town.CountryId + " の国は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var country = countryOptional.Data;

      if (country.IntEstablished + Config.CountryBattleStopDuring > game.GameDateTime.ToInt())
      {
        var characterCount = await repo.Country.CountCharactersAsync(country.Id, true);
        if (characterCount >= Config.CountryJoinMaxOnLimited)
        {
          await game.CharacterLogAsync("<country>" + town.Name + "</country> に仕官しようとしましたが、すでに仕官数上限に達しています");
          return;
        }
      }

      if (country.AiType != CountryAiType.Human && character.AiType == CharacterAiType.Human)
      {
        var items = await repo.Character.GetItemsAsync(character.Id);
        var itemInfo = items.GetInfos().FirstOrDefault(i => i.UsingEffects != null && i.UsingEffects.Any(u => u.Type == CharacterItemEffectType.JoinableAiCountry && u.Value == (int)country.AiType));
        if (itemInfo != null)
        {
          var item = items.FirstOrDefault(i => i.Type == itemInfo.Type);
          await ItemService.SpendCharacterAsync(repo, item, character);
          await game.CharacterLogAsync($"対象国はAIの統治する特別な国のため、仕官のさいにアイテム {itemInfo.Name} を使用しました");
        }
        else
        {
          await game.CharacterLogAsync($"<country>{country.Name}</country> に仕官しようとしましたが、AIの統治する特別な国のため人間は仕官できません");
          return;
        }
      }

      await CharacterService.ChangeCountryAsync(repo, country.Id, new Character[] { character, });
      await game.CharacterLogAsync("<country>" + country.Name + "</country> に仕官しました");
      await game.MapLogAsync(EventType.CharacterJoin, "<character>" + character.Name + "</character> は <country>" + country.Name + "</country> に仕官しました", false);

      var townCharas = await repo.Town.GetCharactersAsync(town.Id);
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(town), townCharas.Select(tc => tc.Id));
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(country), character.Id);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
      var country = await repo.Country.GetAliveByIdAsync(chara.CountryId);
      if (country.HasData)
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
