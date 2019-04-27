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

    /// <summary>
    /// 魚鱗
    /// </summary>
    GyorinA = 1,
  }

  public class FormationTypeInfo
  {
    public FormationType Type { get; set; }

    public string Name { get; set; }

    public List<CharacterSoldierTypeData> Data { get; set; }

    public int RequiredPoint { get; set; }

    public int NextLevel { get; set; }

    public Func<IEnumerable<FormationType>, bool> SubjectAppear { get; set; }

    public bool CanGetByCommand { get; set; } = true;

    public CharacterSoldierTypeData GetDataFromLevel(int level)
    {
      if (this.Data.Count > level - 1)
      {
        return this.Data[level - 1];
      }
      else
      {
        return this.Data[this.Data.Count - 1];
      }
    }

    public bool CheckLevelUp(Formation formation)
    {
      if (formation.Type != this.Type)
      {
        return false;
      }

      if (formation.Level >= this.Data.Count)
      {
        formation.Experience = this.NextLevel;
        return false;
      }

      if (formation.Experience >= this.NextLevel)
      {
        formation.Experience -= this.NextLevel;
        formation.Level++;
        if (formation.Level >= this.Data.Count)
        {
          formation.Experience = this.NextLevel;
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
        Data = new List<CharacterSoldierTypeData>
        {
          new CharacterSoldierTypeData
          {
          },
          new CharacterSoldierTypeData
          {
          },
        },
        RequiredPoint = 0,
        NextLevel = 1000,
      },
      new FormationTypeInfo
      {
        Type = FormationType.GyorinA,
        Name = "魚鱗",
        Data = new List<CharacterSoldierTypeData>
        {
          new CharacterSoldierTypeData
          {
            BaseAttack = 10,
          },
        },
        RequiredPoint = 500,
        NextLevel = 1000,
      },
    };

    public static Optional<FormationTypeInfo> Get(FormationType type)
    {
      return items.FirstOrDefault(t => t.Type == type).ToOptional();
    }
  }
}
