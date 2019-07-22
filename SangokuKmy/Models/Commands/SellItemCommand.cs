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
  public class SellItemCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.SellItem;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      var itemTypeOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();
      var itemIdOptional = options.FirstOrDefault(p => p.Type == 2).ToOptional();

      if (!townOptional.HasData)
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var town = townOptional.Data;

      if (!itemTypeOptional.HasData)
      {
        await game.CharacterLogAsync("アイテム売却のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
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

      Optional<CharacterItem> itemOptional = default;
      if (info.IsResource)
      {
        if (itemIdOptional.Data?.NumberValue == null)
        {
          await game.CharacterLogAsync("アイテム（資源）売却のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
          return;
        }

        itemOptional = await repo.CharacterItem.GetByIdAsync((uint)itemIdOptional.Data.NumberValue);
        if (!itemOptional.HasData)
        {
          await game.CharacterLogAsync($"ID: {itemIdOptional.Data.NumberValue} のアイテムは存在しません。<emerge>管理者にお問い合わせください</emerge>");
          return;
        }
      }

      CharacterItem target;
      if (!info.IsResource)
      {
        var charaItems = await repo.Character.GetItemsAsync(character.Id);
        target = charaItems.FirstOrDefault(i => i.Type == itemType && (i.Status == CharacterItemStatus.CharacterHold || i.Status == CharacterItemStatus.CharacterPending));
      }
      else
      {
        target = itemOptional.Data;
        if ((target.Status != CharacterItemStatus.CharacterHold && target.Status != CharacterItemStatus.CharacterPending) || target.CharacterId != character.Id)
        {
          target = null;
        }
      }
      if (target == null)
      {
        await game.CharacterLogAsync($"アイテム {info.Name} を売却しようとしましたが、それは現在所持していません");
        return;
      }
      var itemMoney = info.GetMoney(target);

      character.Money += itemMoney / 2;
      await ItemService.ReleaseCharacterAsync(repo, target, character);
      await game.CharacterLogAsync($"<town>{town.Name}</town> にアイテム {info.Name} を売却しました。金 <num>+{itemMoney / 2}</num>");
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var itemType = (CharacterItemType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var itemId = options.FirstOrDefault(p => p.Type == 2)?.NumberValue;
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);

      var items = await repo.Character.GetItemsAsync(chara.Id);
      if (!items.Any(i => i.Type == itemType && (i.Status == CharacterItemStatus.CharacterHold || i.Status == CharacterItemStatus.CharacterPending) && (itemId == null || itemId == i.Id)))
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      var info = CharacterItemInfoes.Get(itemType).GetOrError(ErrorCode.InvalidCommandParameter);
      if (!info.CanSell)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
