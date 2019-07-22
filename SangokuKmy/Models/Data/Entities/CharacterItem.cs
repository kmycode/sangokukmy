﻿using Newtonsoft.Json;
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

    [Column("resource")]
    [JsonProperty("resource")]
    public ushort Resource { get; set; }
  }

  public enum CharacterItemStatus : short
  {
    Unknown = 0,
    TownOnSale = 1,
    TownHidden = 2,
    CharacterHold = 3,
    Hidden = 4,
    CharacterPending = 5,
    CharacterSpent = 6,
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
    Jurokkokushunshu = 35,
    Shunshusashiden = 36,
    Ryoshishunshu = 37,
    Goetsushunshu = 38,
    Sengokusaku = 39,
    Shiki = 40,
    Sangokushi = 41,
    Shokanron = 42,
    Ekikyo = 43,
    Shokyo = 44,
    Shikyo = 45,
    Gakkyo = 46,
    Reiki = 47,
    Cha = 48,
    Seiyukokusokan = 49,
    Shuhai = 50,
    Samban = 51,
    Ryoshikyo = 52,
    Hakusanro = 53,
    Kinzokannoko = 54,
    Gyokushimompeki = 55,
    Kyushaku = 56,
    Kashinoheki = 57,
    Chukoetsu = 58,
    EquippedGeki = 59,
    EquippedHorse = 60,
  }

  public enum CharacterItemEffectType
  {
    Strong,
    Intellect,
    Leadership,
    Popularity,
    Money,
    TerroristEnemy,
    DiscountSoldierPercentageWithResource
  }

  public enum CharacterItemRareType
  {
    TownOnSaleOrHidden,
    TownHiddenOnly,
    TownOnSaleOnly,
    EventOnly,
    NotExists,
  }

  public class CharacterItemEffect
  {
    public CharacterItemEffectType Type { get; set; }
    public int Value { get; set; }
  }

  public class CharacterResourceItemEffect : CharacterItemEffect
  {
    public List<SoldierType> DiscountSoldierTypes { get; set; }
  }

  public class CharacterItemInfo
  {
    public CharacterItemType Type { get; set; }
    public string Name { get; set; }
    public int Money { get; set; }
    public int MoneyPerResource { get; set; }
    public int InitializeNumber { get; set; }
    public CharacterItemRareType RareType { get; set; }
    public int DefaultResource { get; set; } = 0;
    public bool IsResource { get; set; } = false;
    public int ResourceLevel { get; set; }
    public bool CanSell { get; set; } = true;
    public bool CanHandOver { get; set; } = true;
    public bool CanUse { get; set; } = false;
    public int GenerateMoney { get; set; } = 0;
    public IList<CharacterItemEffect> Effects { get; set; }
    public IList<CharacterItemEffect> UsingEffects { get; set; }
    public IList<CharacterFrom> DiscoverFroms { get; set; }

    public int GetMoney(CharacterItem item)
    {
      if (!this.IsResource)
      {
        return this.Money;
      }
      return item.Resource * this.MoneyPerResource;
    }
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
        InitializeNumber = 0,
        RareType = CharacterItemRareType.NotExists,
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
        InitializeNumber = 0,
        RareType = CharacterItemRareType.NotExists,
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
        InitializeNumber = 0,
        RareType = CharacterItemRareType.NotExists,
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
        InitializeNumber = 3,
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
        Type = CharacterItemType.Satekkyu,
        Name = "鎖鉄球",
        Money = 15000,
        InitializeNumber = 3,
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
        InitializeNumber = 3,
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
        InitializeNumber = 3,
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
        InitializeNumber = 3,
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
        InitializeNumber = 4,
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
        InitializeNumber = 4,
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
        InitializeNumber = 4,
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
        InitializeNumber = 4,
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
        InitializeNumber = 4,
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
        InitializeNumber = 4,
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
        InitializeNumber = 4,
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
        Type = CharacterItemType.Jurokkokushunshu,
        Name = "十六国春秋",
        Money = 28000,
        InitializeNumber = 4,
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
        Type = CharacterItemType.Shunshusashiden,
        Name = "春秋左氏伝",
        Money = 28000,
        InitializeNumber = 4,
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
        Type = CharacterItemType.Ryoshishunshu,
        Name = "呂氏春秋",
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
        Type = CharacterItemType.Goetsushunshu,
        Name = "呉越春秋",
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
        InitializeNumber = 2,
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
        Type = CharacterItemType.Sengokusaku,
        Name = "戦国策",
        Money = 78000,
        InitializeNumber = 2,
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
        Type = CharacterItemType.Shiki,
        Name = "史記",
        Money = 78000,
        InitializeNumber = 2,
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
      new CharacterItemInfo
      {
        Type = CharacterItemType.Sangokushi,
        Name = "三国志",
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
        DiscoverFroms = new List<CharacterFrom>
        {
          CharacterFrom.Civilian,
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Shokanron,
        Name = "傷寒論",
        Money = 36000,
        InitializeNumber = 8,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Popularity,
            Value = 10,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Ekikyo,
        Name = "易経",
        Money = 55000,
        InitializeNumber = 4,
        RareType = CharacterItemRareType.TownHiddenOnly,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Leadership,
            Value = 10,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Shokyo,
        Name = "書経",
        Money = 55000,
        InitializeNumber = 4,
        RareType = CharacterItemRareType.TownHiddenOnly,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Leadership,
            Value = 10,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Shikyo,
        Name = "詩経",
        Money = 55000,
        InitializeNumber = 4,
        RareType = CharacterItemRareType.TownHiddenOnly,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Leadership,
            Value = 10,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Gakkyo,
        Name = "楽経",
        Money = 55000,
        InitializeNumber = 4,
        RareType = CharacterItemRareType.TownHiddenOnly,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Leadership,
            Value = 10,
          },
        },
        DiscoverFroms = new List<CharacterFrom>
        {
          CharacterFrom.Civilian,
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Reiki,
        Name = "礼記",
        Money = 55000,
        InitializeNumber = 4,
        RareType = CharacterItemRareType.TownHiddenOnly,
        Effects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Leadership,
            Value = 10,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Cha,
        Name = "茶",
        Money = 10000,
        InitializeNumber = 8,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanUse = true,
        CanSell = false,
        UsingEffects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Money,
            Value = 100_000,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Seiyukokusokan,
        Name = "青釉穀倉罐",
        Money = 10000,
        InitializeNumber = 8,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanUse = true,
        CanSell = false,
        UsingEffects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Money,
            Value = 100_000,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Shuhai,
        Name = "酒杯",
        Money = 10000,
        InitializeNumber = 8,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanUse = true,
        CanSell = false,
        UsingEffects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Money,
            Value = 100_000,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Samban,
        Name = "算盤",
        Money = 20000,
        InitializeNumber = 4,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanUse = true,
        CanSell = false,
        UsingEffects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Money,
            Value = 200_000,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Ryoshikyo,
        Name = "呂氏鏡",
        Money = 20000,
        InitializeNumber = 4,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanUse = true,
        CanSell = false,
        UsingEffects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Money,
            Value = 200_000,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Hakusanro,
        Name = "博山炉",
        Money = 30000,
        InitializeNumber = 2,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanUse = true,
        CanSell = false,
        UsingEffects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Money,
            Value = 300_000,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Kinzokannoko,
        Name = "金象嵌の壺",
        Money = 30000,
        InitializeNumber = 2,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanUse = true,
        CanSell = false,
        UsingEffects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Money,
            Value = 300_000,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Gyokushimompeki,
        Name = "玉龍紋璧",
        Money = 30000,
        InitializeNumber = 2,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanUse = true,
        CanSell = false,
        UsingEffects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Money,
            Value = 300_000,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Kyushaku,
        Name = "九錫",
        Money = 50000,
        InitializeNumber = 1,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanUse = true,
        CanSell = false,
        UsingEffects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Money,
            Value = 500_000,
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Kashinoheki,
        Name = "和氏の璧",
        Money = 200000,
        InitializeNumber = 1,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanUse = true,
        CanSell = false,
        UsingEffects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.Money,
            Value = 2_000_000,
          },
        },
        DiscoverFroms = new List<CharacterFrom>
        {
          CharacterFrom.Merchant,
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.Chukoetsu,
        Name = "中行説の霊",
        Money = 5_000_000,
        InitializeNumber = 1,
        RareType = CharacterItemRareType.TownHiddenOnly,
        CanUse = true,
        CanSell = false,
        UsingEffects = new List<CharacterItemEffect>
        {
          new CharacterItemEffect
          {
            Type = CharacterItemEffectType.TerroristEnemy,
          },
        },
        DiscoverFroms = new List<CharacterFrom>
        {
          CharacterFrom.Warrior,
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.EquippedGeki,
        Name = "装備戟",
        IsResource = true,
        ResourceLevel = 1,
        MoneyPerResource = 18,
        InitializeNumber = 12,
        DefaultResource = 1000,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        UsingEffects = new List<CharacterItemEffect>
        {
          new CharacterResourceItemEffect
          {
            Type = CharacterItemEffectType.DiscountSoldierPercentageWithResource,
            Value = 30,
            DiscountSoldierTypes = new List<SoldierType>
            {
              SoldierType.HeavyInfantry,
            },
          },
        },
      },
      new CharacterItemInfo
      {
        Type = CharacterItemType.EquippedHorse,
        Name = "装備馬",
        IsResource = true,
        ResourceLevel = 1,
        MoneyPerResource = 18,
        InitializeNumber = 12,
        DefaultResource = 1000,
        RareType = CharacterItemRareType.TownOnSaleOrHidden,
        UsingEffects = new List<CharacterItemEffect>
        {
          new CharacterResourceItemEffect
          {
            Type = CharacterItemEffectType.DiscountSoldierPercentageWithResource,
            Value = 30,
            DiscountSoldierTypes = new List<SoldierType>
            {
              SoldierType.HeavyCavalry,
            },
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
      if (info.HasData && info.Data.Effects != null && info.Data.Effects.Any(e => e.Type == type))
      {
        return info.Data.Effects.Where(e => e.Type == type).Sum(e => e.Value);
      }
      return 0;
    }

    public static int GetSumOfUsingValues(this CharacterItem item, CharacterItemEffectType type)
    {
      var info = item.GetInfo();
      if (info.HasData && info.Data.UsingEffects != null && info.Data.UsingEffects.Any(e => e.Type == type))
      {
        return info.Data.UsingEffects.Where(e => e.Type == type).Sum(e => e.Value);
      }
      return 0;
    }

    public static IEnumerable<(CharacterItem Item, CharacterItemInfo Info, CharacterResourceItemEffect Effect)> GetResources(this IEnumerable<CharacterItem> item, CharacterItemEffectType type, Predicate<CharacterResourceItemEffect> subject, int size)
    {
      var targets = item
        .Join(infos, i => i.Type, i => i.Type, (it, inn) => new { Item = it, Info = inn, Effects = inn.UsingEffects?.OfType<CharacterResourceItemEffect>().Where(ie => ie.Type == type && subject(ie)) ?? Enumerable.Empty<CharacterResourceItemEffect>(), })
        .Where(i => i.Effects.Any())
        // 並べ替え時は残量よりもレベルを優先する（安定ソート前提）
        .OrderBy(i => i.Item.Resource)
        .OrderBy(i => i.Info.ResourceLevel);

      foreach (var target in targets)
      {
        yield return (target.Item, target.Info, target.Effects.FirstOrDefault());
        size -= target.Item.Resource;
        if (size <= 0)
        {
          break;
        }
      }
    }
  }
}
