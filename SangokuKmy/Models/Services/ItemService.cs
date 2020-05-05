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

      // 資源を統合
      var infoOptional = CharacterItemInfoes.Get(item.Type);
      if (infoOptional.HasData)
      {
        var info = infoOptional.Data;
        if (info.IsResource)
        {
          var characterItems = await repo.Character.GetItemsAsync(chara.Id);
          var resource = characterItems.FirstOrDefault(i => i.Id != item.Id && i.Status == CharacterItemStatus.CharacterHold && i.Type == item.Type);
          if (resource != null)
          {
            resource.Resource += item.Resource;
            item.Status = CharacterItemStatus.CharacterSpent;
            item.CharacterId = 0;
            item.Resource = 0;
            await StatusStreaming.Default.SendAllAsync(ApiData.From(resource));
          }
        }
      }

      await CharacterService.StreamCharacterAsync(repo, chara);
      await StatusStreaming.Default.SendAllAsync(ApiData.From(item));
    }

    public static async Task SetCharacterPendingAsync(MainRepository repo, CharacterItem item, Character chara)
    {
      if (item.Status == CharacterItemStatus.CharacterHold || item.Status == CharacterItemStatus.CharacterPending)
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

      var system = await repo.System.GetAsync();

      item.Status = CharacterItemStatus.CharacterPending;
      item.CharacterId = chara.Id;
      item.TownId = 0;
      item.LastStatusChangedGameDate = system.GameDateTime;

      await CharacterService.StreamCharacterAsync(repo, chara);
      await StatusStreaming.Default.SendAllAsync(ApiData.From(item));
    }

    public static async Task ReleaseCharacterAsync(MainRepository repo, CharacterItem item, Character chara)
    {
      await ReleaseCharacterAsync(repo, item, chara, CharacterItemStatus.TownOnSale);
    }

    public static async Task<string> SpendCharacterAsync(MainRepository repo, CharacterItem item, Character chara)
    {
      await ReleaseCharacterAsync(repo, item, chara, CharacterItemStatus.CharacterSpent);

      var info = item.GetInfo();
      if (info.HasData)
      {
        var logs = new List<string>();
        foreach (var effect in info.Data.UsingEffects)
        {
          if (effect.Type == CharacterItemEffectType.Money)
          {
            chara.Money += effect.Value;
            logs.Add($"金 <num>+{effect.Value}</num>");
          }
          if (effect.Type == CharacterItemEffectType.SkillPoint)
          {
            chara.SkillPoint += effect.Value;
            logs.Add($"技能P <num>+{effect.Value}</num>");
          }
          if (effect.Type == CharacterItemEffectType.TerroristEnemy)
          {
            await repo.DelayEffect.AddAsync(new DelayEffect
            {
              Type = DelayEffectType.TerroristEnemy,
            });
            logs.Add($"異民族敵性化");
          }
          if (effect.Type == CharacterItemEffectType.AppearKokin)
          {
            await repo.DelayEffect.AddAsync(new DelayEffect
            {
              Type = DelayEffectType.AppearKokin,
            });
            logs.Add($"黄巾出現");
          }
          if (effect.Type == CharacterItemEffectType.DiscountSoldierPercentageWithResource)
          {
            logs.Add("特定兵種割引");
          }
          if (effect.Type == CharacterItemEffectType.FormationEx)
          {
            var formation = await repo.Character.GetFormationAsync(chara.Id, chara.FormationType);
            if (formation != null)
            {
              var i = FormationTypeInfoes.Get(chara.FormationType);
              if (i.HasData)
              {
                formation.Experience += effect.Value;
                i.Data.CheckLevelUp(formation);
                await StatusStreaming.Default.SendCharacterAsync(ApiData.From(formation), chara.Id);
                logs.Add($"陣形経験値 <num>{effect.Value}</num>");
              }
            }
          }
          if (effect.Type == CharacterItemEffectType.IntellectEx)
          {
            chara.AddIntellectEx((short)effect.Value);
            logs.Add($"知力経験値 <num>{effect.Value}</num>");
          }
        }

        return string.Join("と", logs);
      }

      return string.Empty;
    }

    private static async Task ReleaseCharacterAsync(MainRepository repo, CharacterItem item, Character chara, CharacterItemStatus newStatus)
    {
      if ((item.Status != CharacterItemStatus.CharacterHold && item.Status != CharacterItemStatus.CharacterPending) || (chara != null && item.CharacterId != chara.Id))
      {
        return;
      }

      if (chara != null && item.Status == CharacterItemStatus.CharacterHold)
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

      item.Status = newStatus;
      item.CharacterId = 0;

      if (newStatus == CharacterItemStatus.TownHidden || newStatus == CharacterItemStatus.TownOnSale)
      {
        if (chara == null)
        {
          var towns = await repo.Town.GetAllAsync();
          item.TownId = RandomService.Next(towns).Id;
        }
        else
        {
          item.TownId = chara.TownId;
        }
      }

      if (chara != null)
      {
        await CharacterService.StreamCharacterAsync(repo, chara);
      }
      await StatusStreaming.Default.SendAllAsync(ApiData.From(item));
    }

    public static async Task<CharacterItem> DivideResourceAndSaveAsync(MainRepository repo, CharacterItem item, int resourceSize)
    {
      if (item.Resource <= resourceSize)
      {
        return item;
      }

      item.Resource -= resourceSize;

      var newItem = new CharacterItem
      {
        Type = item.Type,
        Status = item.Status,
        CharacterId = item.CharacterId,
        TownId = item.TownId,
        IntLastStatusChangedGameDate = item.IntLastStatusChangedGameDate,
        Resource = resourceSize,
      };
      await GenerateItemAndSaveAsync(repo, newItem);

      await StatusStreaming.Default.SendAllAsync(ApiData.From(newItem));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(item));

      return newItem;
    }

    public static async Task<Optional<CharacterItemInfo>> PickTownHiddenItemAsync(MainRepository repo, uint townId, Character chara)
    {
      if (chara.AiType != CharacterAiType.Human && chara.AiType != CharacterAiType.Administrator)
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
              Resource = (ushort)info.DefaultResource,
            });
          }
          for (var i = 0; i < saleCount; i++)
          {
            items.Add(new CharacterItem
            {
              Type = info.Type,
              Status = CharacterItemStatus.TownOnSale,
              TownId = RandomService.Next(towns).Id,
              Resource = (ushort)info.DefaultResource,
            });
          }
        }
      }

      await repo.CharacterItem.AddAsync(items);
    }

    public static async Task GenerateItemAndSaveAsync(MainRepository repo, CharacterItem item)
    {
      await repo.CharacterItem.AddAsync(item);
      await repo.SaveChangesAsync();
      await StatusStreaming.Default.SendAllAsync(ApiData.From(item));
    }
  }
}
