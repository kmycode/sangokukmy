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
    public short Money { get; set; }

    public short FakeMoney { get; set; }

    public short Technology { get; set; }

    public short PowerStrong { get; set; }

    public short PowerIntellect { get; set; }

    public short PowerPopularity { get; set; }

    public short BaseAttack { get; set; }
    
    public short BaseDefend { get; set; }
    
    public short StrongAttack { get; set; }
    
    public short StrongDefend { get; set; }
    
    public short IntellectAttack { get; set; }
    
    public short IntellectDefend { get; set; }
    
    public short PopularityAttack { get; set; }
    
    public short PopularityDefend { get; set; }

    public short TypeInfantry { get; set; }

    public short TypeCavalry { get; set; }

    public short TypeCrossbow { get; set; }

    public short TypeWall { get; set; }

    public short TypeWeapon { get; set; }

    public short TypeGuard { get; set; }

    public short TypeInfantryAttack { get; set; }

    public short TypeInfantryDefend { get; set; }

    public short TypeGuardAttack { get; set; }

    public short TypeGuardDefend { get; set; }

    public short GogyoFire { get; set; }

    public short GogyoTree { get; set; }

    public short GogyoSoil { get; set; }

    public short GogyoMetal { get; set; }

    public short GogyoWater { get; set; }

    public short GogyoAttack { get; set; }

    public short GogyoDefend { get; set; }

    public short RushProbability { get; set; }
    
    public ushort RushAttack { get; set; }
    
    public ushort RushDefend { get; set; }

    public short DisorderProbability { get; set; }

    public short TypeWallDisorderProbability { get; set; }

    public short FriendlyFireProbability { get; set; }
    
    public short ContinuousProbability { get; set; }

    public short ContinuousProbabilityOnSingleTurn { get; set; }

    public short WallAttack { get; set; }
    
    public short WallDefend { get; set; }

    public short InfantryAttack { get; set; }

    public short InfantryDefend { get; set; }

    public short CavalryAttack { get; set; }

    public short CavalryDefend { get; set; }

    public short CrossbowAttack { get; set; }

    public short CrossbowDefend { get; set; }

    public short WeaponAttack { get; set; }

    public short WeaponDefend { get; set; }

    public short StrongEx { get; set; }

    public short IntellectEx { get; set; }

    public short LeadershipEx { get; set; }

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
      // 相手が城壁のとき、特殊な戦闘補正がつく
      var typeWall = enemyType.TypeWall / 100.0f;

      var a = (float)this.BaseAttack * (1 - typeWall / 2);
      var d = (float)this.BaseDefend * (1 - typeWall / 2);

      a += this.StrongAttack / 1000.0f * chara.Strong;
      d += this.StrongDefend / 1000.0f * chara.Strong;

      a += this.IntellectAttack / 1000.0f * chara.Intellect;
      d += this.IntellectDefend / 1000.0f * chara.Intellect;

      a += this.WallAttack * typeWall;
      d += this.WallDefend * typeWall;
      a += this.InfantryAttack * enemyType.TypeInfantry / 100.0f;
      d += this.InfantryDefend * enemyType.TypeInfantry / 100.0f;
      a += this.CavalryAttack * enemyType.TypeCavalry / 100.0f;
      d += this.CavalryDefend * enemyType.TypeCavalry / 100.0f;
      a += this.CrossbowAttack * enemyType.TypeCrossbow / 100.0f;
      d += this.CrossbowDefend * enemyType.TypeCrossbow / 100.0f;
      a += this.WeaponAttack * enemyType.TypeWeapon / 100.0f;
      d += this.WeaponDefend * enemyType.TypeWeapon / 100.0f;

      a += this.TypeInfantryAttack * (this.TypeInfantry / 100.0f) * (1 - typeWall);
      d += this.TypeInfantryDefend * (this.TypeInfantry / 100.0f) * (1 - typeWall);
      a += this.TypeGuardAttack * (this.TypeGuard / 100.0f) * (1 - typeWall);
      d += this.TypeGuardDefend * (this.TypeGuard / 100.0f) * (1 - typeWall);

      var g = this.CalcGogyoPower(enemyType);
      a += this.GogyoAttack * g * (1 - typeWall);
      d += this.GogyoDefend * g * (1 - typeWall);

      return ((int)a, (int)d);
    }

    private float CalcGogyoPower(CharacterSoldierTypeData target)
    {
      float GetSize(short stronger, short weaker)
      {
        // 0 <= size <= 1
        if (stronger == 0 || weaker == 0)
        {
          return 0;
        }
        var size = (stronger + weaker) / (float)stronger / 2 * (stronger + weaker) / 200.0f;
        return size;
      }

      // 基準値: 1
      return GetSize(this.GogyoTree, target.GogyoWater) + GetSize(this.GogyoTree, target.GogyoSoil) +
        GetSize(this.GogyoFire, target.GogyoTree) + GetSize(this.GogyoFire, target.GogyoMetal) +
        GetSize(this.GogyoSoil, target.GogyoFire) + GetSize(this.GogyoSoil, target.GogyoWater) +
        GetSize(this.GogyoMetal, target.GogyoSoil) + GetSize(this.GogyoMetal, target.GogyoTree) +
        GetSize(this.GogyoWater, target.GogyoMetal) + GetSize(this.GogyoWater, target.GogyoFire);
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
      return this.CalcProbability(this.RushProbability);
    }

    public bool IsDisorder()
    {
      return this.CalcProbability((short)(this.DisorderProbability + this.TypeWall / 100.0f * this.TypeWallDisorderProbability));
    }

    public bool IsFriendlyFire()
    {
      return this.CalcProbability(this.FriendlyFireProbability);
    }

    private bool CalcProbability(short probability)
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

    public float CalcRushAttack(CharacterSoldierTypeData enemyType)
    {
      return Math.Max(this.RushAttack - enemyType.RushDefend, 0) / 8.0f;
    }
  }

  public static class CharacterSoldierTypeExtentions
  {
    public static CharacterSoldierTypeData ToData(this IEnumerable<CharacterSoldierTypePart> parts)
    {
      var d = new CharacterSoldierTypeData
      {
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

    public static CharacterSoldierTypeData Append(this CharacterSoldierTypeData self, IEnumerable<CharacterSoldierTypeData> d)
    {
      foreach (var dd in d)
      {
        self.Append(dd);
      }
      return self;
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
      self.InfantryAttack += d.InfantryAttack;
      self.InfantryDefend += d.InfantryDefend;
      self.CavalryAttack += d.CavalryAttack;
      self.CavalryDefend += d.CavalryDefend;
      self.CrossbowAttack += d.CrossbowAttack;
      self.CrossbowDefend += d.CrossbowDefend;
      self.WeaponAttack += d.WeaponAttack;
      self.WeaponDefend += d.WeaponDefend;
      self.ContinuousProbability += d.ContinuousProbability;
      self.ContinuousProbabilityOnSingleTurn += d.ContinuousProbabilityOnSingleTurn;
      self.RushProbability += d.RushProbability;
      self.RushAttack += d.RushAttack;
      self.RushDefend += d.RushDefend;
      self.DisorderProbability += d.DisorderProbability;
      self.TypeWallDisorderProbability += d.TypeWallDisorderProbability;
      self.FriendlyFireProbability += d.FriendlyFireProbability;
      self.StrongEx += d.StrongEx;
      self.IntellectEx += d.IntellectEx;
      self.LeadershipEx += d.LeadershipEx;
      self.PopularityEx += d.PopularityEx;
      self.TypeCavalry += d.TypeCavalry;
      self.TypeCrossbow += d.TypeCrossbow;
      self.TypeInfantry += d.TypeInfantry;
      self.TypeWall += d.TypeWall;
      self.TypeWeapon += d.TypeWeapon;
      self.TypeGuard += d.TypeGuard;
      self.TypeInfantryAttack += d.TypeInfantryAttack;
      self.TypeInfantryDefend += d.TypeInfantryDefend;
      self.TypeGuardAttack += d.TypeGuardAttack;
      self.TypeGuardDefend += d.TypeGuardDefend;
      self.GogyoFire += d.GogyoFire;
      self.GogyoMetal += d.GogyoMetal;
      self.GogyoSoil += d.GogyoSoil;
      self.GogyoTree += d.GogyoTree;
      self.GogyoWater += d.GogyoWater;
      self.GogyoAttack += d.GogyoAttack;
      self.GogyoDefend += d.GogyoDefend;
      return self;
    }
  }
}
