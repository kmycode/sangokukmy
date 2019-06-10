using Newtonsoft.Json;
using SangokuKmy.Common;
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
  }

  public static class CharacterItemInfoes
  {
    private static readonly CharacterItemInfo[] infos = new CharacterItemInfo[]
    {
      new CharacterItemInfo
      {
        Type = CharacterItemType.Yari,
        Name = "槍",
        Money = 1000,
        InitializeNumber = 5,
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
