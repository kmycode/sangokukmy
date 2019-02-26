using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("scouted_buildings")]
  public class ScoutedBuilding : TownBuildingBase
  {
    [Column("scout_id")]
    [JsonIgnore]
    public uint ScoutId { get; set; }
  }
}
