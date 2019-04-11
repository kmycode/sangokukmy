using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using SangokuKmy.Common;
using System.Linq;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("country_policies")]
  public class CountryPolicy
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("country_id")]
    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    [Column("type")]
    [JsonIgnore]
    public CountryPolicyType Type { get; set; }

    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (CountryPolicyType)value;
    }
  }

  public enum CountryPolicyType : short
  {
    Unknown = 0,

    /// <summary>
    /// 貯蔵
    /// </summary>
    Storage = 1,

    /// <summary>
    /// 密偵
    /// </summary>
    Scouter = 2,

    /// <summary>
    /// 兵種開発
    /// </summary>
    SoldierDevelopment = 3,

    /// <summary>
    /// 人材開発
    /// </summary>
    HumanDevelopment = 4,

    /// <summary>
    /// 経済評論
    /// </summary>
    Economy = 5,

    /// <summary>
    /// 災害対策
    /// </summary>
    SaveWall = 6,

    /// <summary>
    /// 賊の監視
    /// </summary>
    AntiGang = 7,

    /// <summary>
    /// 連戦
    /// </summary>
    BattleContinuous = 8,

    /// <summary>
    /// 突撃
    /// </summary>
    BattleRush = 9,

    /// <summary>
    /// 国庫拡張
    /// </summary>
    UndergroundStorage = 10,

    /// <summary>
    /// 国庫拡張2
    /// </summary>
    StomachStorage = 11,

    /// <summary>
    /// 国庫拡張3
    /// </summary>
    BloodVesselsStorage = 12,
  }

  public class CountryPolicyTypeInfo
  {
    public CountryPolicyType Type { get; set; }

    public string Name { get; set; }

    public int RequestedPoint { get; set; }

    public Func<IEnumerable<CountryPolicyType>, bool> SubjectAppear { get; set; }
  }

  public static class CountryPolicyTypeInfoes
  {
    private static List<CountryPolicyTypeInfo> infoes = new List<CountryPolicyTypeInfo>
    {
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.Storage,
        Name = "貯蔵",
        RequestedPoint = 4000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.Scouter,
        Name = "密偵",
        RequestedPoint = 4000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.SoldierDevelopment,
        Name = "兵種開発",
        RequestedPoint = 500_0000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.HumanDevelopment,
        Name = "人材開発",
        RequestedPoint = 4000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.Economy,
        Name = "経済評論",
        RequestedPoint = 2000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.SaveWall,
        Name = "災害対策",
        RequestedPoint = 4000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.AntiGang,
        Name = "賊の監視",
        RequestedPoint = 4000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.BattleContinuous,
        Name = "連戦戦術",
        RequestedPoint = 500_0000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.BattleRush,
        Name = "突撃戦術",
        RequestedPoint = 500_0000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.UndergroundStorage,
        Name = "地下貯蔵",
        RequestedPoint = 4000,
        SubjectAppear = list => list.Contains(CountryPolicyType.Storage),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.StomachStorage,
        Name = "胃の中",
        RequestedPoint = 4000,
        SubjectAppear = list => list.Contains(CountryPolicyType.UndergroundStorage),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.BloodVesselsStorage,
        Name = "血管の中",
        RequestedPoint = 4000,
        SubjectAppear = list => list.Contains(CountryPolicyType.StomachStorage),
      },
    };

    public static Optional<CountryPolicyTypeInfo> Get(CountryPolicyType type)
    {
      return infoes.FirstOrDefault(i => i.Type == type).ToOptional();
    }
  }
}
