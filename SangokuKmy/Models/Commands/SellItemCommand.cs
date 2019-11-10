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
  public class SellItemCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.SellItem;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      var itemTypeOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();
      var resourceSizeOptional = options.FirstOrDefault(p => p.Type == 3).ToOptional();

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

      if (info.IsResource)
      {
        if (resourceSizeOptional.Data?.NumberValue == null)
        {
          await game.CharacterLogAsync("アイテム（資源）売却のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
          return;
        }
      }

      CharacterItem target;
      var resourceSize = resourceSizeOptional.Data?.NumberValue ?? 0;
      if (!info.IsResource)
      {
        var charaItems = await repo.Character.GetItemsAsync(character.Id);
        target = charaItems.FirstOrDefault(i => i.Type == itemType && (i.Status == CharacterItemStatus.CharacterHold || i.Status == CharacterItemStatus.CharacterPending));
      }
      else
      {
        var charaItems = await repo.Character.GetItemsAsync(character.Id);
        var targets = charaItems.Where(i => i.Type == itemType && (i.Status == CharacterItemStatus.CharacterHold || i.Status == CharacterItemStatus.CharacterPending));
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
              var size = currentSize <= resourceSize ? 0 : currentSize - resourceSize;
              target.Resource += t.Resource - size;
              t.Resource = size;
              if (t.Resource == 0)
              {
                t.Status = CharacterItemStatus.CharacterSpent;
                t.CharacterId = 0;
              }
              await StatusStreaming.Default.SendAllAsync(ApiData.From(t));

              if (currentSize >= resourceSize)
              {
                break;
              }
            }
            await StatusStreaming.Default.SendAllAsync(ApiData.From(target));
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
      var resourceSize = options.FirstOrDefault(p => p.Type == 3)?.NumberValue;
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);

      var items = await repo.Character.GetItemsAsync(chara.Id);
      if (!items.Any(i => i.Type == itemType && (i.Status == CharacterItemStatus.CharacterHold || i.Status == CharacterItemStatus.CharacterPending)))
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      var info = CharacterItemInfoes.Get(itemType).GetOrError(ErrorCode.InvalidCommandParameter);
      if (!info.CanSell)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      if (info.IsResource)
      {
        if (resourceSize == null)
        {
          ErrorCode.LackOfCommandParameter.Throw();
        }
        if (resourceSize > info.DefaultResource)
        {
          ErrorCode.InvalidCommandParameter.Throw();
        }
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
