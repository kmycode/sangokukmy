﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;

namespace SangokuKmy.Models.Commands
{
  public class UseItemCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.UseItem;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var itemTypeOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();

      if (!itemTypeOptional.HasData)
      {
        await game.CharacterLogAsync("アイテム使用のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var itemType = (CharacterItemType)itemTypeOptional.Data.NumberValue;

      var infoOptional = CharacterItemInfoes.Get(itemType);
      if (!infoOptional.HasData)
      {
        await game.CharacterLogAsync($"ID: {(short)itemType} のアイテムは存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var info = infoOptional.Data;

      var charaItems = await repo.Character.GetItemsAsync(character.Id);
      var target = charaItems.FirstOrDefault(i => i.Type == itemType && i.Status == CharacterItemStatus.CharacterHold);
      if (target == null)
      {
        target = charaItems.FirstOrDefault(i => i.Type == itemType && i.Status == CharacterItemStatus.CharacterPending);
        var skills = await repo.Character.GetSkillsAsync(character.Id);
        if (target == null)
        {
          await game.CharacterLogAsync($"アイテム {info.Name} を使用しようとしましたが、それは現在所持していません");
          return;
        }
      }

      if (info.UsingEffects != null && info.UsingEffects.Any(ue => ue.Type == CharacterItemEffectType.AddSubBuildingExtraSize))
      {
        var townOptional = await repo.Town.GetByIdAsync(character.TownId);
        if (townOptional.HasData)
        {
          var town = townOptional.Data;
          if (town.CountryId != character.CountryId)
          {
            await game.CharacterLogAsync($"アイテム {info.Name} は、自国の都市でしか使用できません");
            return;
          }
          if (town.TownSubBuildingExtraSpace + info.UsingEffects.Where(ue => ue.Type == CharacterItemEffectType.AddSubBuildingExtraSize).Sum(ue => ue.Value) > 4)
          {
            await game.CharacterLogAsync($"アイテム {info.Name} を使おうとしましたが、<town>{town.Name}</town> の追加の敷地を <num>4</num> より多くすることはできません");
            return;
          }
        }
      }
      
      var log = await ItemService.SpendCharacterAsync(repo, target, character);
      await game.CharacterLogAsync($"アイテム {info.Name} を使用しました。効果: {log}");
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var itemType = (CharacterItemType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);

      var items = await repo.Character.GetItemsAsync(chara.Id);
      if (!items.Any(i => i.Type == itemType && (i.Status == CharacterItemStatus.CharacterHold || i.Status == CharacterItemStatus.CharacterPending)))
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      var info = CharacterItemInfoes.Get(itemType).GetOrError(ErrorCode.InvalidCommandParameter);
      if (!info.CanUse)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
