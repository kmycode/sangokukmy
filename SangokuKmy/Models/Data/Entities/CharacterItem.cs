using Newtonsoft.Json;
using SangokuKmy.Common;
using SangokuKmy.Models.Data.ApiEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("character_items")]
  public class CharacterItem
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("type")]
    [JsonIgnore]
    public CharacterItemType Type { get; set; }

    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (CharacterItemType)value;
    }

    [Column("status")]
    [JsonIgnore]
    public CharacterItemStatus Status { get; set; }

    [NotMapped]
    [JsonProperty("status")]
    public short ApiStatus
    {
      get => (short)this.Status;
      set => this.Status = (CharacterItemStatus)value;
    }

    [NotMapped]
    [JsonProperty("lastStatusChangedGameDate")]
    public GameDateTime LastStatusChangedGameDate
    {
      get => GameDateTime.FromInt(this.IntLastStatusChangedGameDate);
      set => this.IntLastStatusChangedGameDate = value.ToInt();
    }

    [Column("last_status_changed_game_date")]
    [JsonIgnore]
    public int IntLastStatusChangedGameDate { get; set; }

    [Column("town_id")]
    [JsonIgnore]
    public uint TownId { get; set; }

    [NotMapped]
    [JsonProperty("townId")]
    public uint ApiTownId
    {
      get
      {
        if (this.Status == CharacterItemStatus.TownOnSale)
        {
          return this.TownId;
        }
        return default;
      }
    }

    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }
  }

  public enum CharacterItemStatus : short
  {
    Unknown = 0,
    TownOnSale = 1,
    TownHidden = 2,
    CharacterHold = 3,
    Hidden = 4,
    CharacterPending = 5,
  }

  public enum CharacterItemType : short
  {
    Unknown = 0,
    Yari = 1,
    Geki = 2,
    Yumi = 3,
    Ohkanaduchi = 4,
    Sansenryohato = 5,
    Satekkyu = 6,
    Kiringa = 7,
    Ryuseitsuchi = 8,
    Kinjaken = 9,
    Hien = 10,
    Kojoto = 11,
    Ryuyoto = 12,
    Hosuto = 13,
    Shiyusai = 14,
    Gentetsuken = 15,
    Hekimeiken = 16,
    Seikonoken = 17,
    Kitenken = 18,
    Shiyuittsuinoken = 19,
    Ryotan = 20,
    Seiryusekigetsuto = 21,
    Hotengagi = 22,
    Jinryunoken = 23,
    Heibanshishozu = 24,
    Taiheiyoushutsunosho = 25,
    Bokushi = 26,
    Rokkan = 27,
    Seishokuchikeizu = 28,
    Tonkotensho = 29,
    Sonshinoheihosho = 30,
    Seinosho = 31,
    Motokushinsho = 32,
    Heihonijuyompen = 33,
    Shinkoshinsho = 34,
  }

  public enum CharacterItemEffectType
  {
    Strong,
    Intellect,
    Leadership,
    Popularity,
  }

  public enum CharacterItemRareType
  {
    TownOnSaleOrHidden,
    TownHiddenOnly,
    TownOnSaleOnly,
    EventOnly,
  }

  public class CharacterItemEffect
  {
    public CharacterItemEffectType Type { get; set; }
    public int Value { get; set; }
  }

  public class CharacterItemInfo
  {
    public CharacterItemType Type { get; set; }
    public string Name { get; set; }
    public int Money { get; set; }
    public int InitializeNumber { get; set; }
    public CharacterItemRareType RareType { get; set; }
    public bool CanSell { get; set; } = true;
    public bool CanHandOver { get; set; } = true;
    public IList<CharacterItemEffect> Effects { get; set; }
    public IList<CharacterFrom> DiscoverFroms { get; set; }
  }

  public static class CharacterItemInfoes
  {
    private static readonly CharacterItemInfo[] infos = new CharacterItemInfo[]
    {
      new CharacterItemInfo
      {
        Type = CharacterItemType.Yari,
        Name = "槍",
        Money = 5000,
        InitializeNumber = 8,
        RareType = CharacterItemRareType.TownOnSaleOnly,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 1,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Geki,
        Name = "戟",
        Money = 5000,
        InitializeNumber = 8,
        RareType = CharacterItemRareType.TownOnSaleOnly,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 1,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Yumi,
        Name = "弓",
        Money = 5000,
        InitializeNumber = 8,
        RareType = CharacterItemRareType.TownOnSaleOnly,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 1,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Ohkanaduchi,
        Name = "大金槌",
        Money = 5000,
        InitializeNumber = 8,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 1,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Sansenryohato,
        Name = "三尖両刃刀",
        Money = 15000,
        InitializeNumber = 6,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 3,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Satekkyu,
        Name = "鎖鉄球",
        Money = 15000,
        InitializeNumber = 6,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 3,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Kiringa,
        Name = "麒麟牙",
        Money = 15000,
        InitializeNumber = 6,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 3,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Ryuseitsuchi,
        Name = "流星槌",
        Money = 15000,
        InitializeNumber = 6,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 3,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Kinjaken,
        Name = "金蛇剣",
        Money = 15000,
        InitializeNumber = 4,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 3,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Hien,
        Name = "飛燕",
        Money = 28000,
        InitializeNumber = 3,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 5,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Kojoto,
        Name = "古錠刀",
        Money = 28000,
        InitializeNumber = 3,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 5,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Ryuyoto,
        Name = "柳葉刀",
        Money = 28000,
        InitializeNumber = 3,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 5,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Hosuto,
        Name = "鳳嘴刀",
        Money = 28000,
        InitializeNumber = 3,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 5,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Shiyusai,
        Name = "蚩尤砕",
        Money = 55000,
        InitializeNumber = 2,
        RareType = CharacterItemRareType.TownHiddenOnly,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 10,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Gentetsuken,
        Name = "玄鉄剣",
        Money = 55000,
        InitializeNumber = 2,
        RareType = CharacterItemRareType.TownHiddenOnly,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 10,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Hekimeiken,
        Name = "碧銘剣",
        Money = 55000,
        InitializeNumber = 2,
        RareType = CharacterItemRareType.TownHiddenOnly,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 10,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Seikonoken,
        Name = "青紅の剣",
        Money = 55000,
        InitializeNumber = 2,
        RareType = CharacterItemRareType.TownHiddenOnly,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 10,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Kitenken,
        Name = "倚天剣",
        Money = 55000,
        InitializeNumber = 2,
        RareType = CharacterItemRareType.TownHiddenOnly,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 10,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Shiyuittsuinoken,
        Name = "雌雄一対の剣",
        Money = 78000,
        InitializeNumber = 2,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanSell = false,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 15,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Ryotan,
        Name = "竜胆",
        Money = 78000,
        InitializeNumber = 2,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanSell = false,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 15,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Seiryusekigetsuto,
        Name = "青龍堰月刀",
        Money = 78000,
        InitializeNumber = 2,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanSell = false,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 15,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Hotengagi,
        Name = "方天画戟",
        Money = 105000,
        InitializeNumber = 1,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanSell = false,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 20,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Jinryunoken,
        Name = "神龍の剣",
        Money = 105000,
        InitializeNumber = 1,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanSell = false,
        CanHandOver = false,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Strong,
            Value = 20,
          },
        },
        DiscoverFroms = new List<CharacterFrom>
        {
          CharacterFrom.Warrior,
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Heibanshishozu,
        Name = "平蛮指掌図",
        Money = 5000,
        InitializeNumber = 8,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Intellect,
            Value = 1,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Taiheiyoushutsunosho,
        Name = "太平妖術の書",
        Money = 5000,
        InitializeNumber = 8,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Intellect,
            Value = 1,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Bokushi,
        Name = "墨子",
        Money = 15000,
        InitializeNumber = 8,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Intellect,
            Value = 3,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Rokkan,
        Name = "六韜",
        Money = 15000,
        InitializeNumber = 8,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Intellect,
            Value = 3,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Seishokuchikeizu,
        Name = "西蜀地形図",
        Money = 28000,
        InitializeNumber = 8,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Intellect,
            Value = 5,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Tonkotensho,
        Name = "遁甲天書",
        Money = 28000,
        InitializeNumber = 8,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Intellect,
            Value = 5,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Sonshinoheihosho,
        Name = "孫子の兵法書",
        Money = 28000,
        InitializeNumber = 8,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Intellect,
            Value = 5,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Seinosho,
        Name = "青嚢書",
        Money = 55000,
        InitializeNumber = 4,
        RareType = CharacterItemRareType.TownHiddenOnly,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Intellect,
            Value = 10,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Motokushinsho,
        Name = "孟徳新書",
        Money = 55000,
        InitializeNumber = 3,
        RareType = CharacterItemRareType.TownHiddenOnly,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Intellect,
            Value = 10,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Heihonijuyompen,
        Name = "兵法二十四編",
        Money = 78000,
        InitializeNumber = 1,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanSell = false,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Intellect,
            Value = 15,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Shinkoshinsho,
        Name = "信仰新書",
        Money = 105000,
        InitializeNumber = 1,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanSell = false,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Intellect,
            Value = 20,
          },
        },
      },
    };

    public static IReadOnlyList<CharacterItemInfo> GetAll()
    {
      return infos;
    }

    public static Optional<CharacterItemInfo> Get(CharacterItemType type)
    {
      return infos.FirstOrDefault(i => i.Type == type).ToOptional();
    }

    public static Optional<CharacterItemInfo> GetInfo(this CharacterItem item)
    {
      return infos.FirstOrDefault(i => i.Type == item.Type).ToOptional();
    }

    public static IEnumerable<CharacterItemInfo> GetInfos(this IEnumerable<CharacterItem> items)
    {
      return items.Join(infos, i => i.Type, i => i.Type, (it, inn) => inn);
    }

    public static int GetSumOfValues(this IEnumerable<CharacterItem> items, CharacterItemEffectType type)
    {
      var effects = items.GetInfos().SelectMany(i => i.Effects).Where(e => e.Type == type);
      return effects.Any() ? effects.Sum(e => e.Value) : 0;
    }

    public static int GetSumOfValues(this CharacterItem item, CharacterItemEffectType type)
    {
      var info = item.GetInfo();
      if (info.HasData && info.Data.Effects.Any(e => e.Type == type))
      {
        return info.Data.Effects.Where(e => e.Type == type).Sum(e => e.Value);
      }
      return 0;
    }
  }
}
