using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("country_researches")]
  public class CountryResearch
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("country_id")]
    [JsonIgnore]
    public uint CountryId { get; set; }

    [Column("status")]
    [JsonIgnore]
    public CountryResearchStatus Status { get; set; }

    [NotMapped]
    [JsonProperty("status")]
    public short ApiStatus
    {
      get => (short)this.Status;
      set => this.Status = (CountryResearchStatus)value;
    }

    [Column("type")]
    [JsonIgnore]
    public CountryResearchType Type { get; set; }

    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (CountryResearchType)value;
    }

    [Column("level")]
    [JsonProperty("level")]
    public short Level { get; set; }

    [Column("progress")]
    [JsonProperty("progress")]
    public int Progress { get; set; }

    [Column("progress_max")]
    [JsonProperty("progressMax")]
    public int ProgressMax { get; set; }
  }

  public enum CountryResearchStatus : short
  {
    Pending = 0,
    InReady = 1,
    Researching = 2,
    Available = 3,
  }

  public enum CountryResearchType : short
  {
    Undefined = 0,
  }
}
