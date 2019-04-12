using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("town_defenders")]
  public class TownDefender
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [NotMapped]
    [JsonIgnore]
    public TownDefenderStatus Status { get; set; }

    [NotMapped]
    [JsonProperty("status")]
    public short ApiStatus
    {
      get => (short)this.Status;
      set => this.Status = (TownDefenderStatus)value;
    }

    /// <summary>
    /// 都市ID
    /// </summary>
    [Column("town_id")]
    [JsonProperty("townId")]
    public uint TownId { get; set; }

    /// <summary>
    /// 武将ID
    /// </summary>
    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }
  }

  public enum TownDefenderStatus : short
  {
    Available = 0,
    Losed = 1,
  }
}
