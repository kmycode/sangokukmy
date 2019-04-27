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

    [Column("status")]
    [JsonIgnore]
    public CountryPolicyStatus Status { get; set; }

    [NotMapped]
    [JsonProperty("status")]
    public short ApiStatus
    {
      get => (short)this.Status;
      set => this.Status = (CountryPolicyStatus)value;
    }
  }

  public enum CountryPolicyStatus : short
  {
    Unadopted = 0,
    Available = 1,
    Boosting = 2,
    Boosted = 3,
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

    /// <summary>
    /// 賊の殲滅
    /// </summary>
    KillGang = 13,

    /// <summary>
    /// 武官国家
    /// </summary>
    StrongCountry = 14,

    /// <summary>
    /// 文官国家
    /// </summary>
    IntellectCountry = 15,

    /// <summary>
    /// 人情国家
    /// </summary>
    PopularityCountry = 16,

    /// <summary>
    /// 農業国家
    /// </summary>
    AgricultureCountry = 17,

    /// <summary>
    /// 商業国家
    /// </summary>
    CommercialCountry = 18,

    /// <summary>
    /// 城塞国家
    /// </summary>
    WallCountry = 19,
  }

  public class CountryPolicyTypeInfo
  {
    public CountryPolicyType Type { get; set; }

    public string Name { get; set; }

    public int BasePoint { get; set; }

    public Func<IEnumerable<CountryPolicyType>, bool> SubjectAppear { get; set; }

    public int GetRequestedPoint(CountryPolicyStatus status)
    {
      switch (status)
      {
        case CountryPolicyStatus.Available:
          return 0;
        case CountryPolicyStatus.Boosted:
          return this.BasePoint / 2;
        default:
          return this.BasePoint;
      }
    }
  }

  public static class CountryPolicyTypeInfoes
  {
    private static List<CountryPolicyTypeInfo> infoes = new List<CountryPolicyTypeInfo>
    {
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.Storage,
        Name = "貯蔵",
        BasePoint = 4000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.Scouter,
        Name = "密偵",
        BasePoint = 4000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.SoldierDevelopment,
        Name = "兵種開発",
        BasePoint = 500_0000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.HumanDevelopment,
        Name = "人材開発",
        BasePoint = 4000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.Economy,
        Name = "経済評論",
        BasePoint = 2000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.SaveWall,
        Name = "災害対策",
        BasePoint = 4000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.AntiGang,
        Name = "賊の監視",
        BasePoint = 4000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.BattleContinuous,
        Name = "連戦戦術",
        BasePoint = 500_0000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.BattleRush,
        Name = "突撃戦術",
        BasePoint = 500_0000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.UndergroundStorage,
        Name = "地下貯蔵",
        BasePoint = 4000,
        SubjectAppear = list => list.Contains(CountryPolicyType.Storage),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.StomachStorage,
        Name = "胃の中",
        BasePoint = 4000,
        SubjectAppear = list => list.Contains(CountryPolicyType.UndergroundStorage),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.BloodVesselsStorage,
        Name = "血管の中",
        BasePoint = 4000,
        SubjectAppear = list => list.Contains(CountryPolicyType.StomachStorage),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.KillGang,
        Name = "賊の殲滅",
        BasePoint = 2000,
        SubjectAppear = list => list.Contains(CountryPolicyType.AntiGang),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.StrongCountry,
        Name = "武官国家",
        BasePoint = 1500,
        SubjectAppear = list => list.Contains(CountryPolicyType.HumanDevelopment),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.IntellectCountry,
        Name = "文官国家",
        BasePoint = 2000,
        SubjectAppear = list => list.Contains(CountryPolicyType.HumanDevelopment),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.PopularityCountry,
        Name = "仁官国家",
        BasePoint = 2000,
        SubjectAppear = list => list.Contains(CountryPolicyType.IntellectCountry),
      },
    };

    public static Optional<CountryPolicyTypeInfo> Get(CountryPolicyType type)
    {
      return infoes.FirstOrDefault(i => i.Type == type).ToOptional();
    }
  }
}
