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
  [Table("character_skills")]
  public class CharacterSkill
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("type")]
    [JsonIgnore]
    public CharacterSkillType Type { get; set; }

    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (CharacterSkillType)value;
    }

    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    [Column("status")]
    [JsonIgnore]
    public CharacterSkillStatus Status { get; set; }

    [NotMapped]
    [JsonProperty("status")]
    public short ApiStatus
    {
      get => (short)this.Status;
      set => this.Status = (CharacterSkillStatus)value;
    }
  }

  public enum CharacterSkillStatus : short
  {
    Undefined = 0,
    Available = 1,
  }

  public enum CharacterSkillType : short
  {
    Strong1 = 1,
    Strong2 = 2,
    Strong3 = 3,
    Strong4 = 4,
    Strong5 = 5,
    Intellect1 = 6,
    Intellect2 = 7,
    Intellect3 = 8,
    Intellect4 = 9,
    Intellect5 = 10,
    Merchant1 = 11,
    Merchant2 = 12,
    Merchant3 = 13,
    Merchant4 = 14,
    Merchant5 = 15,
  }

  public enum CharacterSkillEffectType
  {
    Strong,
    Intellect,
    Leadership,
    Popularity,
    StrongExRegularly,
    IntellectExRegularly,
    LeadershipExRegularly,
    PopularityExRegularly,
    SoldierDiscountPercentage,
    SoldierType,
    SoldierCorrection,
    ItemMax,
    ItemDiscountPercentage,
    ItemAppearOnDomesticAffairThousandth,
    Command,
    RiceBuyMax,
    RiceBuyContribution,
    DomesticAffairMulPercentage,
    PolicyBoostProbabilityThousandth,
    GenerateItem,
  }

  public class CharacterSkillEffect
  {
    public CharacterSkillEffectType Type { get; set; }
    public int Value { get; set; }
    public CharacterSoldierTypeData SoldierTypeData { get; set; }
  }

  public class CharacterSkillInfo
  {
    public CharacterSkillType Type { get; set; }
    public string Name { get; set; }
    public int RequestedPoint { get; set; }
    public Func<IEnumerable<CharacterSkill>, bool> SubjectAppear { get; set; }
    public IList<CharacterSkillEffect> Effects { get; set; }
  }

  public static class CharacterSkillInfoes
  {
    private static readonly IList<CharacterSkillInfo> infos = new List<CharacterSkillInfo>
    {
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Strong1,
        RequestedPoint = 0,
        Name = "武家 Lv.1",
        SubjectAppear = skills => false,
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.StrongExRegularly,
            Value = 7,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Strong2,
        RequestedPoint = 10,
        Name = "武家 Lv.2",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Strong1),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.SoldierCorrection,
            SoldierTypeData = new CharacterSoldierTypeData
            {
              BaseAttack = 20,
            },
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Strong3,
        RequestedPoint = 10,
        Name = "武家 Lv.3",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Strong2),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.SoldierDiscountPercentage,
            Value = 15,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Strong4,
        RequestedPoint = 10,
        Name = "武家 Lv.4",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Strong3),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.SoldierType,
            Value = (int)SoldierType.RepeatingCrossbow,
          },
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.SoldierCorrection,
            SoldierTypeData = new CharacterSoldierTypeData
            {
              BaseAttack = 20,
              RushProbability = 200,
              RushAttack = 80,
            },
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Strong5,
        RequestedPoint = 10,
        Name = "武家 Lv.5",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Strong4),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.Command,
            Value = (int)CharacterCommandType.TownPatrol,
          },
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.SoldierCorrection,
            SoldierTypeData = new CharacterSoldierTypeData
            {
              ContinuousProbabilityOnSingleTurn = 8000,
            },
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Intellect1,
        RequestedPoint = 0,
        Name = "官吏 Lv.1",
        SubjectAppear = skills => false,
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.IntellectExRegularly,
            Value = 7,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Intellect2,
        RequestedPoint = 8,
        Name = "官吏 Lv.2",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Intellect1),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.DomesticAffairMulPercentage,
            Value = 40,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Intellect3,
        RequestedPoint = 8,
        Name = "官吏 Lv.3",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Intellect2),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.PolicyBoostProbabilityThousandth,
            Value = 5,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Intellect4,
        RequestedPoint = 12,
        Name = "官吏 Lv.4",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Intellect3),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.SoldierCorrection,
            SoldierTypeData = new CharacterSoldierTypeData
            {
              TypeGuardAttack = 20,
              TypeGuardDefend = 40,
            },
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Intellect5,
        RequestedPoint = 12,
        Name = "官吏 Lv.5",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Intellect4),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.DomesticAffairMulPercentage,
            Value = 40,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Merchant1,
        RequestedPoint = 0,
        Name = "商人 Lv.1",
        SubjectAppear = skills => false,
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.ItemMax,
            Value = 2,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Merchant2,
        RequestedPoint = 11,
        Name = "商人 Lv.2",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Merchant1),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.RiceBuyMax,
            Value = 5000,
          },
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.RiceBuyContribution,
            Value = 15,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Merchant3,
        RequestedPoint = 10,
        Name = "商人 Lv.3",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Merchant2),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.ItemDiscountPercentage,
            Value = 20,
          },
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.ItemAppearOnDomesticAffairThousandth,
            Value = 4,
          },
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.ItemMax,
            Value = 2,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Merchant4,
        RequestedPoint = 10,
        Name = "商人 Lv.4",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Merchant3),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.Command,
            Value = (int)CharacterCommandType.TownInvest,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Merchant5,
        RequestedPoint = 9,
        Name = "商人 Lv.5",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Merchant4),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.RiceBuyMax,
            Value = 3000,
          },
        },
      },
    };

    public static Optional<CharacterSkillInfo> Get(CharacterSkillType type)
    {
      return infos.FirstOrDefault(i => i.Type == type).ToOptional();
    }

    private static Optional<CharacterSkillInfo> GetInfo(this CharacterSkill item)
    {
      return infos.FirstOrDefault(i => i.Type == item.Type).ToOptional();
    }

    private static IEnumerable<CharacterSkillInfo> GetInfos(this IEnumerable<CharacterSkill> items)
    {
      return items.Join(infos, i => i.Type, i => i.Type, (it, inn) => inn);
    }

    public static bool AnySkillEffects(this IEnumerable<CharacterSkill> items, CharacterSkillEffectType type)
    {
      return items.GetInfos().SelectMany(i => i.Effects).Any(e => e.Type == type);
    }

    public static bool AnySkillEffects(this IEnumerable<CharacterSkill> items, CharacterSkillEffectType type, int value)
    {
      return items.GetInfos().SelectMany(i => i.Effects).Any(e => e.Type == type && e.Value == value);
    }

    public static int GetSumOfValues(this IEnumerable<CharacterSkill> items, CharacterSkillEffectType type)
    {
      var effects = items.GetInfos().SelectMany(i => i.Effects).Where(e => e.Type == type);
      return effects.Any() ? effects.Sum(e => e.Value) : 0;
    }

    public static int GetSumOfValues(this CharacterSkill item, CharacterSkillEffectType type)
    {
      var info = item.GetInfo();
      if (info.HasData && info.Data.Effects.Any(e => e.Type == type))
      {
        return info.Data.Effects.Where(e => e.Type == type).Sum(e => e.Value);
      }
      return 0;
    }

    public static CharacterSoldierTypeData GetSoldierTypeData(this IEnumerable<CharacterSkill> items)
    {
      var effects = items.GetInfos().SelectMany(i => i.Effects).Where(e => e.Type == CharacterSkillEffectType.SoldierCorrection);
      var correction = new CharacterSoldierTypeData();
      foreach (var e in effects)
      {
        correction.Append(e.SoldierTypeData);
      }
      return correction;
    }
  }
}
