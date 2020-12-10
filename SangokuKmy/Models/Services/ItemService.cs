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
            // ２つの資源アイテムを統合
            resource.Resource += item.Resource;
            item.Status = CharacterItemStatus.CharacterSpent;
            item.CharacterId = 0;
            item.Resource = 0;
            await StatusStreaming.Default.SendAllAsync(ApiData.From(resource));
          }
          else
          {
            // 今の資源アイテムがそのまま所持物となる
            item.IsAvailable = true;
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

    public static async Task<string> SpendCharacterAsync(MainRepository repo, CharacterItem item, Character chara, bool isWithEffect = true)
    {
      await ReleaseCharacterAsync(repo, item, chara, CharacterItemStatus.CharacterSpent);

      if (!isWithEffect)
      {
        return string.Empty;
      }

      var info = item.GetInfo();
      if (info.HasData && info.Data.UsingEffects != null)
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
          if (effect.Type == CharacterItemEffectType.AddSubBuildingExtraSize)
          {
            var townOptional = await repo.Town.GetByIdAsync(chara.TownId);
            if (townOptional.HasData)
            {
              townOptional.Data.TownSubBuildingExtraSpace += (short)effect.Value;
              logs.Add($"<town>{townOptional.Data.Name}</town> の敷地 <num>+{effect.Value}</num>");
              await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(townOptional.Data), repo);
            }
            else
            {
              logs.Add($"<emerge>エラー: 都市が見つかりませんでした ID: {chara.TownId}</emerge>");
            }
          }
          if (effect.Type == CharacterItemEffectType.CheckGyokuji)
          {
            var countries = await repo.Country.GetAllAsync();
            var country = countries.FirstOrDefault(c => c.GyokujiStatus == CountryGyokujiStatus.HasGenuine);
            if (country != null)
            {
              logs.Add($"本物の玉璽を所持する国: <country>{country.Name}</country>");
            }
            else
            {
              logs.Add("本物の玉璽を所持する国: なし");
            }
          }
          if (effect.Type == CharacterItemEffectType.Skill)
          {
            var i = CharacterSkillInfoes.Get((CharacterSkillType)effect.Value);
            if (i.HasData)
            {
              var skills = await repo.Character.GetSkillsAsync(chara.Id);
              if (!skills.Any(s => s.Status == CharacterSkillStatus.Available && (int)s.Type == effect.Value))
              {
                var skill = new CharacterSkill
                {
                  CharacterId = chara.Id,
                  Status = CharacterSkillStatus.Available,
                  Type = (CharacterSkillType)effect.Value,
                };
                await repo.Character.AddSkillAsync(skill);
                await StatusStreaming.Default.SendCharacterAsync(ApiData.From(skill), chara.Id);
              }
              logs.Add("技能 " + i.Data.Name);
            }
            else
            {
              logs.Add($"<emerge>エラー: {effect.Value} の技能は存在しません。管理者にお問い合わせください");
            }
          }
        }

        return logs.Count > 0 ? string.Join("と", logs) : string.Empty;
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
      if (chara.AiType != CharacterAiType.Human && chara.AiType != CharacterAiType.Administrator && !chara.AiType.IsManaged())
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
      foreach (var item in infos.SelectMany(i => GenerateItems(i, towns)))
      {
        items.Add(item);
      }

      await repo.CharacterItem.AddAsync(items);
    }

    public static async Task RegenerateItemOnTownsAsync(MainRepository repo, IReadOnlyList<Town> towns)
    {
      var system = await repo.System.GetAsync();
      if (system.GameDateTime.Month != 1)
      {
        return;
      }

      var infos = CharacterItemInfoes.GetAll();

      var items = new List<CharacterItem>();
      foreach (var item in infos.Where(i => i.RegenerateYears != null && i.RegenerateYears.Contains(system.GameDateTime.Year)).SelectMany(i => GenerateItems(i, towns)))
      {
        items.Add(item);
      }

      if (items.Any())
      {
        await repo.CharacterItem.AddAsync(items);
        await repo.SaveChangesAsync();
        await StatusStreaming.Default.SendAllAsync(items.Select(i => ApiData.From(i)));
      }
    }

    private static IReadOnlyList<CharacterItem> GenerateItems(CharacterItemInfo info, IReadOnlyList<Town> towns)
    {
      var items = new List<CharacterItem>();

      var num = info.InitializeNumber;
      if (info.RarePerPeriod > 1 && RandomService.Next(0, info.RarePerPeriod) > 0)
      {
        num = 0;
      }

      if (num > 0)
      {
        if (info.RareType == CharacterItemRareType.EventOnly)
        {
          for (var i = 0; i < num; i++)
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
            hiddenCount = num;
          }
          else if (info.RareType == CharacterItemRareType.TownOnSaleOnly)
          {
            saleCount = num;
          }
          else if (info.RareType == CharacterItemRareType.TownOnSaleOrHidden)
          {
            hiddenCount = RandomService.Next(0, num + 1);
            saleCount = num - hiddenCount;
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

      return items;
    }

    public static async Task GenerateItemAndSaveAsync(MainRepository repo, CharacterItem item)
    {
      var info = item.GetInfo();
      if (info.HasData)
      {
        if (info.Data.IsUniqueCharacter)
        {
          item.UniqueCharacterId = item.CharacterId;
        }
      }

      await repo.CharacterItem.AddAsync(item);
      await repo.SaveChangesAsync();
      await StatusStreaming.Default.SendAllAsync(ApiData.From(item));
    }
  }
}
