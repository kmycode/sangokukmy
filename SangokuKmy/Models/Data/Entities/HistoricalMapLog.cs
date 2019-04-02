using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("historical_maplogs")]
  public class HistoricalMapLog
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("history_id")]
    [JsonIgnore]
    public uint HistoryId { get; set; }

    [Column("event_type")]
    [JsonIgnore]
    public EventType EventType { get; set; }
    
    [NotMapped]
    [JsonProperty("eventType")]
    public short ApiEventType
    {
      get => (short)this.EventType;
      set => this.EventType = (EventType)value;
    }
    
    [Column("message")]
    [JsonProperty("message")]
    public string Message { get; set; }
    
    [Column("game_date")]
    [JsonIgnore]
    public int IntGameDateTime { get; set; }
    
    [NotMapped]
    [JsonProperty("gameDate")]
    public GameDateTime ApiGameDateTime
    {
      get => GameDateTime.FromInt(this.IntGameDateTime);
      set => this.IntGameDateTime = value.ToInt();
    }
    
    [Column("date")]
    [JsonIgnore]
    public DateTime Date { get; set; }
    
    [NotMapped]
    [JsonProperty("date")]
    public ApiDateTime ApiDateTime
    {
      get => ApiDateTime.FromDateTime(this.Date);
      set => this.Date = value.ToDateTime();
    }

    public static HistoricalMapLog FromMapLog(MapLog log)
    {
      return new HistoricalMapLog
      {
        Message = log.Message,
        EventType = log.EventType,
        ApiGameDateTime = log.ApiGameDateTime,
        Date = log.Date,
      };
    }
  }
}
