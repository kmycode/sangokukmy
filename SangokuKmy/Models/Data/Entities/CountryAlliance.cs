using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("country_alliances")]
  public class CountryAlliance
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 同盟の状態
    /// </summary>
    [Column("status")]
    [JsonIgnore]
    public CountryAllianceStatus Status { get; set; }

    /// <summary>
    /// 同盟の状態（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("status")]
    public short ApiStatus
    {
      get => (short)this.Status;
      set => this.Status = (CountryAllianceStatus)value;
    }

    /// <summary>
    /// 同盟内容変更の対象ID（有効中の条約では0、協議中の内容を含むデータでは有効中の条約IDが設定される）
    /// </summary>
    [Column("change_target_id")]
    [JsonProperty("changeTargetId")]
    public uint ChangeTargetId { get; set; }

    /// <summary>
    /// 同盟をリクエストした国のID
    /// </summary>
    [Column("requested_country_id")]
    [JsonProperty("requestedCountryId")]
    public uint RequestedCountryId { get; set; }

    /// <summary>
    /// 同盟をリクエストされた国のID
    /// </summary>
    [Column("insisted_country_id")]
    [JsonProperty("insistedCountryId")]
    public uint InsistedCountryId { get; set; }

    /// <summary>
    /// 同盟を公開するか
    /// </summary>
    [Column("is_public")]
    [JsonProperty("isPublic")]
    public bool IsPublic { get; set; }

    /// <summary>
    /// 破棄猶予
    /// </summary>
    [Column("breaking_delay")]
    [JsonProperty("breakingDelay")]
    public int BreakingDelay { get; set; }

    /// <summary>
    /// 自由記述欄
    /// </summary>
    [Column("memo")]
    [JsonProperty("memo")]
    public string Memo { get; set; }
  }

  public enum CountryAllianceStatus : short
  {
    None = 0,

    /// <summary>
    /// 片方の承認待ち
    /// </summary>
    Requesting = 1,

    /// <summary>
    /// 却下された
    /// </summary>
    Dismissed = 2,

    /// <summary>
    /// 同盟が有効中
    /// </summary>
    Available = 3,

    /// <summary>
    /// 破棄猶予中
    /// </summary>
    InBreaking = 4,

    /// <summary>
    /// 破棄済
    /// </summary>
    Broken = 5,

    /// <summary>
    /// 条項修正の承認待ち
    /// </summary>
    ChangeRequesting = 6,

    /// <summary>
    /// 条約修正協議中の内容
    /// </summary>
    ChangeRequestingValue = 7,

    /// <summary>
    /// 同盟の条約が変更された（DB上ではAvailableに書き換えられる）
    /// </summary>
    Changed = 8,
  }
}
