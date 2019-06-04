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
  public class BuyItemCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.BuyItem;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var itemTypeOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();
      var townIdOptional = options.FirstOrDefault(p => p.Type == 2).ToOptional();
      
      if (!itemTypeOptional.HasData || !townIdOptional.HasData)
      {
        await game.CharacterLogAsync("アイテム購入のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var itemType = (CharacterItemType)itemTypeOptional.Data.NumberValue;

      var townOptional = await repo.Town.GetByIdAsync((uint)townIdOptional.Data.NumberValue);
      if (!townOptional.HasData)
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var town = townOptional.Data;

      var infoOptional = CharacterItemInfoes.Get(itemType);
      if (!infoOptional.HasData)
      {
        await game.CharacterLogAsync($"ID: {(short)itemType} のアイテムは存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var info = infoOptional.Data;

      if (info.Money > character.Money)
      {
        await game.CharacterLogAsync("アイテム購入の金が足りません");
        return;
      }

      var allItems = await repo.CharacterItem.GetAllAsync();
      var target = allItems.FirstOrDefault(i => i.Status == CharacterItemStatus.TownOnSale && i.TownId == townIdOptional.Data.NumberValue && i.Type == itemType);
      if (target == null)
      {
        await game.CharacterLogAsync($"アイテムを購入しようとしましたが、<town>{town.Name}</town> の {info.Name} はすでに売り切れています");
        return;
      }

      character.Money -= info.Money;
      await ItemService.SetCharacterAsync(repo, target, character);
      await game.CharacterLogAsync($"<town>{town.Name}</town> のアイテム {info.Name} を購入しました");
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var itemType = (CharacterItemType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
      var town = await repo.Town.GetByIdAsync(chara.TownId).GetOrErrorAsync(ErrorCode.InternalDataNotFoundError, new { command = "item", townId = chara.TownId, });

      var items = await repo.Town.GetItemsAsync(town.Id);
      if (!items.Any(i => i.Status == CharacterItemStatus.TownOnSale && i.Type == itemType))
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      var optionsWithoutResult = options.Where(o => o.Type == 1);
      options = optionsWithoutResult.Append(new CharacterCommandParameter
      {
        Type = 2,
        NumberValue = (int)chara.TownId,
      }).ToArray();

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
