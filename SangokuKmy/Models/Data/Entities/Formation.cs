using Newtonsoft.Json;
using SangokuKmy.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("formations")]
  public class Formation
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    [Column("type")]
    [JsonIgnore]
    public FormationType Type { get; set; }

    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (FormationType)value;
    }

    [Column("experience")]
    [JsonProperty("experience")]
    public int Experience { get; set; }

    [Column("level")]
    [JsonProperty("level")]
    public short Level { get; set; }
  }

  public enum FormationType : short
  {
    Normal = 0,
    Gyorin = 1,
    Hoshi = 2,
    Suiko = 3,
    Kakuyoku = 4,
    Hoen = 5,
    Koyaku = 6,
    Engetsu = 7,
    Ganko = 8,
    Choda = 9,
    Kojo = 10,
  }

  public class FormationTypeLevelInfo
  {
    public CharacterSoldierTypeData Data { get; set; }

    public int NextLevel { get; set; }
  }

  public class FormationTypeInfo
  {
    public FormationType Type { get; set; }

    public string Name { get; set; }

    public List<FormationTypeLevelInfo> Levels { get; set; }

    public int RequiredPoint { get; set; }

    public Func<IEnumerable<Formation>, bool> SubjectAppear { get; set; }

    public bool CanGetByCommand { get; set; } = true;

    public CharacterSoldierTypeData GetDataFromLevel(int level)
    {
      if (this.Levels.Count > level - 1)
      {
        return this.Levels[level - 1].Data;
      }
      else
      {
        return this.Levels[this.Levels.Count - 1].Data;
      }
    }

    public bool CheckLevelUp(Formation formation)
    {
      if (formation.Type != this.Type)
      {
        return false;
      }

      if (formation.Level >= this.Levels.Count)
      {
        formation.Experience = 0;
        return false;
      }

      var nextLevel = this.Levels[formation.Level - 1].NextLevel;
      if (formation.Experience >= nextLevel)
      {
        formation.Experience -= nextLevel;
        formation.Level++;
        if (formation.Level >= this.Levels.Count)
        {
          formation.Experience = 0;
        }
        return true;
      }

      return false;
    }
  }

  public static class FormationTypeInfoes
  {
    private static readonly List<FormationTypeInfo> items = new List<FormationTypeInfo>
    {
      new FormationTypeInfo
      {
        Type = FormationType.Normal,
        Name = "通常",
        Levels = new List<FormationTypeLevelInfo>
        {
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
            },
            NextLevel = 1000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              BaseAttack = 4,
            },
            NextLevel = 3000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              BaseAttack = 8,
            },
            NextLevel = 6000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              BaseAttack = 16,
            },
            NextLevel = 10000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              BaseAttack = 32,
            },
            NextLevel = 10000,
          },
        },
        RequiredPoint = 0,
      },
      new FormationTypeInfo
      {
        Type = FormationType.Kakuyoku,
        Name = "鶴翼",
        Levels = new List<FormationTypeLevelInfo>
        {
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoFire = 100,
              BaseAttack = 8,
            },
            NextLevel = 1000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoFire = 100,
              BaseAttack = 16,
            },
            NextLevel = 2000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoFire = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 16,
            },
            NextLevel = 5000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoFire = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 32,
              GogyoDefend = 16,
              RushProbability = 1600,
              RushAttack = 40,
            },
            NextLevel = 8000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoFire = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 48,
              GogyoDefend = 32,
              RushProbability = 1600,
              RushAttack = 40,
              ContinuousProbability = 400,
            },
            NextLevel = 10000,
          },
        },
        RequiredPoint = 500,
      },
      new FormationTypeInfo
      {
        Type = FormationType.Gyorin,
        Name = "魚鱗",
        Levels = new List<FormationTypeLevelInfo>
        {
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoWater = 100,
              BaseAttack = 8,
            },
            NextLevel = 1000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoWater = 100,
              BaseAttack = 16,
            },
            NextLevel = 2000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoWater = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 16,
            },
            NextLevel = 5000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoWater = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 32,
              GogyoDefend = 16,
              RushProbability = 1600,
              RushAttack = 40,
            },
            NextLevel = 8000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoWater = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 48,
              GogyoDefend = 32,
              RushProbability = 1600,
              RushAttack = 40,
              ContinuousProbability = 400,
            },
            NextLevel = 10000,
          },
        },
        RequiredPoint = 500,
      },
      new FormationTypeInfo
      {
        Type = FormationType.Hoen,
        Name = "方円",
        Levels = new List<FormationTypeLevelInfo>
        {
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoTree = 100,
              BaseAttack = 8,
            },
            NextLevel = 1000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoTree = 100,
              BaseAttack = 16,
            },
            NextLevel = 2000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoTree = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 16,
            },
            NextLevel = 5000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoTree = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 32,
              GogyoDefend = 60,
              RushProbability = 100,
              RushAttack = 40,
            },
            NextLevel = 8000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoTree = 100,
              BaseAttack = 16,
              BaseDefend = 32,
              GogyoAttack = 48,
              GogyoDefend = 96,
              RushProbability = 400,
              RushAttack = 40,
              RushDefend = 80,
              ContinuousProbability = 200,
            },
            NextLevel = 10000,
          },
        },
        RequiredPoint = 500,
      },
      new FormationTypeInfo
      {
        Type = FormationType.Koyaku,
        Name = "衡軛",
        Levels = new List<FormationTypeLevelInfo>
        {
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoSoil = 100,
              BaseAttack = 8,
            },
            NextLevel = 1000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoSoil = 100,
              BaseAttack = 16,
            },
            NextLevel = 2000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoSoil = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 16,
            },
            NextLevel = 5000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoSoil = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 32,
              GogyoDefend = 16,
              RushProbability = 1600,
              RushAttack = 40,
            },
            NextLevel = 8000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoSoil = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 48,
              GogyoDefend = 32,
              RushProbability = 1600,
              RushAttack = 40,
              ContinuousProbability = 400,
            },
            NextLevel = 10000,
          },
        },
        RequiredPoint = 500,
      },
      new FormationTypeInfo
      {
        Type = FormationType.Hoshi,
        Name = "鋒矢",
        Levels = new List<FormationTypeLevelInfo>
        {
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoFire = 100,
              BaseAttack = 8,
            },
            NextLevel = 1000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoFire = 100,
              BaseAttack = 16,
            },
            NextLevel = 2000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoFire = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 16,
            },
            NextLevel = 5000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoFire = 100,
              BaseAttack = 32,
              BaseDefend = 16,
              GogyoAttack = 48,
              GogyoDefend = 16,
              RushProbability = 800,
              RushAttack = 80,
            },
            NextLevel = 8000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoFire = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 60,
              GogyoDefend = 16,
              RushProbability = 1200,
              RushAttack = 120,
            },
            NextLevel = 10000,
          },
        },
        RequiredPoint = 500,
      },
      new FormationTypeInfo
      {
        Type = FormationType.Engetsu,
        Name = "偃月",
        Levels = new List<FormationTypeLevelInfo>
        {
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoWater = 100,
              BaseAttack = 8,
            },
            NextLevel = 1000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoWater = 100,
              BaseAttack = 16,
            },
            NextLevel = 2000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoWater = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 16,
            },
            NextLevel = 5000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoWater = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 16,
              GogyoDefend = 8,
              RushProbability = 1400,
              RushAttack = 28,
            },
            NextLevel = 8000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoWater = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 32,
              GogyoDefend = 16,
              RushProbability = 2000,
              RushAttack = 48,
              ContinuousProbability = 400,
            },
            NextLevel = 10000,
          },
        },
        RequiredPoint = 500,
      },
      new FormationTypeInfo
      {
        Type = FormationType.Ganko,
        Name = "雁行",
        Levels = new List<FormationTypeLevelInfo>
        {
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoTree = 100,
              BaseAttack = 8,
            },
            NextLevel = 1000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoTree = 100,
              BaseAttack = 16,
            },
            NextLevel = 2000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoTree = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 16,
            },
            NextLevel = 5000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoTree = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 32,
              GogyoDefend = 16,
              RushProbability = 1600,
              RushAttack = 40,
            },
            NextLevel = 8000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoTree = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 48,
              GogyoDefend = 32,
              RushProbability = 1600,
              RushAttack = 40,
              ContinuousProbability = 400,
            },
            NextLevel = 10000,
          },
        },
        RequiredPoint = 500,
      },
      new FormationTypeInfo
      {
        Type = FormationType.Choda,
        Name = "長蛇",
        Levels = new List<FormationTypeLevelInfo>
        {
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoSoil = 100,
              BaseAttack = 8,
            },
            NextLevel = 1000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoSoil = 100,
              BaseAttack = 16,
            },
            NextLevel = 2000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoSoil = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 16,
            },
            NextLevel = 5000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoSoil = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 16,
              GogyoDefend = 16,
              RushProbability = 300,
              RushAttack = 40,
              ContinuousProbability = 1200,
            },
            NextLevel = 8000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoSoil = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 32,
              GogyoDefend = 32,
              RushProbability = 400,
              RushAttack = 40,
              ContinuousProbability = 2000,
            },
            NextLevel = 10000,
          },
        },
        RequiredPoint = 500,
      },
      new FormationTypeInfo
      {
        Type = FormationType.Suiko,
        Name = "錐行",
        Levels = new List<FormationTypeLevelInfo>
        {
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoMetal = 100,
              BaseAttack = 8,
            },
            NextLevel = 1000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoMetal = 100,
              BaseAttack = 16,
            },
            NextLevel = 2000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoMetal = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 16,
            },
            NextLevel = 5000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoMetal = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 32,
              GogyoDefend = 16,
              RushProbability = 1600,
              RushAttack = 40,
            },
            NextLevel = 8000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoMetal = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              GogyoAttack = 48,
              GogyoDefend = 32,
              RushProbability = 1600,
              RushAttack = 40,
              ContinuousProbability = 400,
            },
            NextLevel = 10000,
          },
        },
        RequiredPoint = 500,
      },
      new FormationTypeInfo
      {
        Type = FormationType.Kojo,
        Name = "攻城",
        Levels = new List<FormationTypeLevelInfo>
        {
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoMetal = 100,
              BaseAttack = 8,
            },
            NextLevel = 1000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoMetal = 100,
              BaseAttack = 16,
            },
            NextLevel = 2000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoMetal = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              WallAttack = 32,
            },
            NextLevel = 5000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoMetal = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              WallAttack = 72,
              WallDefend = 24,
              RushProbability = 1600,
              RushAttack = 40,
            },
            NextLevel = 8000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              GogyoMetal = 100,
              BaseAttack = 16,
              BaseDefend = 16,
              WallAttack = 120,
              WallDefend = 60,
              RushProbability = 1600,
              RushAttack = 40,
              ContinuousProbability = 400,
            },
            NextLevel = 10000,
          },
        },
        RequiredPoint = 500,
      },
    };

    public static Optional<FormationTypeInfo> Get(FormationType type)
    {
      return items.FirstOrDefault(t => t.Type == type).ToOptional();
    }

    public static IEnumerable<FormationTypeInfo> GetAll()
    {
      return items;
    }

    public static IEnumerable<FormationTypeInfo> GetAllGettables(IEnumerable<Formation> alreadys)
    {
      return items.Where(t => t.SubjectAppear == null || t.SubjectAppear(alreadys));
    }
  }
}
