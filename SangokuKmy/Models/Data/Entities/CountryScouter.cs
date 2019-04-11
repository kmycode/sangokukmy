using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("country_scouters")]
  public class CountryScouter
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("country_id")]
    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    [Column("town_id")]
    [JsonProperty("townId")]
    public uint TownId { get; set; }

    [NotMapped]
    [JsonProperty("isRemoved")]
    public bool IsRemoved { get; set; }
  }
}
