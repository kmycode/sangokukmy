using Newtonsoft.Json;
using SangokuKmy.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Services;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SangokuKmy.Models.Data.Entities
{
  public class CharacterSoldierTypeData
  {
    [JsonProperty("description")]
    public string Description { get; set; }

    [JsonProperty("money")]
    public short Money { get; set; }

    [JsonProperty("fakeMoney")]
    public short FakeMoney { get; set; }

    [JsonProperty("technology")]
    public short Technology { get; set; }

    [JsonProperty("powerStrong")]
    public short PowerStrong { get; set; }

    [JsonProperty("powerIntellect")]
    public short PowerIntellect { get; set; }

    [JsonProperty("powerPopularity")]
    public short PowerPopularity { get; set; }

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

    [JsonProperty("typeInfantryAttack")]
    public short TypeInfantryAttack { get; set; }

    [JsonProperty("typeCavalry")]
    public short TypeCavalry { get; set; }

    [JsonProperty("typeCavalryAttack")]
    public short TypeCavalryAttack { get; set; }

    [JsonProperty("typeCrossbow")]
    public short TypeCrossbow { get; set; }

    [JsonProperty("typeCrossbowAttack")]
    public short TypeCrossbowAttack { get; set; }

    [JsonProperty("typeWall")]
    public short TypeWall { get; set; }

    [JsonProperty("typeAntiWall")]
    public short TypeAntiWall { get; set; }

    [JsonProperty("typeGuard")]
    public short TypeGuard { get; set; }

    [JsonProperty("typeGuardAttack")]
    public short TypeGuardAttack { get; set; }

    [JsonProperty("typeGuardDefend")]
    public short TypeGuardDefend { get; set; }

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

    [JsonProperty("continuousProbabilityOnSingleTurn")]
    public short ContinuousProbabilityOnSingleTurn { get; set; }

    [JsonProperty("continuousAttack")]
    public short ContinuousAttack { get; set; }
    
    [JsonProperty("continuousDefend")]
    public short ContinuousDefend { get; set; }

    [JsonProperty("infantryAttack")]
    public short InfantryAttack { get; set; }

    [JsonProperty("cavalryAttack")]
    public short CavalryAttack { get; set; }

    [JsonProperty("crossbowAttack")]
    public short CrossbowAttack { get; set; }

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
    public int ResearchMoney
    {
      get
      {
        return this.Money * 160;
      }
    }

    [JsonIgnore]
    public int ResearchCost
    {
      get
      {
        return (int)(this.Money * (this.Technology / 230.0f));
      }
    }

    public int CalcPower(Character chara)
    {
      var p = 0;
      p += (int)(chara.Strong * (this.PowerStrong / 10.0f));
      p += (int)(chara.Intellect * (this.PowerIntellect / 10.0f));
      p += (int)(chara.Popularity * (this.PowerPopularity / 10.0f));
      return p;
    }

    public (int AttackCorrection, int DefendCorrection) CalcCorrections(Character chara, IEnumerable<CharacterSkill> skills, CharacterSoldierTypeData enemyType)
    {
      var a = (float)this.BaseAttack;
      var d = (float)this.BaseDefend;

      a += this.StrongAttack / 1000.0f * chara.Strong;
      d += this.StrongDefend / 1000.0f * chara.Strong;

      a += this.IntellectAttack / 1000.0f * chara.Intellect;
      d += this.IntellectDefend / 1000.0f * chara.Intellect;

      a += this.InfantryAttack * (enemyType.TypeInfantry / 100.0f);
      a += this.CavalryAttack * (enemyType.TypeCavalry / 100.0f);
      a += this.CrossbowAttack * (enemyType.TypeCrossbow / 100.0f);

      a += this.WallAttack * (enemyType.TypeWall / 100.0f);
      d += this.WallDefend * (enemyType.TypeWall / 100.0f);

      a += this.TypeInfantryAttack * (this.TypeInfantry / 100.0f);
      a += this.TypeCavalryAttack * (this.TypeCavalry / 100.0f);
      a += this.TypeCrossbowAttack * (this.TypeCrossbow / 100.0f);

      a += this.TypeGuardAttack * (this.TypeGuard / 100.0f);
      d += this.TypeGuardDefend * (this.TypeGuard / 100.0f);

      return ((int)a, (int)d);
    }

    public (int AttackCorrection, int DefendCorrection) CalcPostCorrections(CountryPostType post)
    {
      var a = 0.0f;
      var d = 0.0f;

      if (post == CountryPostType.Monarch)
      {
        a += 20;
      }
      else if (post == CountryPostType.GrandGeneral)
      {
        a += 10;
      }
      else if (post == CountryPostType.General)
      {
        a += this.TypeInfantry / 100.0f * 10;
      }
      else if (post == CountryPostType.CavalryGeneral)
      {
        a += this.TypeCavalry / 100.0f * 10;
      }
      else if (post == CountryPostType.BowmanGeneral)
      {
        a += this.TypeCrossbow / 100.0f * 10;
      }
      else if (post == CountryPostType.GuardGeneral)
      {
        a += (this.TypeGuard + this.TypeWall) / 100.0f * 10;
      }

      return ((int)a, (int)d);
    }

    public bool CanContinuous()
    {
      return this.CanContinuous(this.ContinuousProbability);
    }

    public bool CanContinuousOnSingleTurn()
    {
      return this.CanContinuous(this.ContinuousProbabilityOnSingleTurn);
    }

    private bool CanContinuous(short probability)
    {
      if (probability == 0)
      {
        return false;
      }
      else if (probability < 10000 - 1)
      {
        return RandomService.Next(0, 10000) < probability;
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
      return this.RushAttack / 10.0f;
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
        FakeMoney = (short)parts.Sum(p => p.FakeMoney),
        Technology = (short)parts.Max(p => p.Technology),
      };
      foreach (var p in parts)
      {
        d.Append(p.Data);
      }
      return d;
    }

    public static CharacterSoldierTypeData Append(this CharacterSoldierTypeData self, CharacterSoldierTypeData d)
    {
      self.PowerStrong += d.PowerStrong;
      self.PowerIntellect += d.PowerIntellect;
      self.PowerPopularity += d.PowerPopularity;
      self.BaseAttack += d.BaseAttack;
      self.BaseDefend += d.BaseDefend;
      self.StrongAttack += d.StrongAttack;
      self.StrongDefend += d.StrongDefend;
      self.IntellectAttack += d.IntellectAttack;
      self.IntellectDefend += d.IntellectDefend;
      self.WallAttack += d.WallAttack;
      self.WallDefend += d.WallDefend;
      self.ContinuousProbability += d.ContinuousProbability;
      self.ContinuousProbabilityOnSingleTurn += d.ContinuousProbabilityOnSingleTurn;
      self.RushProbability += d.RushProbability;
      self.RushAttack += d.RushAttack;
      self.StrongEx += d.StrongEx;
      self.IntellectEx += d.IntellectEx;
      self.LeadershipEx += d.LeadershipEx;
      self.PopularityEx += d.PopularityEx;
      self.TypeCavalry += d.TypeCavalry;
      self.TypeCavalryAttack += d.TypeCavalryAttack;
      self.CavalryAttack += d.CavalryAttack;
      self.TypeCrossbow += d.TypeCrossbow;
      self.TypeCrossbowAttack += d.TypeCrossbowAttack;
      self.CrossbowAttack += d.CrossbowAttack;
      self.TypeInfantry += d.TypeInfantry;
      self.TypeInfantryAttack += d.TypeInfantryAttack;
      self.InfantryAttack += d.InfantryAttack;
      self.TypeWall += d.TypeWall;
      self.TypeAntiWall += d.TypeAntiWall;
      self.TypeGuard += d.TypeGuard;
      self.TypeGuardAttack += d.TypeGuardAttack;
      self.TypeGuardDefend += d.TypeGuardDefend;
      return self;
    }
  }
}
