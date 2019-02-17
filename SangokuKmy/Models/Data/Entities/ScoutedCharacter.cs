using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("scouted_characters")]
  public class ScoutedCharacter
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("scout_id")]
    [JsonIgnore]
    public uint ScoutId { get; set; }

    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    [Column("soldier_type")]
    [JsonProperty("soldierType")]
    public short SoldierType { get; set; }

    [Column("soldier_number")]
    [JsonProperty("soldierNumber")]
    public int SoldierNumber { get; set; }
  }
}
