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

    [Column("preset")]
    [JsonIgnore]
    public SoldierType Preset { get; set; }

    [Column("is_conscript_disabled")]
    [JsonIgnore]
    public bool IsConscriptDisabled { get; set; }

    [Column("name", TypeName = "varchar(32)")]
    [JsonProperty("name")]
    public string Name { get; set; }

    [Column("money")]
    [JsonProperty("money")]
    public short Money { get; set; }

    [Column("rice_per_turn")]
    [JsonProperty("ricePerTurn")]
    public int RicePerTurn { get; set; }

    [Column("technology")]
    [JsonProperty("technology")]
    public short Technology { get; set; }

    [Column("research_cost")]
    [JsonProperty("researchCost")]
    public short ResearchCost { get; set; }

    [Column("base_attack")]
    [JsonProperty("baseAttack")]
    public short BaseAttack { get; set; }

    [Column("base_defend")]
    [JsonProperty("baseDefend")]
    public short BaseDefend { get; set; }

    [Column("strong_attack")]
    [JsonProperty("strongAttack")]
    public short StrongAttack { get; set; }

    [Column("strong_defend")]
    [JsonProperty("strongDefend")]
    public short StrongDefend { get; set; }

    [Column("intellect_attack")]
    [JsonProperty("intellectAttack")]
    public short IntellectAttack { get; set; }

    [Column("intellect_defend")]
    [JsonProperty("intellectDefend")]
    public short IntellectDefend { get; set; }

    [Column("leadership_attack")]
    [JsonProperty("leadershipAttack")]
    public short LeadershipAttack { get; set; }

    [Column("leadership_defend")]
    [JsonProperty("leadershipDefend")]
    public short LeadershipDefend { get; set; }

    [Column("popularity_attack")]
    [JsonProperty("popularityAttack")]
    public short PopularityAttack { get; set; }

    [Column("popularity_defend")]
    [JsonProperty("popularityDefend")]
    public short PopularityDefend { get; set; }

    [Column("rush_probability")]
    [JsonProperty("rushProbability")]
    public short RushProbability { get; set; }

    [Column("rush_attack")]
    [JsonProperty("rushAttack")]
    public short RushAttack { get; set; }

    [Column("rush_defend")]
    [JsonProperty("rushDefend")]
    public short RushDefend { get; set; }

    [Column("rush_against_attack")]
    [JsonProperty("rushAgainstAttack")]
    public short RushAgainstAttack { get; set; }

    [Column("rush_against_defend")]
    [JsonProperty("rushAgainstDefend")]
    public short RushAgainstDefend { get; set; }

    [Column("continuous_probability")]
    [JsonProperty("continuousProbability")]
    public short ContinuousProbability { get; set; }

    [Column("continuous_attack")]
    [JsonProperty("continuousAttack")]
    public short ContinuousAttack { get; set; }

    [Column("continuous_defend")]
    [JsonProperty("continuousDefend")]
    public short ContinuousDefend { get; set; }

    [Column("wall_attack")]
    [JsonProperty("wallAttack")]
    public short WallAttack { get; set; }

    [Column("wall_defend")]
    [JsonProperty("wallDefend")]
    public short WallDefend { get; set; }

    [Column("through_defenders_probability")]
    [JsonProperty("throughDefendersProbability")]
    public short ThroughDefendersToWallProbability { get; set; }

    [Column("recovery")]
    [JsonProperty("recovery")]
    public short Recovery { get; set; }

    public void UpdateCosts()
    {
      var m = 10.0f;
      var t = 0.0f;
      var r = 0.0f;

      m += MathF.Pow(1.01f, this.BaseAttack * 1.08f + this.BaseDefend * 1.28f) * 50;
      t += this.BaseAttack * 2.8f + this.BaseDefend * 3.8f;
      r += this.BaseAttack + this.BaseDefend;

      m += MathF.Pow(3.6f, this.IntellectAttack / 1000.0f * 1.07f + this.IntellectDefend / 1000.0f * 1.32f) * 65;
      t += this.IntellectAttack * 3.8f + this.IntellectDefend * 5.8f;
      r += this.IntellectAttack * 1.1f + this.IntellectDefend * 1.3f;

      this.Money = Math.Max((short)(m - this.RicePerTurn), (short)10);
      this.Technology = Math.Max((short)t, (short)100);
      this.ResearchCost = Math.Max((short)r, (short)200);
    }

    public (int AttackCorrection, int DefendCorrection) CalcCorrections(Character chara, BattlerEnemyType enemyType)
    {
      var a = (float)this.BaseAttack;
      var d = (float)this.BaseDefend;

      a += this.IntellectAttack / 1000.0f * chara.Intellect;
      d += this.IntellectDefend / 1000.0f * chara.Intellect;

      if (enemyType == BattlerEnemyType.Wall || enemyType == BattlerEnemyType.WallGuard || enemyType == BattlerEnemyType.StrongGuards)
      {
        a += this.WallAttack;
        d += this.WallDefend;
      }

      return ((int)a, (int)d);
    }
  }

  public enum CharacterSoldierStatus : short
  {
    InDraft = 0,
    Researching = 1,
    Available = 2,
    Removed = 3,
  }

  public enum BattlerEnemyType
  {
    Character,
    WallGuard,
    Wall,
    StrongGuards,
  }

  public static class DefaultCharacterSoldierTypes
  {
    private static readonly List<CharacterSoldierType> types = new List<CharacterSoldierType>
    {
      new CharacterSoldierType
      {
        Preset = SoldierType.Common,
        Name = "雑兵",
        Money = 10,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.Guard,
        Name = "禁兵",
        Money = 10,
        BaseAttack = 10,
        BaseDefend = 10,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.LightInfantry,
        Name = "軽歩兵",
        Money = 20,
        Technology = 100,
        BaseAttack = 10,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.Archer,
        Name = "弓兵",
        Money = 30,
        Technology = 200,
        BaseDefend = 15,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.LightCavalry,
        Name = "軽騎兵",
        Money = 50,
        Technology = 300,
        BaseAttack = 35,
        BaseDefend = 10,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.StrongCrossbow,
        Name = "強弩兵",
        Money = 70,
        Technology = 400,
        BaseAttack = 10,
        BaseDefend = 35,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.LightIntellect,
        Name = "神鬼兵",
        Money = 100,
        Technology = 500,
        IntellectAttack = 1000,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.HeavyInfantry,
        Name = "重歩兵",
        Money = 125,
        Technology = 600,
        BaseAttack = 50,
        BaseDefend = 30,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.HeavyCavalry,
        Name = "重騎兵",
        Money = 150,
        Technology = 700,
        BaseAttack = 60,
        BaseDefend = 40,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.Intellect,
        Name = "智攻兵",
        Money = 175,
        Technology = 800,
        IntellectAttack = 800,
        IntellectDefend = 400,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.RepeatingCrossbow,
        Name = "連弩兵",
        Money = 200,
        Technology = 900,
        BaseAttack = 90,
        BaseDefend = 30,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.StrongGuards,
        Name = "壁守兵",
        Money = 225,
        Technology = 999,
        IntellectDefend = 1000,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.Seiran,
        Name = "井闌",
        Money = 300,
        Technology = 500,
        WallAttack = 200,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.Guard_Step1,
        IsConscriptDisabled = true,
        BaseAttack = 20,
        BaseDefend = 20,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.Guard_Step2,
        IsConscriptDisabled = true,
        BaseAttack = 40,
        BaseDefend = 40,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.Guard_Step3,
        IsConscriptDisabled = true,
        BaseAttack = 60,
        BaseDefend = 60,
      },
      new CharacterSoldierType
      {
        Preset = SoldierType.Guard_Step4,
        IsConscriptDisabled = true,
        BaseAttack = 90,
        BaseDefend = 90,
      },
    };

    public static Optional<CharacterSoldierType> Get(SoldierType type)
    {
      return types.FirstOrDefault(t => t.Preset == type).ToOptional();
    }
  }
}
