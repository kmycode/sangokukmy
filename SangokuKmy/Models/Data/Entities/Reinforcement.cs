using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("reinforcements")]
  public class Reinforcement
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    [Column("character_country_id")]
    [JsonProperty("characterCountryId")]
    public uint CharacterCountryId { get; set; }

    [Column("requested_country_id")]
    [JsonProperty("requestedCountryId")]
    public uint RequestedCountryId { get; set; }

    [Column("status")]
    [JsonIgnore]
    public ReinforcementStatus Status { get; set; }

    [NotMapped]
    [JsonProperty("status")]
    public short ApiStatus
    {
      get => (short)this.Status;
      set => this.Status = (ReinforcementStatus)value;
    }
  }

  public enum ReinforcementStatus : short
  {
    None = 0,
    Requesting = 1,
    RequestDismissed = 2,
    RequestCanceled = 3,
    Active = 4,
    Returned = 5,
    Submited = 6,
  }
}
