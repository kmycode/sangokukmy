using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("map_logs")]
  public class MapLog
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 重要な事柄であるか
    /// </summary>
    [Column("is_important")]
    [JsonProperty("isImportant")]
    public bool IsImportant { get; set; }

    /// <summary>
    /// イベントの種類
    /// </summary>
    [Column("event_type")]
    [JsonIgnore]
    public EventType EventType { get; set; }

    /// <summary>
    /// イベントの種類（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("eventType")]
    public short ApiEventType
    {
      get => (short)this.EventType;
      set => this.EventType = (EventType)value;
    }

    /// <summary>
    /// メッセージ
    /// </summary>
    [Column("message")]
    [JsonProperty("message")]
    public string Message { get; set; }

    /// <summary>
    /// ゲーム内の年月（DB保存用）
    /// </summary>
    [Column("game_date")]
    [JsonIgnore]
    public int IntGameDateTime { get; set; }

    /// <summary>
    /// ゲーム内の年月
    /// </summary>
    [NotMapped]
    [JsonProperty("gameDate")]
    public GameDateTime ApiGameDateTime
    {
      get => GameDateTime.FromInt(this.IntGameDateTime);
      set => this.IntGameDateTime = value.ToInt();
    }

    /// <summary>
    /// ログが出力された日時
    /// </summary>
    [Column("date")]
    [JsonIgnore]
    public DateTime Date { get; set; }

    /// <summary>
    /// ログが出力された日時（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("date")]
    public ApiDateTime ApiDateTime
    {
      get => ApiDateTime.FromDateTime(this.Date);
      set => this.Date = value.ToDateTime();
    }
  }

  public enum EventType : short
  {
    Unknown = 0,

    /// <summary>
    /// 収入
    /// </summary>
    Incomes = 1,
  }
}
