using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SangokuKmy.Models.Data.Entities
{
  public class TownBuildingBase
  {
    [Key]
    [Column("id")]
    [JsonIgnore]
    public uint Id { get; set; }

    [Column("town_id")]
    [JsonIgnore]
    public uint TownId { get; set; }

    [Column("type")]
    [JsonIgnore]
    public TownBuildingType Type { get; set; }

    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (TownBuildingType)value;
    }

    [Column("value")]
    [JsonProperty("value")]
    public int Value { get; set; }

    [Column("value_max")]
    [JsonProperty("valueMax")]
    public int ValueMax { get; set; }
  }

  [Table("town_buildings")]
  public class TownBuilding : TownBuildingBase
  {
  }

  public enum TownBuildingType : short
  {
    None = 0,
  }
}
