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

    public List<TownType> TownTypes { get; set; } = null;

    public bool CanConscript { get; set; } = true;

    public bool CanConscriptWithoutResource { get; set; } = true;

    public bool CanConscriptWithoutSkill { get; set; } = true;

    public bool CanConscriptWithoutSubBuilding { get; set; } = true;

    public TownSubBuildingType NeededSubBuildingType { get; set; }

    public SoldierKind Kind { get; set; } = SoldierKind.Battle;
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
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Guard,
        Name = "禁兵",
        Data = new CharacterSoldierTypeData
        {
          TypeInfantry = 10,
          BaseAttack = 1,
          BaseDefend = 1,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 1,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.LightInfantry,
        Name = "軽戟兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 3,
          BaseDefend = 3,
          CavalryAttack = 3,
          CavalryDefend = 3,
          DisorderProbability = 20,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 6,
        Technology = 200,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Infantry,
        Name = "戟兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 6,
          BaseDefend = 6,
          CavalryAttack = 6,
          CavalryDefend = 6,
          ContinuousProbability = 10,
          DisorderProbability = 40,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 12,
        Technology = 500,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.HeavyInfantry,
        Name = "重戟兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 9,
          BaseDefend = 9,
          CavalryAttack = 9,
          CavalryDefend = 9,
          ContinuousProbability = 30,
          DisorderProbability = 60,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 18,
        Technology = 800,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Seishu,
        Name = "青洲兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 12,
          BaseDefend = 12,
          CavalryAttack = 12,
          CavalryDefend = 12,
          ContinuousProbability = 50,
          DisorderProbability = 80,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 35,
        Technology = 1200,
        CanConscriptWithoutResource = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Cow,
        Name = "軽騎兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 3,
          BaseDefend = 3,
          CrossbowAttack = 3,
          CrossbowDefend = 3,
          RushProbability = 20,
          RushAttack = 2,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 6,
        Technology = 200,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.LightCavalry,
        Name = "騎兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 6,
          BaseDefend = 6,
          CrossbowAttack = 6,
          CrossbowDefend = 6,
          ContinuousProbability = 20,
          RushProbability = 40,
          RushAttack = 4,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 12,
        Technology = 500,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.HeavyCavalry,
        Name = "重騎兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 9,
          BaseDefend = 9,
          CrossbowAttack = 9,
          CrossbowDefend = 9,
          ContinuousProbability = 40,
          RushProbability = 60,
          RushAttack = 6,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 18,
        Technology = 800,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Chariot,
        Name = "戦車兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 12,
          BaseDefend = 12,
          CrossbowAttack = 12,
          CrossbowDefend = 12,
          ContinuousProbability = 60,
          RushProbability = 80,
          RushAttack = 8,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 35,
        Technology = 1200,
        CanConscriptWithoutResource = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Archer,
        Name = "弓兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 3,
          BaseDefend = 3,
          InfantryAttack = 3,
          InfantryDefend = 3,
          RushDefend = 2,
          TypeCrossbow = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 6,
        Technology = 200,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.LongArcher,
        Name = "弩兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 6,
          BaseDefend = 6,
          InfantryAttack = 6,
          InfantryDefend = 6,
          RushDefend = 4,
          TypeCrossbow = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 12,
        Technology = 500,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.StrongCrossbow,
        Name = "強弩兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 9,
          BaseDefend = 9,
          InfantryAttack = 9,
          InfantryDefend = 9,
          ContinuousProbability = 10,
          RushDefend = 6,
          TypeCrossbow = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 18,
        Technology = 800,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.RepeatingCrossbow,
        Name = "連弩兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 12,
          BaseDefend = 12,
          InfantryAttack = 12,
          InfantryDefend = 12,
          ContinuousProbability = 20,
          RushDefend = 8,
          TypeCrossbow = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 35,
        Technology = 1200,
        CanConscriptWithoutResource = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Shosha,
        Name = "衝車",
        Data = new CharacterSoldierTypeData
        {
          WallAttack = 15,
          WallDefend = 5,
          InfantryAttack = -2,
          InfantryDefend = -2,
          CavalryAttack = -2,
          CavalryDefend = -2,
          CrossbowAttack = -2,
          CrossbowDefend = -2,
          TypeWeapon = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 16,
        Technology = 500,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Seiran,
        Name = "井闌",
        Data = new CharacterSoldierTypeData
        {
          WallAttack = 20,
          WallDefend = 10,
          InfantryAttack = -2,
          InfantryDefend = -2,
          CavalryAttack = -2,
          CavalryDefend = -2,
          CrossbowAttack = -2,
          CrossbowDefend = -2,
          TypeWeapon = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 26,
        Technology = 600,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.StoneSlingshot,
        Name = "投石器",
        Data = new CharacterSoldierTypeData
        {
          WallAttack = 25,
          WallDefend = 15,
          InfantryAttack = -2,
          InfantryDefend = -2,
          CavalryAttack = -2,
          CavalryDefend = -2,
          CrossbowAttack = -2,
          CrossbowDefend = -2,
          TypeWeapon = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 40,
        Technology = 1000,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Prayer,
        Name = "祈祷兵",
        Data = new CharacterSoldierTypeData
        {
          GogyoAttack = 12,
          GogyoDefend = 12,
          TypeWeapon = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 20,
        Technology = 800,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Mercenary,
        Name = "槍兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 3,
          BaseDefend = 3,
          CavalryAttack = 9,
          CavalryDefend = 3,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 12,
        Technology = 500,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Stoner,
        Name = "投石兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 5,
          BaseDefend = 5,
          CavalryAttack = 5,
          CavalryDefend = 5,
          DisorderProbability = 30,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 10,
        Technology = 600,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.SpearCavalry,
        Name = "槍騎兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 5,
          BaseDefend = 5,
          CrossbowAttack = 5,
          CrossbowDefend = 5,
          FriendlyFireProbability = 30,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 10,
        Technology = 600,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.ArcherCavalry,
        Name = "弓騎兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 5,
          BaseDefend = 5,
          InfantryAttack = 5,
          InfantryDefend = 5,
          RushProbability = 30,
          RushAttack = 4,
          TypeCrossbow = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 10,
        Technology = 600,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Elephant,
        Name = "象兵",
        Data = new CharacterSoldierTypeData
        {
          CavalryDefend = 2,
          CrossbowAttack = 2,
          RushProbability = 200,
          RushAttack = 12,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 22,
        Technology = 800,
        CanConscriptWithoutResource = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Toko,
        Name = "藤甲兵",
        Data = new CharacterSoldierTypeData
        {
          BaseDefend = 18,
          CavalryAttack = 6,
          CavalryDefend = 12,
          GogyoDefend = 5,
          RushDefend = 5,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 22,
        Technology = 800,
        CanConscriptWithoutResource = false,
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
        Money = 6,
        Technology = 300,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.LightIntellect,
        Name = "梓歩兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 6,
          BaseDefend = 6,
          CavalryAttack = 6,
          CavalryDefend = 6,
          DisorderProbability = 100,
          TypeInfantry = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        Money = 16,
        Technology = 600,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.IntellectHeavyCavalry,
        Name = "梓馬兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 6,
          BaseDefend = 6,
          CrossbowAttack = 6,
          CrossbowDefend = 6,
          RushProbability = 50,
          RushAttack = 6,
          DisorderProbability = 100,
          TypeCavalry = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        Money = 16,
        Technology = 600,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.IntellectArcher,
        Name = "梓弓兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 6,
          BaseDefend = 6,
          InfantryAttack = 6,
          InfantryDefend = 6,
          DisorderProbability = 100,
          TypeCrossbow = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        Money = 16,
        Technology = 600,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.IntellectRepeatingCrossbow,
        Name = "梓琴兵",
        Data = new CharacterSoldierTypeData
        {
          WallAttack = 12,
          WallDefend = 12,
          InfantryAttack = -3,
          InfantryDefend = -3,
          CavalryAttack = -3,
          CavalryDefend = -3,
          CrossbowAttack = -3,
          CrossbowDefend = -3,
          WeaponAttack = -3,
          TypeWeapon = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        Money = 23,
        Technology = 600,
        CanConscriptWithoutSkill = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.IntellectCrossbow,
        Name = "梓弩兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 7,
          BaseDefend = 7,
          InfantryAttack = 7,
          InfantryDefend = 7,
          ContinuousProbability = 30,
          RushProbability = 80,
          RushAttack = 8,
          DisorderProbability = 80,
          FriendlyFireProbability = 80,
          TypeCrossbow = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        Money = 33,
        Technology = 1300,
        CanConscriptWithoutSkill = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Craftsman,
        Name = "工作兵",
        Data = new CharacterSoldierTypeData
        {
          WallAttack = 10,
          WeaponAttack = 4,
          WeaponDefend = 4,
          RushDefend = 2,
          DisorderProbability = 120,
          FriendlyFireProbability = 120,
          TypeInfantry = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        Money = 20,
        Technology = 1000,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.StrongGuards,
        Name = "壁守兵",
        Data = new CharacterSoldierTypeData
        {
          BaseDefend = 11,
          TypeWall = 5,
          TypeInfantry = 5,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        Money = 20,
        Technology = 1000,
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
        Money = 8,
        Technology = 300,
        CanConscriptWithoutSkill = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.PopularityHalberd,
        Name = "義戈兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 3,
          BaseDefend = 1,
          CavalryAttack = 2,
          CavalryDefend = 1,
          GogyoAttack = 2,
          ContinuousProbability = 20,
          TypeInfantry = 10,
          PopularityEx = 1,
          PowerPopularity = 1,
        },
        Money = 12,
        Technology = 400,
        CanConscriptWithoutSkill = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.PopularityCavalry,
        Name = "義殲兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 6,
          BaseDefend = -2,
          CrossbowAttack = 4,
          RushProbability = 30,
          RushAttack = 8,
          TypeCavalry = 10,
          PopularityEx = 1,
          PowerPopularity = 1,
        },
        Money = 12,
        Technology = 300,
        CanConscriptWithoutSkill = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.PopularityStoner,
        Name = "投擲器",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 8,
          BaseDefend = -4,
          WallAttack = 16,
          WallDefend = 8,
          GogyoAttack = 2,
          GogyoDefend = 2,
          FriendlyFireProbability = 40,
          TypeWeapon = 10,
          PopularityEx = 1,
          PowerPopularity = 1,
        },
        Money = 36,
        Technology = 700,
        CanConscriptWithoutSkill = false,
      },
      
      // ----------------------------------------------------------
      
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.LightApostle,
        Name = "使徒見習い",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 3,
          BaseDefend = 3,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 10,
        Technology = 300,
        Kind = SoldierKind.Religion,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Apostle,
        Name = "使徒",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 6,
          BaseDefend = 4,
          TypeInfantry = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        Money = 15,
        Technology = 500,
        Kind = SoldierKind.Religion,
        CanConscriptWithoutSkill = false,
      },

      // ----------------------------------------------------------

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
        Preset = SoldierType.Guard_Step5,
        Name = "守兵E",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 12,
          BaseDefend = 7,
          TypeWall = 10,
          PowerStrong = 1,
        },
        Technology = 32767,
        CanConscript = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Guard_Step6,
        Name = "守兵F",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 15,
          BaseDefend = 8,
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
