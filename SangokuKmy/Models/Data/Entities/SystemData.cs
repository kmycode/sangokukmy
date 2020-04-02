using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Common;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  /// <summary>
  /// システムデータ
  /// </summary>
  [Table("system_data")]
  public class SystemData
  {
    /// <summary>
    /// ゲームプログラムが初めて起動されたときのデータ（期が開始されるタイミングではない）
    /// </summary>
    public static SystemData Initialized => new SystemData
    {
      IsDebug = false,
      Period = 0,
      BetaVersion = 0,
      GameDateTime = new GameDateTime
      {
        Year = Config.StartYear,
        Month = Config.StartMonth,
      },
      CurrentMonthStartDateTime = DateTime.Now,
    };

    [Key]
    [Column("id")]
    [JsonIgnore]
    public uint Id { get; set; }

    /// <summary>
    /// デバッグモードであるぁ
    /// </summary>
    [Column("is_debug")]
    [JsonIgnore]
    public bool IsDebug { get; set; }

    /// <summary>
    /// 期
    /// </summary>
    [Column("period")]
    [JsonProperty("period")]
    public short Period { get; set; }

    /// <summary>
    /// ベータのバージョン。0で正式な期
    /// </summary>
    [Column("beta_version")]
    [JsonProperty("betaVersion")]
    public short BetaVersion { get; set; }

    /// <summary>
    /// 次の期はベータであるか
    /// </summary>
    [Column("is_next_period_beta")]
    [JsonIgnore]
    public bool IsNextPeriodBeta { get; set; }

    /// <summary>
    /// 現在のゲーム内の年月（DB保存用）
    /// </summary>
    [Column("game_date_time")]
    public int IntGameDateTime
    {
      get => this.GameDateTime.ToInt();
      set => this.GameDateTime = GameDateTime.FromInt(value);
    }

    /// <summary>
    /// 現在のゲーム内の年月
    /// </summary>
    [NotMapped]
    [JsonProperty("gameDateTime")]
    public GameDateTime GameDateTime { get; set; }

    /// <summary>
    /// リセット待ちであるか
    /// </summary>
    [Column("is_waiting_reset")]
    [JsonProperty("isWaitingReset")]
    public bool IsWaitingReset { get; set; }

    /// <summary>
    /// リセットするゲーム内の年月（DB保存用）
    /// </summary>
    [Column("reset_game_date_time")]
    public int IntResetGameDateTime
    {
      get => this.ResetGameDateTime.ToInt();
      set => this.ResetGameDateTime = GameDateTime.FromInt(value);
    }

    /// <summary>
    /// リセットするゲーム内の年月
    /// </summary>
    [NotMapped]
    [JsonProperty("resetGameDateTime")]
    public GameDateTime ResetGameDateTime { get; set; }

    /// <summary>
    /// 現在の月が始まった時刻
    /// </summary>
    [Column("current_month_start_date_time")]
    [JsonIgnore]
    public DateTime CurrentMonthStartDateTime { get; set; }

    /// <summary>
    /// 現在の月が始まった時刻（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("currentMonthStartDateTime")]
    public ApiDateTime ApiCurrentMonthStartDateTime
    {
      get => ApiDateTime.FromDateTime(this.CurrentMonthStartDateTime);
      set => this.CurrentMonthStartDateTime = value.ToDateTime();
    }

    /// <summary>
    /// 新規登録時に招待コードが必要であるか
    /// </summary>
    [Column("invitation_code_requested_entry")]
    [JsonProperty("invitationCodeRequestedAtEntry")]
    public bool InvitationCodeRequestedAtEntry { get; set; }

    /// <summary>
    /// 異民族の出現回数
    /// </summary>
    [Column("terrorist_count")]
    [JsonIgnore]
    public short TerroristCount { get; set; }

    /// <summary>
    /// 国家の出現回数
    /// </summary>
    [Column("management_count")]
    [JsonIgnore]
    public short ManagementCountryCount { get; set; }

    /// <summary>
    /// バトルロワイヤルモード
    /// </summary>
    [Column("is_battle_royale_mode")]
    [JsonProperty("isBattleRoyaleMode")]
    public bool IsBattleRoyaleMode { get; set; }
  }
}
