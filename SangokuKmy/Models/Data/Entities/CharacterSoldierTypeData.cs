﻿using Newtonsoft.Json;
using SangokuKmy.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Entities
{
  public class CharacterSoldierTypeData
  {
    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("money")]
    public short Money { get; set; }

    [JsonProperty("technology")]
    public short Technology { get; set; }

    [JsonProperty("baseAttack")]
    public short BaseAttack { get; set; }
    
    [JsonProperty("baseDefend")]
    public short BaseDefend { get; set; }
    
    [JsonProperty("strongAttack")]
    public short StrongAttack { get; set; }
    
    [JsonProperty("strongDefend")]
    public short StrongDefend { get; set; }
    
    [JsonProperty("intellectAttack")]
    public short IntellectAttack { get; set; }
    
    [JsonProperty("intellectDefend")]
    public short IntellectDefend { get; set; }
    
    [JsonProperty("leadershipAttack")]
    public short LeadershipAttack { get; set; }
    
    [JsonProperty("leadershipDefend")]
    public short LeadershipDefend { get; set; }
    
    [JsonProperty("popularityAttack")]
    public short PopularityAttack { get; set; }
    
    [JsonProperty("popularityDefend")]
    public short PopularityDefend { get; set; }
    
    [JsonProperty("rushProbability")]
    public short RushProbability { get; set; }
    
    [JsonProperty("rushAttack")]
    public short RushAttack { get; set; }
    
    [JsonProperty("rushDefend")]
    public short RushDefend { get; set; }
    
    [JsonProperty("rushAgainstAttack")]
    public short RushAgainstAttack { get; set; }
    
    [JsonProperty("rushAgainstDefend")]
    public short RushAgainstDefend { get; set; }
    
    [JsonProperty("continuousProbability")]
    public short ContinuousProbability { get; set; }
    
    [JsonProperty("continuousAttack")]
    public short ContinuousAttack { get; set; }
    
    [JsonProperty("continuousDefend")]
    public short ContinuousDefend { get; set; }
    
    [JsonProperty("wallAttack")]
    public short WallAttack { get; set; }
    
    [JsonProperty("wallDefend")]
    public short WallDefend { get; set; }
    
    [JsonProperty("throughDefendersProbability")]
    public short ThroughDefendersToWallProbability { get; set; }
    
    [JsonProperty("recovery")]
    public short Recovery { get; set; }

    [JsonIgnore]
    public float ResearchMoneyBase
    {
      get
      {
        return this.Money * 160;
      }
    }

    [JsonIgnore]
    public float ResearchCostBase
    {
      get
      {
        return this.Money * (this.Technology / 230.0f);
      }
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

  public enum BattlerEnemyType
  {
    Character,
    WallGuard,
    Wall,
    StrongGuards,
  }

  public static class CharacterSoldierTypeExtentions
  {
    public static CharacterSoldierTypeData ToData(this IEnumerable<CharacterSoldierTypePart> parts)
    {
      var d = new CharacterSoldierTypeData
      {
        Description = string.Join(',', parts.GroupBy(p => p.Name).Select(p => $"{p.Key}{p.Count()}")),
        Money = (short)parts.Sum(p => p.Money),
        Technology = (short)parts.Max(p => p.Technology),
        BaseAttack = (short)parts.Sum(p => p.Data.BaseAttack),
        BaseDefend = (short)parts.Sum(p => p.Data.BaseDefend),
        IntellectAttack = (short)parts.Sum(p => p.Data.IntellectAttack),
        IntellectDefend = (short)parts.Sum(p => p.Data.IntellectDefend),
        WallAttack = (short)parts.Sum(p => p.Data.WallAttack),
      };
      return d;
    }
  }
}