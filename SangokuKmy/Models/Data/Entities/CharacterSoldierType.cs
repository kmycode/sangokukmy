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
  [Table("character_soldier_types")]
  public class CharacterSoldierType
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("character_id")]
    [JsonIgnore]
    public uint CharacterId { get; set; }

    [Column("status")]
    [JsonIgnore]
    public CharacterSoldierStatus Status { get; set; }

    [NotMapped]
    [JsonProperty("status")]
    public short ApiStatus
    {
      get => (short)this.Status;
      set => this.Status = (CharacterSoldierStatus)value;
    }

    [Column("name", TypeName = "varchar(32)")]
    [JsonProperty("name")]
    public string Name { get; set; }

    [Column("rice_per_turn")]
    [JsonProperty("ricePerTurn")]
    public int RicePerTurn { get; set; }

    [Column("research_cost")]
    [JsonProperty("researchCost")]
    public short ResearchCost { get; set; }

    [Column("common_soldier")]
    [JsonProperty("commonSoldier")]
    public short Common { get; set; }

    [Column("light_infantory")]
    [JsonProperty("lightInfantory")]
    public short LightInfantry { get; set; }

    [Column("archer")]
    [JsonProperty("archer")]
    public short Archer { get; set; }

    [Column("light_cavalry")]
    [JsonProperty("lightCavalry")]
    public short LightCavalry { get; set; }

    [Column("strong_crossbow")]
    [JsonProperty("strongCrossbow")]
    public short StrongCrossbow { get; set; }

    [Column("light_intellect")]
    [JsonProperty("lightIntellect")]
    public short LightIntellect { get; set; }

    [Column("heavy_infantory")]
    [JsonProperty("heavyInfantory")]
    public short HeavyInfantry { get; set; }

    [Column("heavy_cavalry")]
    [JsonProperty("heavyCavalry")]
    public short HeavyCavalry { get; set; }

    [Column("intellect")]
    [JsonProperty("intellect")]
    public short Intellect { get; set; }

    [Column("repeating_crossbow")]
    [JsonProperty("repeatingCrossbow")]
    public short RepeatingCrossbow { get; set; }

    [Column("strong_guards")]
    [JsonProperty("strongGuards")]
    public short StrongGuards { get; set; }

    [Column("seiran")]
    [JsonProperty("seiran")]
    public short Seiran { get; set; }

    [Column("guard_1")]
    [JsonProperty("guard_1")]
    public short Guard_Step1 { get; set; }

    [Column("guard_2")]
    [JsonProperty("guard_2")]
    public short Guard_Step2 { get; set; }

    [Column("guard_3")]
    [JsonProperty("guard_3")]
    public short Guard_Step3 { get; set; }

    [Column("guard_4")]
    [JsonProperty("guard_4")]
    public short Guard_Step4 { get; set; }

    public bool IsVerify
    {
      get
      {
        return this.Common >= 0 &&
          this.LightInfantry >= 0 &&
          this.Archer >= 0 &&
          this.LightCavalry >= 0 &&
          this.StrongCrossbow >= 0 &&
          this.LightIntellect >= 0 &&
          this.HeavyInfantry >= 0 &&
          this.HeavyCavalry >= 0 &&
          this.Intellect >= 0 &&
          this.RepeatingCrossbow >= 0 &&
          this.StrongGuards >= 0 &&
          this.Seiran >= 0 &&
          this.Guard_Step1 == 0 &&
          this.Guard_Step2 == 0 &&
          this.Guard_Step3 == 0 &&
          this.Guard_Step4 == 0 &&
          this.Size <= 12;
      }
    }

    public int Size
    {
      get
      {
        return this.Common + this.LightInfantry + this.Archer +
              this.LightCavalry + this.StrongCrossbow + this.LightIntellect +
              this.HeavyInfantry + this.HeavyCavalry + this.Intellect +
              this.RepeatingCrossbow + this.StrongGuards + this.Seiran;
      }
    }
  }

  public enum CharacterSoldierStatus : short
  {
    InDraft = 0,
    Researching = 1,
    Available = 2,
    Removed = 3,
  }

  public class CharacterSoldierTypePart
  {
    public SoldierType Preset { get; set; }

    public string Name { get; set; }

    public CharacterSoldierTypeData Data { get; set; }

    public int Money { get; set; }

    public int Technology { get; set; }

    public bool CanConscript { get; set; } = true;
  }

  public static class DefaultCharacterSoldierTypeParts
  {
    private static readonly List<CharacterSoldierTypePart> parts = new List<CharacterSoldierTypePart>
    {
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Common,
        Name = "剣兵",
        Data = new CharacterSoldierTypeData
        {
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 1,
        CanConscript = true,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Guard,
        Name = "禁兵",
        Data = new CharacterSoldierTypeData
        {
          TypeGuard = 10,
          BaseAttack = 1,
          BaseDefend = 1,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 1,
        CanConscript = true,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.LightInfantry,
        Name = "軽戟兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 1,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 2,
        Technology = 100,
        CanConscript = true,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Archer,
        Name = "弓兵",
        Data = new CharacterSoldierTypeData
        {
          BaseDefend = 1,
          TypeCrossbow = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 3,
        Technology = 200,
        CanConscript = true,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.LightCavalry,
        Name = "軽騎兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 3,
          BaseDefend = 1,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
          ContinuousProbability = 18,
        },
        Money = 5,
        Technology = 300,
        CanConscript = true,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.StrongCrossbow,
        Name = "強弩兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 1,
          BaseDefend = 3,
          TypeCrossbow = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 7,
        Technology = 400,
        CanConscript = true,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.LightIntellect,
        Name = "神鬼兵",
        Data = new CharacterSoldierTypeData
        {
          StrongAttack = 100,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        Money = 10,
        Technology = 500,
        CanConscript = true,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.HeavyInfantry,
        Name = "重戟兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 5,
          BaseDefend = 3,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
          ContinuousProbability = 40,
        },
        Money = 12,
        Technology = 600,
        CanConscript = true,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.HeavyCavalry,
        Name = "重騎兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 6,
          BaseDefend = 4,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
          ContinuousProbability = 60,
        },
        Money = 15,
        Technology = 700,
        CanConscript = true,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Intellect,
        Name = "智攻兵",
        Data = new CharacterSoldierTypeData
        {
          IntellectAttack = 80,
          IntellectDefend = 40,
          TypeInfantry = 10,
          IntellectEx = 1,
          PowerStrong = 1,
        },
        Money = 17,
        Technology = 800,
        CanConscript = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.RepeatingCrossbow,
        Name = "連弩兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 9,
          BaseDefend = 4,
          TypeCrossbow = 10,
          StrongEx = 1,
          PowerStrong = 1,
          ContinuousProbability = 40,
        },
        Money = 20,
        Technology = 800,
        CanConscript = true,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.StrongGuards,
        Name = "壁守兵",
        Data = new CharacterSoldierTypeData
        {
          IntellectDefend = 100,
          IntellectEx = 1,
          TypeGuard = 10,
          PowerStrong = 1,
        },
        Money = 22,
        Technology = 999,
        CanConscript = true,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Seiran,
        Name = "井闌",
        Data = new CharacterSoldierTypeData
        {
          WallAttack = 20,
          WallDefend = 10,
          StrongEx = 1,
          TypeAntiWall = 10,
          PowerStrong = 1,
        },
        Money = 30,
        Technology = 500,
        CanConscript = true,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.WallCommon,
        Name = "城壁雑兵",
        Data = new CharacterSoldierTypeData
        {
          StrongEx = 1,
          TypeWall = 10,
          PowerStrong = 1,
        },
        Money = 1,
        CanConscript = true,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Guard_Step1,
        Name = "守兵A",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 2,
          BaseDefend = 3,
          TypeWall = 10,
          PowerStrong = 1,
        },
        Technology = 32767,
        CanConscript = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Guard_Step2,
        Name = "守兵B",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 4,
          BaseDefend = 4,
          TypeWall = 10,
          PowerStrong = 1,
        },
        Technology = 32767,
        CanConscript = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Guard_Step3,
        Name = "守兵C",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 6,
          BaseDefend = 5,
          TypeWall = 10,
          PowerStrong = 1,
        },
        Technology = 32767,
        CanConscript = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Guard_Step4,
        Name = "守兵D",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 9,
          BaseDefend = 6,
          TypeWall = 10,
          PowerStrong = 1,
        },
        Technology = 32767,
        CanConscript = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.TerroristCommonA,
        Name = "異民族兵A",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 3,
          BaseDefend = 2,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 5,
        Technology = 32767,
        CanConscript = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.TerroristCommonB,
        Name = "異民族兵B",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 4,
          BaseDefend = 3,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
          ContinuousProbability = 40,
        },
        Money = 10,
        Technology = 32767,
        CanConscript = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.TerroristCommonC,
        Name = "異民族兵C",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 6,
          BaseDefend = 4,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
          ContinuousProbability = 70,
        },
        Money = 15,
        Technology = 32767,
        CanConscript = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.ThiefCommonA,
        Name = "賊兵A",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 1,
          BaseDefend = 0,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 2,
        Technology = 32767,
        CanConscript = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.ThiefCommonB,
        Name = "賊兵B",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 2,
          BaseDefend = 0,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 4,
        Technology = 32767,
        CanConscript = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.ThiefCommonC,
        Name = "賊兵C",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 3,
          BaseDefend = 1,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 6,
        Technology = 32767,
        CanConscript = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.IntellectCommon,
        Name = "文官雑兵",
        Data = new CharacterSoldierTypeData
        {
          TypeInfantry = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        Money = 2,
        Technology = 200,
        CanConscript = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.IntellectHeavyCavalry,
        Name = "文官重騎兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 6,
          BaseDefend = 4,
          TypeCavalry = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
          ContinuousProbability = 20,
        },
        Money = 18,
        Technology = 900,
        CanConscript = true,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Military,
        Name = "義勇兵",
        Data = new CharacterSoldierTypeData
        {
          TypeInfantry = 10,
          PopularityEx = 1,
          PowerPopularity = 1,
        },
        Money = 10,
        Technology = 200,
        CanConscript = true,
      },
    };

    public static Optional<CharacterSoldierTypePart> Get(SoldierType type)
    {
      return parts.FirstOrDefault(t => t.Preset == type).ToOptional();
    }

    public static CharacterSoldierTypeData GetDataByDefault(SoldierType type)
    {
      var part = parts.FirstOrDefault(t => t.Preset == type);
      if (part != null)
      {
        return Enumerable.Repeat(part, 10).ToData();
      }
      else
      {
        return GetDataByDefault(SoldierType.Common);
      }
    }
  }

  public static class CharacterSoldierTypeExtensions
  {
    public static IEnumerable<CharacterSoldierTypePart> ToParts(this CharacterSoldierType type)
    {
      var parts =
        Enumerable.Repeat(DefaultCharacterSoldierTypeParts.Get(SoldierType.Common), type.Common)
        .Concat(Enumerable.Repeat(DefaultCharacterSoldierTypeParts.Get(SoldierType.LightInfantry), type.LightInfantry))
        .Concat(Enumerable.Repeat(DefaultCharacterSoldierTypeParts.Get(SoldierType.Archer), type.Archer))
        .Concat(Enumerable.Repeat(DefaultCharacterSoldierTypeParts.Get(SoldierType.LightCavalry), type.LightCavalry))
        .Concat(Enumerable.Repeat(DefaultCharacterSoldierTypeParts.Get(SoldierType.StrongCrossbow), type.StrongCrossbow))
        .Concat(Enumerable.Repeat(DefaultCharacterSoldierTypeParts.Get(SoldierType.LightIntellect), type.LightIntellect))
        .Concat(Enumerable.Repeat(DefaultCharacterSoldierTypeParts.Get(SoldierType.HeavyInfantry), type.HeavyInfantry))
        .Concat(Enumerable.Repeat(DefaultCharacterSoldierTypeParts.Get(SoldierType.HeavyCavalry), type.HeavyCavalry))
        .Concat(Enumerable.Repeat(DefaultCharacterSoldierTypeParts.Get(SoldierType.RepeatingCrossbow), type.RepeatingCrossbow))
        .Concat(Enumerable.Repeat(DefaultCharacterSoldierTypeParts.Get(SoldierType.Intellect), type.Intellect))
        .Concat(Enumerable.Repeat(DefaultCharacterSoldierTypeParts.Get(SoldierType.StrongGuards), type.StrongGuards))
        .Concat(Enumerable.Repeat(DefaultCharacterSoldierTypeParts.Get(SoldierType.Seiran), type.Seiran));
      return parts.Where(p => p.HasData).Select(p => p.Data);
    }
  }
}
