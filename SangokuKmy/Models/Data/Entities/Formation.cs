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
              BaseAttack = 1,
            },
            NextLevel = 3000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              BaseAttack = 2,
            },
            NextLevel = 6000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              BaseAttack = 4,
            },
            NextLevel = 10000,
          },
          new FormationTypeLevelInfo
          {
            Data = new CharacterSoldierTypeData
            {
              BaseAttack = 8,
            },
            NextLevel = 10000,
          },
        },
        RequiredPoint = 0,
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
