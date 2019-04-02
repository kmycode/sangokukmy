using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("historical_towns")]
  public class HistoricalTown
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("history_id")]
    [JsonIgnore]
    public uint HistoryId { get; set; }

    [Column("name", TypeName = "varchar(64)")]
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [Column("x")]
    [JsonProperty("x")]
    public short X { get; set; }
    
    [Column("y")]
    [JsonProperty("y")]
    public short Y { get; set; }

    public static HistoricalTown FromTown(Town town)
    {
      return new HistoricalTown
      {
        Name = town.Name,
        X = town.X,
        Y = town.Y,
      };
    }
  }
}
