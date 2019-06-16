using SangokuKmy.Common;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Services
{
  public static class ItemService
  {
    public static async Task SetCharacterAsync(MainRepository repo, CharacterItem item, Character chara)
    {
      if (item.Status == CharacterItemStatus.CharacterHold)
      {
        if (item.CharacterId == chara.Id)
        {
          return;
        }

        var oldChara = await repo.Character.GetByIdAsync(item.CharacterId);
        if (oldChara.HasData)
        {
          await ReleaseCharacterAsync(repo, item, oldChara.Data);
        }
      }

      var strong = (short)item.GetSumOfValues(CharacterItemEffectType.Strong);
      var intellect = (short)item.GetSumOfValues(CharacterItemEffectType.Intellect);
      var leadership = (short)item.GetSumOfValues(CharacterItemEffectType.Leadership);
      var popularity = (short)item.GetSumOfValues(CharacterItemEffectType.Popularity);

      chara.Strong += strong;
      chara.Intellect += intellect;
      chara.Leadership += leadership;
      chara.Popularity += popularity;

      item.Status = CharacterItemStatus.CharacterHold;
      item.CharacterId = chara.Id;
      item.TownId = 0;

      await StatusStreaming.Default.SendAllAsync(ApiData.From(chara));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(item));
    }

    public static async Task SetCharacterPendingAsync(MainRepository repo, CharacterItem item, Character chara)
    {
      if (item.Status == CharacterItemStatus.CharacterHold)
      {
        if (item.CharacterId == chara.Id)
        {
          return;
        }

        var oldChara = await repo.Character.GetByIdAsync(item.CharacterId);
        if (oldChara.HasData)
        {
          await ReleaseCharacterAsync(repo, item, oldChara.Data);
        }
      }

      item.Status = CharacterItemStatus.CharacterPending;
      item.CharacterId = chara.Id;
      item.TownId = 0;

      await StatusStreaming.Default.SendAllAsync(ApiData.From(chara));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(item));
    }

    public static async Task ReleaseCharacterAsync(MainRepository repo, CharacterItem item, Character chara)
    {
      if ((item.Status != CharacterItemStatus.CharacterHold && item.Status != CharacterItemStatus.CharacterPending) || item.CharacterId != chara.Id)
      {
        return;
      }

      if (item.Status == CharacterItemStatus.CharacterHold)
      {
        var strong = (short)item.GetSumOfValues(CharacterItemEffectType.Strong);
        var intellect = (short)item.GetSumOfValues(CharacterItemEffectType.Intellect);
        var leadership = (short)item.GetSumOfValues(CharacterItemEffectType.Leadership);
        var popularity = (short)item.GetSumOfValues(CharacterItemEffectType.Popularity);

        chara.Strong -= strong;
        chara.Intellect -= intellect;
        chara.Leadership -= leadership;
        chara.Popularity -= popularity;
      }

      item.Status = CharacterItemStatus.TownOnSale;
      item.CharacterId = 0;
      item.TownId = chara.TownId;

      await StatusStreaming.Default.SendAllAsync(ApiData.From(chara));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(item));
    }

    public static async Task<Optional<CharacterItemInfo>> PickTownHiddenItemAsync(MainRepository repo, uint townId, Character chara)
    {
      var skills = await repo.Character.GetSkillsAsync(chara.Id);
      var holdItems = await repo.Character.GetItemsAsync(chara.Id);
      if (holdItems.Count(i => i.Status == CharacterItemStatus.CharacterHold) >= CharacterService.GetItemMax(skills))
      {
        return default;
      }

      var items = await repo.Town.GetItemsAsync(townId);
      var hiddenItems = items
        .Where(i => i.Status == CharacterItemStatus.TownHidden)
        .Where(i => i.GetInfo().Data?.DiscoverFroms?.Any(d => d == chara.From) ?? true);
      if (!hiddenItems.Any())
      {
        return default;
      }

      var item = RandomService.Next(hiddenItems);
      var info = item.GetInfo();
      if (!info.HasData)
      {
        return default;
      }

      await SetCharacterPendingAsync(repo, item, chara);
      return info;
    }

    public static async Task InitializeItemOnTownsAsync(MainRepository repo, IReadOnlyList<Town> towns)
    {
      var infos = CharacterItemInfoes.GetAll();

      var items = new List<CharacterItem>();
      foreach (var info in infos)
      {
        if (info.RareType == CharacterItemRareType.EventOnly)
        {
          for (var i = 0; i < info.InitializeNumber; i++)
          {
            items.Add(new CharacterItem
            {
              Type = info.Type,
              Status = CharacterItemStatus.Hidden,
            });
          }
        }
        else
        {
          var hiddenCount = 0;
          var saleCount = 0;
          if (info.RareType == CharacterItemRareType.TownHiddenOnly)
          {
            hiddenCount = info.InitializeNumber;
          }
          else if (info.RareType == CharacterItemRareType.TownOnSaleOnly)
          {
            saleCount = info.InitializeNumber;
          }
          else if (info.RareType == CharacterItemRareType.TownOnSaleOrHidden)
          {
            hiddenCount = RandomService.Next(0, info.InitializeNumber + 1);
            saleCount = info.InitializeNumber - hiddenCount;
          }

          for (var i = 0; i < hiddenCount; i++)
          {
            items.Add(new CharacterItem
            {
              Type = info.Type,
              Status = CharacterItemStatus.TownHidden,
              TownId = RandomService.Next(towns).Id,
            });
          }
          for (var i = 0; i < saleCount; i++)
          {
            items.Add(new CharacterItem
            {
              Type = info.Type,
              Status = CharacterItemStatus.TownOnSale,
              TownId = RandomService.Next(towns).Id,
            });
          }
        }
      }

      await repo.CharacterItem.AddAsync(items);
    }
  }
}
