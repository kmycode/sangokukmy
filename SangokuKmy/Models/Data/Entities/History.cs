using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("histories")]
  public class History
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

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
    /// 統一時刻
    /// </summary>
    [Column("unified_date_time")]
    [JsonIgnore]
    public DateTime UnifiedDateTime { get; set; }

    /// <summary>
    /// 統一時刻（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("unifiedDateTime")]
    public ApiDateTime ApiUnifiedDateTime
    {
      get => ApiDateTime.FromDateTime(this.UnifiedDateTime);
      set => this.UnifiedDateTime = value.ToDateTime();
    }

    /// <summary>
    /// 統一国君主のメッセージ
    /// </summary>
    [Column("unified_country_message")]
    [JsonProperty("unifiedCountryMessage")]
    public string UnifiedCountryMessage { get; set; }

    /// <summary>
    /// 武将一覧
    /// </summary>
    [NotMapped]
    [JsonProperty("characters")]
    public IEnumerable<HistoricalCharacter> Characters { get; set; }

    /// <summary>
    /// 国一覧
    /// </summary>
    [NotMapped]
    [JsonProperty("countries")]
    public IEnumerable<HistoricalCountry> Countries { get; set; }

    [NotMapped]
    [JsonProperty("maplogs")]
    public IEnumerable<HistoricalMapLog> MapLogs { get; set; }

    [NotMapped]
    [JsonProperty("towns")]
    public IEnumerable<HistoricalTown> Towns { get; set; }
  }
}
