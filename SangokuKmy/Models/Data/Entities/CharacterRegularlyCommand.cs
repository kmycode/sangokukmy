using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("character_regularly_commands")]
  public class CharacterRegularlyCommand
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("character_id")]
    [JsonIgnore]
    public uint CharacterId { get; set; }

    [Column("type")]
    [JsonIgnore]
    public CharacterCommandType Type { get; set; }

    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (CharacterCommandType)value;
    }

    [Column("option1")]
    [JsonProperty("option1")]
    public int Option1 { get; set; }

    [Column("option2")]
    [JsonProperty("option2")]
    public int Option2 { get; set; }

    [Column("next_run_game_date_time")]
    [JsonIgnore]
    public int IntNextRunGameDateTime { get; set; }

    [NotMapped]
    [JsonProperty("nextRunGameDateTime")]
    public GameDateTime NextRunGameDateTime
    {
      get => GameDateTime.FromInt(this.IntNextRunGameDateTime);
      set => this.IntNextRunGameDateTime = value.ToInt();
    }

    [NotMapped]
    [JsonProperty("hasRemoved")]
    public bool HasRemoved { get; set; }
  }
}
