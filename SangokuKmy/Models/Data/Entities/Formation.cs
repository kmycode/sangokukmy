﻿using Newtonsoft.Json;
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
      /*
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
              TypeInfantryAttack = 10,
            },
            NextLevel = 1000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              TypeInfantryAttack = 19,
            },
            NextLevel = 3000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              TypeInfantryAttack = 27,
              CavalryAttack = 8,
            },
            NextLevel = 6000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              TypeInfantryAttack = 34,
              CavalryAttack = 8,
              RushProbability = 100,
              RushAttack = 35,
            },
            NextLevel = 10000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              TypeInfantryAttack = 40,
              CavalryAttack = 20,
              RushProbability = 300,
              RushAttack = 85,
            },
            NextLevel = 10000,
          },
        },
        RequiredPoint = 500,
      },
      new FormationTypeInfo
      {
        Type = FormationType.Hoshi,
        Name = "蜂矢",
        Levels = new List<FormationTypeLevelInfo>
        {
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              TypeCavalryAttack = 10,
            },
            NextLevel = 1000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              TypeCavalryAttack = 19,
            },
            NextLevel = 3000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              TypeCavalryAttack = 27,
              CrossbowAttack = 8,
            },
            NextLevel = 6000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              TypeCavalryAttack = 34,
              CrossbowAttack = 8,
              RushProbability = 100,
              RushAttack = 35,
            },
            NextLevel = 10000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              TypeCavalryAttack = 40,
              CrossbowAttack = 20,
              RushProbability = 300,
              RushAttack = 85,
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
              TypeCrossbowAttack = 10,
            },
            NextLevel = 1000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              TypeCrossbowAttack = 19,
            },
            NextLevel = 3000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              TypeCrossbowAttack = 27,
              InfantryAttack = 8,
            },
            NextLevel = 6000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              TypeCrossbowAttack = 34,
              InfantryAttack = 8,
              RushProbability = 100,
              RushAttack = 35,
            },
            NextLevel = 10000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              TypeCrossbowAttack = 40,
              InfantryAttack = 20,
              RushProbability = 300,
              RushAttack = 85,
            },
            NextLevel = 10000,
          },
        },
        RequiredPoint = 500,
      },
      */
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
