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

    public CharacterSoldierTypeData Data { get; set; }

    public int RequiredPoint { get; set; }
    
    public Func<IEnumerable<FormationType>, bool> SubjectAppear { get; set; }

    public bool CanGetByCommand { get; set; } = true;
  }

  public static class FormationTypeInfoes
  {
    private static readonly List<FormationTypeInfo> items = new List<FormationTypeInfo>
    {
      new FormationTypeInfo
      {
        Type = FormationType.Normal,
        Name = "通常",
        Data = new CharacterSoldierTypeData
        {
        },
        RequiredPoint = 0,
      },
      new FormationTypeInfo
      {
        Type = FormationType.GyorinA,
        Name = "魚鱗 Lv.1",
        Data = new CharacterSoldierTypeData
        {
          BaseAttack = 20,
        },
        RequiredPoint = 500,
      },
    };

    public static Optional<FormationTypeInfo> Get(FormationType type)
    {
      return items.FirstOrDefault(t => t.Type == type).ToOptional();
    }
  }
}
