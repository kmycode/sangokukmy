using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Common;

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
      Period = 1,
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
    public uint Id { get; set; }

    /// <summary>
    /// 期
    /// </summary>
    [Column("period")]
    public short Period { get; set; }

    /// <summary>
    /// ベータのバージョン。0で正式な期
    /// </summary>
    [Column("beta_version")]
    public short BetaVersion { get; set; }

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
    public GameDateTime GameDateTime { get; set; }

    /// <summary>
    /// 現在の月が始まった時刻
    /// </summary>
    [Column("current_month_start_date_time")]
    public DateTime CurrentMonthStartDateTime { get; set; }
  }
}
