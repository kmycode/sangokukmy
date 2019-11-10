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
  public class BuyItemCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.BuyItem;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var itemTypeOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();
      var townIdOptional = options.FirstOrDefault(p => p.Type == 2).ToOptional();
      var resourceSizeOptional = options.FirstOrDefault(p => p.Type == 4).ToOptional();

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
      
      if (info.IsResource)
      {
        if (resourceSizeOptional.Data?.NumberValue == null)
        {
          await game.CharacterLogAsync("アイテム（資源）購入のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
          return;
        }
      }

      var items = await repo.Character.GetItemsAsync(character.Id);
      var itemsMax = CharacterService.GetItemMax(await repo.Character.GetSkillsAsync(character.Id));
      if (!info.IsResource && CharacterService.CountLimitedItems(items) >= itemsMax)
      {
        await game.CharacterLogAsync($"アイテム購入しようとしましたが、アイテム所持数が上限 <num>{itemsMax}</num> に達しています");
        return;
      }

      CharacterItem target;
      var resourceSize = resourceSizeOptional.Data?.NumberValue ?? 0;
      if (!info.IsResource)
      {
        var allItems = await repo.CharacterItem.GetAllAsync();
        target = allItems.FirstOrDefault(i => i.Status == CharacterItemStatus.TownOnSale && i.TownId == townIdOptional.Data.NumberValue && i.Type == itemType);
      }
      else
      {
        var allItems = await repo.CharacterItem.GetAllAsync();
        var targets = allItems.Where(i => i.Status == CharacterItemStatus.TownOnSale && i.TownId == townIdOptional.Data.NumberValue && i.Type == itemType);
        if (!targets.Any())
        {
          target = null;
        }
        else
        {
          var allSize = targets.Sum(i => i.Resource);
          if (allSize < resourceSize)
          {
            resourceSize = allSize;
          }

          if (targets.First().Resource > resourceSize)
          {
            target = await ItemService.DivideResourceAndSaveAsync(repo, targets.First(), resourceSize);
          }
          else if (targets.First().Resource == resourceSize)
          {
            target = targets.First();
          }
          else if (targets.Count() >= 2)
          {
            target = targets.First();
            var currentSize = target.Resource;
            foreach (var t in targets.Skip(1))
            {
              currentSize += t.Resource;
              t.Resource = currentSize <= resourceSize ? 0 : currentSize - resourceSize;
              if (t.Resource == 0)
              {
                t.Status = CharacterItemStatus.CharacterSpent;
                t.TownId = 0;
              }
              await StatusStreaming.Default.SendAllAsync(ApiData.From(t));
            }
          }
          else
          {
            target = targets.First();
            resourceSize = target.Resource;
          }
        }
      }
      if (target == null)
      {
        await game.CharacterLogAsync($"アイテムを購入しようとしましたが、<town>{town.Name}</town> の {info.Name} はすでに売り切れています");
        return;
      }

      var skills = await repo.Character.GetSkillsAsync(character.Id);
      var discountRate = (1 - skills.GetSumOfValues(CharacterSkillEffectType.ItemDiscountPercentage) / 100.0f);
      var needMoney = !info.IsResource ?
        (int)(info.Money * discountRate) :
        (int)(info.MoneyPerResource * discountRate * resourceSize);

      if (needMoney > character.Money)
      {
        await game.CharacterLogAsync("アイテム購入の金が足りません");
        return;
      }

      character.Money -= needMoney;
      await ItemService.SetCharacterAsync(repo, target, character);
      await game.CharacterLogAsync($"<num>{needMoney}</num> を投じて、<town>{town.Name}</town> のアイテム {info.Name} を購入しました");
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var itemType = (CharacterItemType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var resourceSize = options.FirstOrDefault(p => p.Type == 4)?.NumberValue;
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
      var town = await repo.Town.GetByIdAsync(chara.TownId).GetOrErrorAsync(ErrorCode.InternalDataNotFoundError, new { command = "item", townId = chara.TownId, });

      var items = await repo.Town.GetItemsAsync(town.Id);
      if (!items.Any(i => i.Status == CharacterItemStatus.TownOnSale && i.Type == itemType))
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      var info = CharacterItemInfoes.Get(itemType);
      if (!info.HasData)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      if (info.Data.IsResource)
      {
        if (resourceSize == null)
        {
          ErrorCode.LackOfCommandParameter.Throw();
        }
        if (resourceSize > info.Data.DefaultResource)
        {
          ErrorCode.InvalidCommandParameter.Throw();
        }
      }

      var optionsWithoutResult = options.Where(o => o.Type == 1 || o.Type == 3);
      options = optionsWithoutResult.Append(new CharacterCommandParameter
      {
        Type = 2,
        NumberValue = (int)chara.TownId,
      }).ToArray();

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
