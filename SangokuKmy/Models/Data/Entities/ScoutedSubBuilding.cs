using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("scouted_sub_buildings")]
  public class ScoutedSubBuilding
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("scout_id")]
    [JsonIgnore]
    public uint ScoutId { get; set; }

    [Column("status")]
    [JsonIgnore]
    public TownSubBuildingStatus Status { get; set; }

    [NotMapped]
    [JsonProperty("status")]
    public short ApiStatus
    {
      get => (short)this.Status;
      set => this.Status = (TownSubBuildingStatus)value;
    }

    [Column("type")]
    [JsonIgnore]
    public TownSubBuildingType Type { get; set; }

    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (TownSubBuildingType)value;
    }
  }
}
