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

    /// <summary>
    /// 戦闘ログのID
    /// </summary>
    [Column("battle_log_id")]
    [JsonProperty("battleLogId")]
    public uint BattleLogId { get; set; }
  }

  public enum EventType : short
  {
    Unknown = 0,

    /// <summary>
    /// 収入
    /// </summary>
    Incomes = 1,

    /// <summary>
    /// イベント
    /// </summary>
    Event = 2,

    /// <summary>
    /// 同盟破棄
    /// </summary>
    AllianceBroken = 3,

    /// <summary>
    /// 開戦
    /// </summary>
    WarStart = 4,

    /// <summary>
    /// 同盟締結
    /// </summary>
    AllianceStart = 5,

    /// <summary>
    /// 宣戦布告
    /// </summary>
    WarInReady = 6,

    /// <summary>
    /// 引き分け（相打ち）
    /// </summary>
    BattleDrawLose = 7,

    /// <summary>
    /// 引き分け
    /// </summary>
    BattleDraw = 8,

    /// <summary>
    /// 戦闘敗北
    /// </summary>
    BattleLose = 9,

    /// <summary>
    /// 戦闘勝利
    /// </summary>
    BattleWin = 10,

    /// <summary>
    /// 支配
    /// </summary>
    TakeAway = 11,

    /// <summary>
    /// 滅亡
    /// </summary>
    Overthrown = 12,

    /// <summary>
    /// 天下統一
    /// </summary>
    Unified = 13,

    /// <summary>
    /// リセット
    /// </summary>
    Reset = 14,

    /// <summary>
    /// 仕官（新規）
    /// </summary>
    CharacterEntry = 15,

    /// <summary>
    /// 建国
    /// </summary>
    Publish = 16,

    /// <summary>
    /// 仕官（無所属から）
    /// </summary>
    CharacterJoin = 17,

    /// <summary>
    /// 放置削除
    /// </summary>
    CharacterRemoved = 18,

    /// <summary>
    /// 援軍
    /// </summary>
    ReinforcementActived = 19,

    /// <summary>
    /// 帰還
    /// </summary>
    ReinforcementReturned = 20,

    /// <summary>
    /// 帰順
    /// </summary>
    ReinforcementSubmited = 21,

    /// <summary>
    /// 登用（無所属から）
    /// </summary>
    PromotionAccepted = 22,

    /// <summary>
    /// 異民族
    /// </summary>
    AppendTerrorists = 23,

    /// <summary>
    /// 農民反乱
    /// </summary>
    AppendFarmers = 24,

    /// <summary>
    /// 都市攻略
    /// </summary>
    TownWarInReady = 25,

    /// <summary>
    /// 焼討
    /// </summary>
    Burn = 26,

    /// <summary>
    /// 扇動
    /// </summary>
    Agitation = 27,

    /// <summary>
    /// 政務官雇用
    /// </summary>
    SecretaryAdded = 28,

    /// <summary>
    /// 政務官解任
    /// </summary>
    SecretaryRemoved = 29,

    /// <summary>
    /// 給与不足による政務官削除
    /// </summary>
    SecretaryRemovedWithNoSalary = 30,
  }
}
