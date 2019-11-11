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
  public class CharacterSoldierTypePart
  {
    public SoldierType Preset { get; set; }

    public string Name { get; set; }

    public CharacterSoldierTypeData Data { get; set; }

    public int Money { get; set; }

    public int FakeMoney
    {
      get
      {
        if (this._fakeMoney < 0)
        {
          return this.Money;
        }
        return this._fakeMoney;
      }
      set
      {
        this._fakeMoney = value;
      }
    }

    private int _fakeMoney = -1;

    public int Technology { get; set; }

    public short ResearchLevel { get; set; } = 1;

    public bool CanConscript { get; set; } = true;

    public bool CanConscriptWithoutResource { get; set; } = true;

    public bool CanConscriptWithoutSkill { get; set; } = true;
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
          BaseDefend = 0,
          RushDefend = 2,
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
          BaseAttack = 0,
          BaseDefend = 1,
          RushDefend = 2,
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
          RushDefend = 2,
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
          TypeGuard = 10,
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
        Money = 16,
        FakeMoney = 20,
        Technology = 800,
        CanConscript = true,
        CanConscriptWithoutResource = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Seishu,
        Name = "青洲兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 9,
          BaseDefend = 7,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
          RushProbability = 10,
          RushAttack = 6,
          ContinuousProbability = 40,
        },
        Money = 19,
        FakeMoney = 25,
        Technology = 900,
        CanConscript = true,
        CanConscriptWithoutResource = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Elephant,
        Name = "象兵",
        Data = new CharacterSoldierTypeData
        {
          TypeCavalry = 10,
          WallDefend = -4,
          RushProbability = 250,
          RushAttack = 12,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 12,
        FakeMoney = 20,
        Technology = 1000,
        CanConscript = true,
        CanConscriptWithoutResource = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Toko,
        Name = "藤甲兵",
        Data = new CharacterSoldierTypeData
        {
          TypeInfantry = 10,
          BaseDefend = 14,
          ContinuousProbability = 60,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 14,
        FakeMoney = 20,
        Technology = 1000,
        CanConscript = true,
        CanConscriptWithoutResource = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.StrongGuards,
        Name = "壁守兵",
        Data = new CharacterSoldierTypeData
        {
          IntellectDefend = 100,
          RushDefend = 3,
          IntellectEx = 1,
          TypeGuard = 10,
          PowerStrong = 1,
        },
        Money = 14,
        Technology = 1000,
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
          TypeWeapon = 10,
          PowerStrong = 1,
        },
        Money = 30,
        FakeMoney = 2,
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
        Name = "梓叡兵",
        Data = new CharacterSoldierTypeData
        {
          TypeInfantry = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        Money = 3,
        Technology = 200,
        CanConscript = true,
        CanConscriptWithoutSkill = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.IntellectHeavyCavalry,
        Name = "梓馬兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 6,
          BaseDefend = 4,
          RushDefend = 3,
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
        Preset = SoldierType.IntellectRepeatingCrossbow,
        Name = "梓琴兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 9,
          BaseDefend = 4,
          RushDefend = 3,
          TypeCrossbow = 10,
          DisorderProbability = 10,
          FriendlyFireProbability = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        Money = 22,
        Technology = 900,
        CanConscript = true,
        CanConscriptWithoutSkill = false,
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
}
