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

namespace SangokuKmy.Models.Commands
{
  public class HandOverItemCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.HandOverItem;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var itemTypeOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();
      var targetIdOptional = options.FirstOrDefault(p => p.Type == 2).ToOptional();
      var resourceSizeOptional = options.FirstOrDefault(p => p.Type == 4).ToOptional();

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

      if (info.IsResource)
      {
        if (resourceSizeOptional.Data?.NumberValue == null)
        {
          await game.CharacterLogAsync("アイテム（資源）譲渡のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
          return;
        }
      }
      
      var charaItems = await repo.Character.GetItemsAsync(character.Id);
      var item = charaItems.FirstOrDefault(i => i.Type == itemType && (i.Status == CharacterItemStatus.CharacterHold));
      if (item == null)
      {
        item = charaItems.FirstOrDefault(i => i.Type == itemType && (i.Status == CharacterItemStatus.CharacterPending));
      }

      var resourceSize = resourceSizeOptional.Data?.NumberValue ?? 0;
      if (info.IsResource)
      {
        if (item.Resource < resourceSize)
        {
          resourceSize = item.Resource;
        }
      }

      if (item == null)
      {
        await game.CharacterLogAsync($"アイテム {info.Name} を譲渡しようとしましたが、それは現在所持していません");
        return;
      }

      character.Contribution += 15;

      if (!info.IsResource || item.Resource <= resourceSize)
      {
        await ItemService.ReleaseCharacterAsync(repo, item, character);
        await ItemService.SetCharacterPendingAsync(repo, item, target);
        await game.CharacterLogAsync($"<character>{target.Name}</character> にアイテム {info.Name} を譲渡しました");
        await game.CharacterLogByIdAsync(target.Id, $"<character>{character.Name}</character> からアイテム {info.Name} を受け取りました");
      }
      else
      {
        // 資源の分配
        var newItem = await ItemService.DivideResourceAndSaveAsync(repo, item, resourceSize);
        await ItemService.ReleaseCharacterAsync(repo, newItem, character);
        await ItemService.SetCharacterPendingAsync(repo, newItem, target);
        await game.CharacterLogAsync($"<character>{target.Name}</character> に資源 {info.Name} を <num>{resourceSize}</num> 譲渡しました");
        await game.CharacterLogByIdAsync(target.Id, $"<character>{character.Name}</character> から資源 {info.Name} を <num>{resourceSizeOptional.Data.NumberValue}</num> 受け取りました");
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var itemType = (CharacterItemType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var targetId = (uint)options.FirstOrDefault(p => p.Type == 2).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var resourceSize = options.FirstOrDefault(p => p.Type == 4)?.NumberValue ?? 0;
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
      if (!items.Any(i => i.Type == itemType && (i.Status == CharacterItemStatus.CharacterHold || i.Status == CharacterItemStatus.CharacterPending)))
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      var info = CharacterItemInfoes.Get(itemType).GetOrError(ErrorCode.InvalidCommandParameter);
      if (!info.CanHandOver)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      if (info.IsResource && (info.DefaultResource < resourceSize || resourceSize == 0))
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("resourceSize", resourceSize, 1, info.DefaultResource));
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
