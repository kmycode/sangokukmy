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
          BaseDefend = 1,
          CavalryAttack = 3,
          CrossbowDefend = 1,
          GogyoDefend = 1,
          RushProbability = 20,
          RushAttack = 3,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 5,
        Technology = 100,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Cow,
        Name = "牛兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 3,
          BaseDefend = 1,
          CrossbowAttack = 2,
          GogyoAttack = 2,
          RushProbability = 20,
          RushAttack = 4,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 5,
        Technology = 200,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Stoner,
        Name = "投石兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 1,
          BaseDefend = 3,
          InfantryAttack = 2,
          WeaponAttack = 2,
          GogyoAttack = 2,
          DisorderProbability = 30,
          TypeCrossbow = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 5,
        Technology = 200,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Archer,
        Name = "弓兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 2,
          BaseDefend = 2,
          InfantryAttack = 3,
          InfantryDefend = 1,
          GogyoAttack = 1,
          GogyoDefend = 2,
          TypeCrossbow = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 5,
        Technology = 200,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Prayer,
        Name = "祈祷兵",
        Data = new CharacterSoldierTypeData
        {
          GogyoAttack = 10,
          GogyoDefend = 5,
          ContinuousProbability = 20,
          TypeWeapon = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 8,
        Technology = 300,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.LightCavalry,
        Name = "軽騎兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 4,
          BaseDefend = 2,
          CrossbowAttack = 3,
          CrossbowDefend = 1,
          WeaponAttack = 2,
          GogyoAttack = 2,
          GogyoDefend = 1,
          ContinuousProbability = 20,
          RushProbability = 30,
          RushAttack = 6,
          RushDefend = 1,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 11,
        Technology = 300,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Infantry,
        Name = "戟兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 4,
          BaseDefend = 2,
          CavalryAttack = 3,
          CavalryDefend = 1,
          GogyoAttack = 1,
          GogyoDefend = 2,
          ContinuousProbability = 20,
          RushProbability = 30,
          RushAttack = 5,
          RushDefend = 1,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 11,
        Technology = 300,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Mercenary,
        Name = "槍兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 3,
          BaseDefend = 2,
          CavalryAttack = 6,
          CavalryDefend = 3,
          GogyoAttack = 2,
          GogyoDefend = 1,
          ContinuousProbability = 10,
          RushProbability = 10,
          RushAttack = 5,
          RushDefend = 3,
          DisorderProbability = 30,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 11,
        Technology = 400,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Shosha,
        Name = "衝車",
        Data = new CharacterSoldierTypeData
        {
          WallAttack = 12,
          WallDefend = 8,
          WeaponAttack = 3,
          WeaponDefend = 2,
          TypeWeapon = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Agriculture,
        },
        Money = 18,
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
          InfantryAttack = 2,
          InfantryDefend = 2,
          TypeWeapon = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Commercial,
        },
        Money = 30,
        Technology = 600,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.StoneSlingshot,
        Name = "投石器",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 3,
          BaseDefend = 1,
          WallAttack = 18,
          WallDefend = 8,
          TypeWeapon = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        Money = 40,
        Technology = 600,
        CanConscriptWithoutSubBuilding = false,
        NeededSubBuildingType = TownSubBuildingType.LargeWorkshop,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.SpearCavalry,
        Name = "槍騎兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 4,
          BaseDefend = 2,
          InfantryAttack = 2,
          CrossbowAttack = 4,
          CrossbowDefend = 2,
          WeaponAttack = 1,
          GogyoAttack = 4,
          ContinuousProbability = 40,
          RushDefend = 2,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Fortress,
        },
        Money = 11,
        Technology = 600,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.ArcherCavalry,
        Name = "弓騎兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 2,
          BaseDefend = 4,
          InfantryAttack = 4,
          InfantryDefend = 2,
          CrossbowAttack = 2,
          WeaponAttack = 1,
          GogyoAttack = 2,
          GogyoDefend = 3,
          ContinuousProbability = 20,
          RushDefend = 2,
          TypeCrossbow = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Fortress,
        },
        Money = 11,
        Technology = 600,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.HeavyInfantry,
        Name = "重戟兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 6,
          BaseDefend = 3,
          CavalryAttack = 4,
          CavalryDefend = 2,
          WeaponAttack = 1,
          GogyoAttack = 2,
          GogyoDefend = 2,
          ContinuousProbability = 10,
          RushProbability = 20,
          RushAttack = 4,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Agriculture,
        },
        Money = 17,
        Technology = 800,
        CanConscriptWithoutResource = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.HeavyCavalry,
        Name = "重騎兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 6,
          BaseDefend = 3,
          CrossbowAttack = 4,
          CrossbowDefend = 2,
          WeaponAttack = 1,
          GogyoAttack = 2,
          ContinuousProbability = 40,
          RushProbability = 20,
          RushAttack = 4,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Commercial,
        },
        Money = 17,
        Technology = 800,
        CanConscriptWithoutResource = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.StrongCrossbow,
        Name = "強弩兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 5,
          BaseDefend = 4,
          InfantryAttack = 4,
          InfantryDefend = 2,
          WeaponAttack = 1,
          GogyoAttack = 2,
          GogyoDefend = 2,
          TypeCrossbow = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Fortress,
        },
        Money = 17,
        Technology = 800,
        CanConscriptWithoutResource = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.RepeatingCrossbow,
        Name = "連弩兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 2,
          BaseDefend = 3,
          WallDefend = 1,
          InfantryAttack = 4,
          InfantryDefend = 2,
          CavalryAttack = 1,
          CavalryDefend = 1,
          CrossbowAttack = 1,
          CrossbowDefend = 1,
          WeaponAttack = 1,
          WeaponDefend = 1,
          GogyoAttack = 5,
          GogyoDefend = 3,
          RushDefend = 3,
          DisorderProbability = 40,
          TypeCrossbow = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Fortress,
        },
        Money = 19,
        Technology = 1000,
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
          CavalryAttack = 6,
          CavalryDefend = 3,
          RushProbability = 20,
          RushAttack = 6,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Agriculture,
        },
        Money = 24,
        Technology = 700,
        CanConscriptWithoutResource = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Chariot,
        Name = "戦車兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 9,
          BaseDefend = 5,
          CrossbowAttack = 6,
          CrossbowDefend = 3,
          GogyoAttack = 2,
          ContinuousProbability = 30,
          DisorderProbability = 30,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Fortress,
        },
        Money = 24,
        Technology = 800,
        CanConscriptWithoutResource = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Elephant,
        Name = "象兵",
        Data = new CharacterSoldierTypeData
        {
          CrossbowAttack = 2,
          CrossbowDefend = 2,
          RushProbability = 200,
          RushAttack = 12,
          TypeCavalry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Agriculture,
        },
        Money = 24,
        Technology = 900,
        CanConscriptWithoutResource = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Toko,
        Name = "藤甲兵",
        Data = new CharacterSoldierTypeData
        {
          BaseDefend = 15,
          CavalryAttack = 2,
          CavalryDefend = 6,
          WeaponDefend = 2,
          GogyoDefend = 4,
          DisorderProbability = 40,
          TypeInfantry = 10,
          StrongEx = 1,
          PowerStrong = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Commercial,
        },
        Money = 24,
        Technology = 900,
        CanConscriptWithoutResource = false,
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
        Money = 15,
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
        TownTypes = new List<TownType>
        {
          TownType.Agriculture,
        },
        Money = 15,
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
        TownTypes = new List<TownType>
        {
          TownType.Agriculture,
        },
        Money = 40,
        Technology = 700,
        CanConscriptWithoutSkill = false,
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
        Money = 8,
        Technology = 300,
        CanConscriptWithoutSkill = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.LightIntellect,
        Name = "梓神兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 3,
          BaseDefend = 1,
          CavalryAttack = 3,
          GogyoAttack = 1,
          DisorderProbability = 120,
          TypeInfantry = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        Money = 15,
        Technology = 400,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.IntellectArcher,
        Name = "梓弓兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 2,
          BaseDefend = 2,
          InfantryAttack = 2,
          InfantryDefend = 1,
          GogyoAttack = 1,
          GogyoDefend = 1,
          DisorderProbability = 100,
          TypeCrossbow = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Commercial,
        },
        Money = 15,
        Technology = 500,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.IntellectHeavyCavalry,
        Name = "梓馬兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 4,
          CrossbowAttack = 2,
          CrossbowDefend = 1,
          RushProbability = 20,
          RushAttack = 6,
          DisorderProbability = 100,
          TypeCavalry = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Agriculture,
        },
        Money = 15,
        Technology = 700,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.IntellectRepeatingCrossbow,
        Name = "梓琴兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 4,
          BaseDefend = 2,
          InfantryAttack = 4,
          InfantryDefend = 2,
          WeaponAttack = 2,
          WeaponDefend = 2,
          FriendlyFireProbability = 50,
          TypeCrossbow = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        Money = 22,
        Technology = 700,
        CanConscriptWithoutSkill = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.IntellectCrossbow,
        Name = "梓弩兵",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 3,
          BaseDefend = 3,
          InfantryAttack = 2,
          InfantryDefend = 1,
          GogyoAttack = 4,
          GogyoDefend = 3,
          DisorderProbability = 80,
          TypeCrossbow = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Commercial,
        },
        Money = 22,
        Technology = 1000,
        CanConscriptWithoutSkill = false,
      },
      new CharacterSoldierTypePart
      {
        Preset = SoldierType.Craftsman,
        Name = "工作兵",
        Data = new CharacterSoldierTypeData
        {
          WallAttack = 8,
          CavalryAttack = 1,
          WeaponAttack = 6,
          WeaponDefend = 2,
          RushDefend = 2,
          DisorderProbability = 120,
          FriendlyFireProbability = 120,
          TypeInfantry = 10,
          IntellectEx = 1,
          PowerIntellect = 1,
        },
        TownTypes = new List<TownType>
        {
          TownType.Agriculture,
        },
        Money = 22,
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
        TownTypes = new List<TownType>
        {
          TownType.Fortress,
        },
        Money = 22,
        Technology = 1000,
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
