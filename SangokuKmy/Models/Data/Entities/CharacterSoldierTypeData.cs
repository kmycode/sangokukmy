using Newtonsoft.Json;
using SangokuKmy.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Services;

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

    [JsonProperty("typeInfantry")]
    public short TypeInfantry { get; set; }

    [JsonProperty("typeCavalry")]
    public short TypeCavalry { get; set; }

    [JsonProperty("typeCrossbow")]
    public short TypeCrossbow { get; set; }

    [JsonProperty("typeWall")]
    public short TypeWall { get; set; }

    [JsonProperty("rushProbability")]
    public short RushProbability { get; set; }
    
    [JsonProperty("rushAttack")]
    public ushort RushAttack { get; set; }
    
    [JsonProperty("rushDefend")]
    public ushort RushDefend { get; set; }
    
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

    [JsonProperty("strongEx")]
    public short StrongEx { get; set; }

    [JsonProperty("intellectEx")]
    public short IntellectEx { get; set; }

    [JsonProperty("leadershipEx")]
    public short LeadershipEx { get; set; }

    [JsonProperty("popularityEx")]
    public short PopularityEx { get; set; }

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

    public (int AttackCorrection, int DefendCorrection) CalcCorrections(Character chara, CharacterSoldierTypeData enemyType)
    {
      var a = (float)this.BaseAttack;
      var d = (float)this.BaseDefend;

      a += this.IntellectAttack / 1000.0f * chara.Intellect;
      d += this.IntellectDefend / 1000.0f * chara.Intellect;

      a += this.WallAttack * (enemyType.TypeWall / 10.0f);
      d += this.WallDefend * (enemyType.TypeWall / 10.0f);

      return ((int)a, (int)d);
    }

    public bool CanContinuous()
    {
      if (this.ContinuousProbability == 0)
      {
        return false;
      }
      else if (this.ContinuousProbability < 10000 - 1)
      {
        return RandomService.Next(0, 10000) < this.ContinuousProbability;
      }
      else
      {
        return true;
      }
    }

    public bool IsRush()
    {
      if (this.RushProbability == 0)
      {
        return false;
      }
      else if (this.RushProbability < 10000 - 1)
      {
        return RandomService.Next(0, 10000) < this.RushProbability;
      }
      else
      {
        return true;
      }
    }

    public float CalcRushAttack()
    {
      return this.RushAttack / 10000.0f;
    }
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
        WallDefend = (short)parts.Sum(p => p.Data.WallDefend),
        ContinuousProbability = (short)parts.Sum(p => p.Data.ContinuousProbability),
        RushProbability = (short)parts.Sum(p => p.Data.RushProbability),
        RushAttack = (ushort)parts.Sum(p => p.Data.RushAttack),
        StrongEx = (short)parts.Sum(p => p.Data.StrongEx),
        IntellectEx = (short)parts.Sum(p => p.Data.IntellectEx),
        LeadershipEx = (short)parts.Sum(p => p.Data.LeadershipEx),
        PopularityEx = (short)parts.Sum(p => p.Data.PopularityEx),
        TypeWall = (short)parts.Sum(p => p.Data.TypeWall),
      };
      return d;
    }
  }
}
