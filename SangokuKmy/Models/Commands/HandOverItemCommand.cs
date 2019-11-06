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
  public class HandOverItemCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.HandOverItem;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var itemTypeOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();
      var targetIdOptional = options.FirstOrDefault(p => p.Type == 2).ToOptional();
      var itemIdOptional = options.FirstOrDefault(p => p.Type == 3).ToOptional();

      if (!itemTypeOptional.HasData || !targetIdOptional.HasData)
      {
        await game.CharacterLogAsync("アイテム売却のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var itemType = (CharacterItemType)itemTypeOptional.Data.NumberValue;

      var targetOptional = await repo.Character.GetByIdAsync((uint)targetIdOptional.Data.NumberValue);
      if (!targetOptional.HasData)
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の武将は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      if (targetOptional.Data.HasRemoved)
      {
        await game.CharacterLogAsync($"<character>{targetOptional.Data.Name}</character> にアイテムを譲渡しようとしましたが、その武将はすでに削除されています");
        return;
      }
      if (targetOptional.Data.AiType != CharacterAiType.Human)
      {
        await game.CharacterLogAsync($"<character>{targetOptional.Data.Name}</character> にアイテムを譲渡しようとしましたが、その武将は人間ではありません");
        return;
      }
      var target = targetOptional.Data;

      var infoOptional = CharacterItemInfoes.Get(itemType);
      if (!infoOptional.HasData)
      {
        await game.CharacterLogAsync($"ID: {(short)itemType} のアイテムは存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var info = infoOptional.Data;

      Optional<CharacterItem> itemOptional = default;
      if (info.IsResource)
      {
        if (itemIdOptional.Data?.NumberValue == null)
        {
          await game.CharacterLogAsync("アイテム（資源）譲渡のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
          return;
        }

        itemOptional = await repo.CharacterItem.GetByIdAsync((uint)itemIdOptional.Data.NumberValue);
        if (!itemOptional.HasData)
        {
          await game.CharacterLogAsync($"ID: {itemIdOptional.Data.NumberValue} のアイテムは存在しません。<emerge>管理者にお問い合わせください</emerge>");
          return;
        }
      }
      
      CharacterItem item;
      if (!info.IsResource)
      {
        var charaItems = await repo.Character.GetItemsAsync(character.Id);
        item = charaItems.FirstOrDefault(i => i.Type == itemType && (i.Status == CharacterItemStatus.CharacterHold || i.Status == CharacterItemStatus.CharacterPending));
      }
      else
      {
        item = itemOptional.Data;
        if ((item.Status != CharacterItemStatus.CharacterHold && item.Status != CharacterItemStatus.CharacterPending) || item.CharacterId != character.Id)
        {
          item = null;
        }
      }
      if (item == null)
      {
        await game.CharacterLogAsync($"アイテム {info.Name} を譲渡しようとしましたが、それは現在所持していません");
        return;
      }

      character.Contribution += 15;

      await ItemService.ReleaseCharacterAsync(repo, item, character);
      await ItemService.SetCharacterPendingAsync(repo, item, target);
      await game.CharacterLogAsync($"<character>{target.Name}</character> にアイテム {info.Name} を譲渡しました");
      await game.CharacterLogByIdAsync(target.Id, $"<character>{character.Name}</character> からアイテム {info.Name} を受け取りました");
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var itemType = (CharacterItemType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var targetId = (uint)options.FirstOrDefault(p => p.Type == 2).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var itemId = options.FirstOrDefault(p => p.Type == 3)?.NumberValue;
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
      var target = await repo.Character.GetByIdAsync(targetId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);

      if (target.HasRemoved)
      {
        ErrorCode.CharacterNotFoundError.Throw();
      }

      if (target.Id == chara.Id)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      var items = await repo.Character.GetItemsAsync(chara.Id);
      if (!items.Any(i => i.Type == itemType && (i.Status == CharacterItemStatus.CharacterHold || i.Status == CharacterItemStatus.CharacterPending) && (itemId == null || itemId == i.Id)))
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      var info = CharacterItemInfoes.Get(itemType).GetOrError(ErrorCode.InvalidCommandParameter);
      if (!info.CanHandOver)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
