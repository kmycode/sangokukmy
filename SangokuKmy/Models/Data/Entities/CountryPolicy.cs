using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;
using System.Collections.Generic;
using SangokuKmy.Common;
using System.Linq;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Common;

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

    [Column("game_date")]
    [JsonIgnore]
    public int IntGameDate { get; set; }

    [NotMapped]
    [JsonProperty("gameDate")]
    public GameDateTime GameDate
    {
      get => GameDateTime.FromInt(this.IntGameDate);
      set => this.IntGameDate = value.ToInt();
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

    /// <summary>
    /// 郡県制
    /// </summary>
    GunKen = 20,

    /// <summary>
    /// 攻防の礎
    /// </summary>
    AttackDefend = 21,

    /// <summary>
    /// 土塁
    /// </summary>
    Earthwork = 22,

    /// <summary>
    /// 石城
    /// </summary>
    StoneCastle = 23,

    /// <summary>
    /// 施設連携
    /// </summary>
    ConnectionBuildings = 24,

    /// <summary>
    /// 徴収
    /// </summary>
    Collection = 25,

    /// <summary>
    /// 壁に耳
    /// </summary>
    WallEar = 26,

    /// <summary>
    /// 増給
    /// </summary>
    AddSalary = 27,

    /// <summary>
    /// 復興支援
    /// </summary>
    HelpRepair = 28,

    /// <summary>
    /// 正義とは
    /// </summary>
    Justice = 29,

    /// <summary>
    /// げき
    /// </summary>
    JusticeMessage = 30,

    /// <summary>
    /// 攻城
    /// </summary>
    Siege = 31,

    /// <summary>
    /// 衝車常備
    /// </summary>
    Shosha = 32,

    /// <summary>
    /// 号令
    /// </summary>
    UnitOrder = 33,

    /// <summary>
    /// 採用策
    /// </summary>
    Recruitment = 34,

    /// <summary>
    /// 武官の肇
    /// </summary>
    StrongStart = 35,

    /// <summary>
    /// 障子に目
    /// </summary>
    Shoji = 36,

    /// <summary>
    /// 胡人徴発
    /// </summary>
    GetTerrorists = 37,
  }

  public enum CountryPolicyEffectType
  {
    Secretary,
    CountrySafeMax,
    CountrySafeCollectionMax,
    BoostWith,
    ResearchLevel,
  }

  public enum CountryPolicyEffectCalcType
  {
    Add,
    Mul,
  }

  public class CountryPolicyEffect
  {
    public CountryPolicyEffectType Type { get; set; }
    public int Value { get; set; }
    public CountryPolicyEffectCalcType CalcType { get; set; } = CountryPolicyEffectCalcType.Add;
  }

  public class CountryPolicyTypeInfo
  {
    public CountryPolicyType Type { get; set; }

    public string Name { get; set; }

    public int BasePoint { get; set; }

    public bool CanBoost { get; set; } = true;

    public List<CountryPolicyEffect> Effects { get; set; } = new List<CountryPolicyEffect>();

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
        Type = CountryPolicyType.GunKen,
        Name = "郡県制",
        BasePoint = 2000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.AgricultureCountry,
        Name = "農業国家",
        BasePoint = 3000,
        SubjectAppear = list => list.Contains(CountryPolicyType.GunKen),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.CommercialCountry,
        Name = "商業国家",
        BasePoint = 3000,
        SubjectAppear = list => list.Contains(CountryPolicyType.GunKen),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.WallCountry,
        Name = "城塞国家",
        BasePoint = 3000,
        SubjectAppear = list => list.Contains(CountryPolicyType.GunKen),
      },

      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.Economy,
        Name = "経済論",
        BasePoint = 4000,
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.Storage,
        Name = "貯蔵",
        BasePoint = 4000,
        SubjectAppear = list => list.Contains(CountryPolicyType.Economy),
        Effects =
        {
          new CountryPolicyEffect
          {
            Type = CountryPolicyEffectType.CountrySafeMax,
            Value = Config.CountrySafeMax,
          },
        },
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.Collection,
        Name = "徴収",
        BasePoint = 3000,
        SubjectAppear = list => list.Contains(CountryPolicyType.Storage),
        Effects =
        {
          new CountryPolicyEffect
          {
            Type = CountryPolicyEffectType.CountrySafeCollectionMax,
            Value = 2000,
          },
        },
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.AddSalary,
        Name = "増給",
        BasePoint = 2000,
        SubjectAppear = list => list.Contains(CountryPolicyType.Collection),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.UndergroundStorage,
        Name = "地下貯蔵",
        BasePoint = 2000,
        SubjectAppear = list => list.Contains(CountryPolicyType.AddSalary),
        Effects =
        {
          new CountryPolicyEffect
          {
            Type = CountryPolicyEffectType.CountrySafeMax,
            Value = Config.CountrySafeMax,
          },
        },
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.WallEar,
        Name = "壁に耳",
        BasePoint = 4000,
        SubjectAppear = list => list.Contains(CountryPolicyType.UndergroundStorage),
        Effects =
        {
          new CountryPolicyEffect
          {
            Type = CountryPolicyEffectType.CountrySafeCollectionMax,
            Value = 2,
            CalcType = CountryPolicyEffectCalcType.Mul,
          },
        },
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.StomachStorage,
        Name = "胃の中",
        BasePoint = 2000,
        SubjectAppear = list => list.Contains(CountryPolicyType.WallEar),
        Effects =
        {
          new CountryPolicyEffect
          {
            Type = CountryPolicyEffectType.CountrySafeMax,
            Value = Config.CountrySafeMax,
          },
        },
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.Shoji,
        Name = "障子に目",
        BasePoint = 4000,
        SubjectAppear = list => list.Contains(CountryPolicyType.StomachStorage),
        Effects =
        {
          new CountryPolicyEffect
          {
            Type = CountryPolicyEffectType.CountrySafeCollectionMax,
            Value = 2,
            CalcType = CountryPolicyEffectCalcType.Mul,
          },
        },
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.BloodVesselsStorage,
        Name = "血管の中",
        BasePoint = 3000,
        SubjectAppear = list => list.Contains(CountryPolicyType.Shoji),
        Effects =
        {
          new CountryPolicyEffect
          {
            Type = CountryPolicyEffectType.CountrySafeMax,
            Value = Config.CountrySafeMax,
          },
        },
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.SaveWall,
        Name = "災害対策",
        BasePoint = 3000,
        SubjectAppear = list => list.Contains(CountryPolicyType.Economy),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.HelpRepair,
        Name = "復興支援",
        BasePoint = 2000,
        SubjectAppear = list => list.Contains(CountryPolicyType.SaveWall),
      },


      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.HumanDevelopment,
        Name = "人材開発",
        BasePoint = 3000,
        Effects =
        {
          new CountryPolicyEffect
          {
            Type = CountryPolicyEffectType.Secretary,
            Value = 2,
          },
        },
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
        SubjectAppear = list => list.Contains(CountryPolicyType.StrongCountry),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.Scouter,
        Name = "密偵",
        BasePoint = 3000,
        SubjectAppear = list => list.Contains(CountryPolicyType.IntellectCountry),
        Effects =
        {
          new CountryPolicyEffect
          {
            Type = CountryPolicyEffectType.Secretary,
            Value = 1,
          },
        },
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.UnitOrder,
        Name = "号令",
        BasePoint = 3000,
        SubjectAppear = list => list.Contains(CountryPolicyType.Scouter),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.PopularityCountry,
        Name = "人情国家",
        BasePoint = 2000,
        SubjectAppear = list => list.Contains(CountryPolicyType.UnitOrder),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.ConnectionBuildings,
        Name = "施設連携",
        BasePoint = 3000,
        SubjectAppear = list => list.Contains(CountryPolicyType.PopularityCountry),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.Recruitment,
        Name = "採用策",
        BasePoint = 2000,
        SubjectAppear = list => list.Contains(CountryPolicyType.ConnectionBuildings),
        Effects =
        {
          new CountryPolicyEffect
          {
            Type = CountryPolicyEffectType.Secretary,
            Value = 1,
          },
        },
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.StrongStart,
        Name = "武官の肇",
        BasePoint = 2000,
        SubjectAppear = list => list.Contains(CountryPolicyType.StrongCountry),
        Effects =
        {
          new CountryPolicyEffect
          {
            Type = CountryPolicyEffectType.BoostWith,
            Value = (int)CountryPolicyType.IntellectCountry,
          },
        },
      },

      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.AntiGang,
        Name = "賊の監視",
        BasePoint = 3000,
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
        Type = CountryPolicyType.Justice,
        Name = "正義とは",
        BasePoint = 2000,
        SubjectAppear = list => list.Contains(CountryPolicyType.KillGang),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.Siege,
        Name = "攻城",
        BasePoint = 4000,
        SubjectAppear = list => list.Contains(CountryPolicyType.Justice),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.JusticeMessage,
        Name = "檄",
        BasePoint = 3000,
        SubjectAppear = list => list.Contains(CountryPolicyType.Siege),
      },
      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.Shosha,
        Name = "衝車常備",
        BasePoint = 4000,
        SubjectAppear = list => list.Contains(CountryPolicyType.JusticeMessage),
      },

      new CountryPolicyTypeInfo
      {
        Type = CountryPolicyType.GetTerrorists,
        Name = "胡人徴発",
        BasePoint = 0,
        SubjectAppear = list => false,
        CanBoost = false,
      },
    };

    public static Optional<CountryPolicyTypeInfo> Get(CountryPolicyType type)
    {
      return infoes.FirstOrDefault(i => i.Type == type).ToOptional();
    }

    public static IEnumerable<CountryPolicyTypeInfo> GetAll()
    {
      return infoes;
    }

    public static IEnumerable<CountryPolicyType> GetAvailableTypes(this IEnumerable<CountryPolicy> policies)
    {
      return policies.Where(p => p.Status == CountryPolicyStatus.Available).Select(p => p.Type);
    }

    private static Optional<CountryPolicyTypeInfo> GetInfo(this CountryPolicy item)
    {
      return infoes.FirstOrDefault(i => i.Type == item.Type).ToOptional();
    }

    private static IEnumerable<CountryPolicyTypeInfo> GetInfos(this IEnumerable<CountryPolicy> items)
    {
      return items.Join(infoes, i => i.Type, i => i.Type, (it, inn) => inn);
    }

    private static IEnumerable<CountryPolicyTypeInfo> GetInfos(this IEnumerable<CountryPolicyType> items)
    {
      return items.Join(infoes, i => i, i => i.Type, (it, inn) => inn);
    }

    public static bool AnySkillEffects(this IEnumerable<CountryPolicy> items, CountryPolicyEffectType type)
    {
      return items.GetInfos().SelectMany(i => i.Effects).Any(e => e.Type == type);
    }

    public static bool AnySkillEffects(this IEnumerable<CountryPolicy> items, CountryPolicyEffectType type, int value)
    {
      return items.GetInfos().SelectMany(i => i.Effects).Any(e => e.Type == type && e.Value == value);
    }

    private static int GetSumOfValues(this IEnumerable<CountryPolicyEffect> effects)
    {
      if (!effects.Any())
      {
        return 0;
      }

      var sum = 0;
      var adds = effects.Where(e => e.CalcType == CountryPolicyEffectCalcType.Add);
      if (adds.Any())
      {
        sum += adds.Sum(e => e.Value);
      }

      foreach (var effect in effects.Where(e => e.CalcType == CountryPolicyEffectCalcType.Mul))
      {
        sum *= effect.Value;
      }

      return sum;
    }

    public static int GetSumOfValues(this IEnumerable<CountryPolicyType> items, CountryPolicyEffectType type)
    {
      var effects = items.GetInfos().SelectMany(i => i.Effects).Where(e => e.Type == type);
      return effects.Any() ? effects.GetSumOfValues() : 0;
    }

    public static int GetSumOfValues(this IEnumerable<CountryPolicy> items, CountryPolicyEffectType type)
    {
      var effects = items.GetInfos().SelectMany(i => i.Effects).Where(e => e.Type == type);
      return effects.Any() ? effects.GetSumOfValues() : 0;
    }

    public static int GetSumOfValues(this CountryPolicy item, CountryPolicyEffectType type)
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
