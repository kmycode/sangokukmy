using Newtonsoft.Json;
using SangokuKmy.Common;
using System;
using System.Collections;
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
    Engineer1 = 16,
    Engineer2 = 17,
    Engineer3 = 18,
    Engineer4 = 19,
    Engineer5 = 20,
    Ai1 = 21,
    Ai2 = 22,
    Ai3 = 23,
    Ai4 = 24,
    Ai5 = 25,
    Terrorist1 = 26,
    Terrorist2 = 27,
    Terrorist3 = 28,
    Terrorist4 = 29,
    Terrorist5 = 30,
    People1 = 31,
    People2 = 32,
    People3 = 33,
    People4 = 34,
    People5 = 35,
    Tactician1 = 36,
    Tactician2 = 37,
    Tactician3 = 38,
    Tactician4 = 39,
    Tactician5 = 40,
    Scholar1 = 41,
    Scholar2 = 42,
    Scholar3 = 43,
    Scholar4 = 44,
    Scholar5 = 45,
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
    StrongOrIntellectExRegularly,
    FormationExRegularly,
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
    SecurityCommandMulPercentage,
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
        RequestedPoint = 8,
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
        RequestedPoint = 8,
        Name = "武家 Lv.4",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Strong3),
        Effects = new List<CharacterSkillEffect>
        {
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
        RequestedPoint = 9,
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
            Type = CharacterSkillEffectType.DomesticAffairMulPercentage,
            Value = 40,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Intellect4,
        RequestedPoint = 9,
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
        RequestedPoint = 10,
        Name = "官吏 Lv.5",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Intellect4),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.PolicyBoostProbabilityThousandth,
            Value = 50,
          },
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.Command,
            Value = (int)CharacterCommandType.TownPatrol,
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
        RequestedPoint = 7,
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
        RequestedPoint = 9,
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
            Value = 8000,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Engineer1,
        RequestedPoint = 0,
        Name = "技師 Lv.1",
        SubjectAppear = skills => false,
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.SoldierCorrection,
            SoldierTypeData = new CharacterSoldierTypeData
            {
              BaseAttack = 30,
            },
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Engineer2,
        RequestedPoint = 5,
        Name = "技師 Lv.2",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Engineer1),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.GenerateItem,
            Value = (int)CharacterItemType.EquippedGoodGeki,
          },
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.GenerateItem,
            Value = (int)CharacterItemType.EquippedGoodHorse,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Engineer3,
        RequestedPoint = 8,
        Name = "技師 Lv.3",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Engineer2),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.GenerateItem,
            Value = (int)CharacterItemType.EquippedRepeatingCrossbow,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Engineer4,
        RequestedPoint = 10,
        Name = "技師 Lv.4",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Engineer3),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.ItemMax,
            Value = 3,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Engineer5,
        RequestedPoint = 12,
        Name = "技師 Lv.5",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Engineer4),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.GenerateItem,
            Value = (int)CharacterItemType.EquippedSeishuYari,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Ai1,
        RequestedPoint = 0,
        Name = "AI Lv.1",
        SubjectAppear = skills => false,
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.StrongOrIntellectExRegularly,
            Value = 9,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Ai2,
        RequestedPoint = 10,
        Name = "AI Lv.2",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Ai1),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.SoldierCorrection,
            SoldierTypeData = new CharacterSoldierTypeData
            {
              BaseAttack = 40,
            },
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Ai3,
        RequestedPoint = 10,
        Name = "AI Lv.3",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Ai2),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.SoldierCorrection,
            SoldierTypeData = new CharacterSoldierTypeData
            {
              RushProbability = 300,
              RushAttack = 50,
            },
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Ai4,
        RequestedPoint = 10,
        Name = "AI Lv.4",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Ai3),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.SoldierCorrection,
            SoldierTypeData = new CharacterSoldierTypeData
            {
              ContinuousProbability = 30,
            },
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Ai5,
        RequestedPoint = 10,
        Name = "AI Lv.5",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Ai4),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.StrongOrIntellectExRegularly,
            Value = 4,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Terrorist1,
        RequestedPoint = 0,
        Name = "胡人 Lv.1",
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
        Type = CharacterSkillType.Terrorist2,
        RequestedPoint = 6,
        Name = "胡人 Lv.2",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Terrorist1),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.SoldierCorrection,
            SoldierTypeData = new CharacterSoldierTypeData
            {
              BaseAttack = 30,
            },
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Terrorist3,
        RequestedPoint = 9,
        Name = "胡人 Lv.3",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Terrorist2),
        Effects = new List<CharacterSkillEffect>
        {
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Terrorist4,
        RequestedPoint = 12,
        Name = "胡人 Lv.4",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Terrorist3),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.Command,
            Value = (int)CharacterCommandType.PeopleDecrease,
          },
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.Command,
            Value = (int)CharacterCommandType.PeopleIncrease,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Terrorist5,
        RequestedPoint = 8,
        Name = "胡人 Lv.5",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Terrorist4),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.GenerateItem,
            Value = (int)CharacterItemType.Elephant,
          },
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.GenerateItem,
            Value = (int)CharacterItemType.Toko,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.People1,
        RequestedPoint = 0,
        Name = "農民 Lv.1",
        SubjectAppear = skills => false,
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.SoldierType,
            Value = (int)SoldierType.Military,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.People2,
        RequestedPoint = 8,
        Name = "農民 Lv.2",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.People1),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.GenerateItem,
            Value = (int)CharacterItemType.SuperSoldier,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.People3,
        RequestedPoint = 8,
        Name = "農民 Lv.3",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.People2),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.SecurityCommandMulPercentage,
            Value = 80,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.People4,
        RequestedPoint = 7,
        Name = "農民 Lv.4",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.People3),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.Command,
            Value = (int)CharacterCommandType.PeopleDecrease,
          },
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.Command,
            Value = (int)CharacterCommandType.PeopleIncrease,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.People5,
        RequestedPoint = 12,
        Name = "農民 Lv.5",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.People4),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.GenerateItem,
            Value = (int)CharacterItemType.EliteSoldier,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Tactician1,
        RequestedPoint = 0,
        Name = "兵家 Lv.1",
        SubjectAppear = skills => false,
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.FormationExRegularly,
            Value = 4,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Tactician2,
        RequestedPoint = 10,
        Name = "兵家 Lv.2",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Tactician1),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.Command,
            Value = (int)CharacterCommandType.SoldierTrainingAll,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Tactician3,
        RequestedPoint = 8,
        Name = "兵家 Lv.3",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Tactician2),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.SoldierCorrection,
            SoldierTypeData = new CharacterSoldierTypeData
            {
              BaseAttack = 40,
              RushProbability = 1200,
              RushAttack = 60,
            },
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Tactician4,
        RequestedPoint = 7,
        Name = "兵家 Lv.4",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Tactician3),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.GenerateItem,
            Value = (int)CharacterItemType.MartialArtsBook,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Tactician5,
        RequestedPoint = 10,
        Name = "兵家 Lv.5",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Tactician4),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.SoldierCorrection,
            SoldierTypeData = new CharacterSoldierTypeData
            {
              ContinuousProbability = 60,
            },
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Scholar1,
        RequestedPoint = 0,
        Name = "学者 Lv.1",
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
        Type = CharacterSkillType.Scholar2,
        RequestedPoint = 6,
        Name = "学者 Lv.2",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Scholar1),
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
        Type = CharacterSkillType.Scholar3,
        RequestedPoint = 12,
        Name = "学者 Lv.3",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Scholar2),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.GenerateItem,
            Value = (int)CharacterItemType.AnnotationBook,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Scholar4,
        RequestedPoint = 10,
        Name = "学者 Lv.4",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Scholar3),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.GenerateItem,
            Value = (int)CharacterItemType.PrivateBook,
          },
        },
      },
      new CharacterSkillInfo
      {
        Type = CharacterSkillType.Scholar5,
        RequestedPoint = 7,
        Name = "学者 Lv.5",
        SubjectAppear = skills => skills.Any(s => s.Type == CharacterSkillType.Scholar4),
        Effects = new List<CharacterSkillEffect>
        {
          new CharacterSkillEffect
          {
            Type = CharacterSkillEffectType.Command,
            Value = (int)CharacterCommandType.Spy,
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

    public static IReadOnlyList<CharacterSkillInfo> GetNextSkills(this IEnumerable<CharacterSkill> skills)
    {
      return infos
        .Where(i => !skills.Any(s => s.Type == i.Type))
        .Where(i => i.SubjectAppear == null || i.SubjectAppear(skills))
        .ToArray();
    }
  }
}
