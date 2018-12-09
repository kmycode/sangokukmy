using Newtonsoft.Json;
using SangokuKmy.Models.Common.Definitions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  /// <summary>
  /// 認証結果
  /// </summary>
  [Table("authentication_data")]
  public class AuthenticationData
  {
    [Key]
    [Column("id")]
    [JsonIgnore]
    public uint Id { get; set; }

    /// <summary>
    /// アクセストークン
    /// </summary>
    [Column("access_token", TypeName = "varchar(256)")]
    [JsonProperty("accessToken")]
    public string AccessToken { get; set; }

    /// <summary>
    /// ユーザのID
    /// </summary>
    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    /// <summary>
    /// 有効期限
    /// </summary>
    [Column("expiration_time")]
    [JsonIgnore]
    public DateTime ExpirationTime { get; set; }

    /// <summary>
    /// 有効期限（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("expirationTime")]
    public ApiDateTime ApiExpirationTime
    {
      get => ApiDateTime.FromDateTime(this.ExpirationTime);
      set => this.ExpirationTime = value.ToDateTime();
    }

    /// <summary>
    /// スコープ
    /// </summary>
    [Column("scope")]
    [JsonIgnore]
    public Scope Scope { get; set; }
  }
}
