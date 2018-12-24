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
    /// イベント名（支配とか滅亡とか）
    /// </summary>
    [Column("event_name", TypeName = "varchar(64)")]
    [JsonProperty("eventName")]
    public string EventName { get; set; }

    /// <summary>
    /// イベントをあらわす色（#をのぞく16進6文字）
    /// </summary>
    [Column("event_color", TypeName = "varchar(12)")]
    [JsonProperty("eventColor")]
    public string EventColor { get; set; }

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
}
