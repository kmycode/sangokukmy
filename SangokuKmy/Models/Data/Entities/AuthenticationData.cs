using SangokuKmy.Models.Common.Definitions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

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
    public uint Id { get; set; }

    /// <summary>
    /// アクセストークン
    /// </summary>
    [Column("access_token", TypeName = "varchar(256)")]
    public string AccessToken { get; set; }

    /// <summary>
    /// ユーザのID
    /// </summary>
    [Column("character_id")]
    public uint CharacterId { get; set; }

    /// <summary>
    /// 有効期限
    /// </summary>
    [Column("expiration_time")]
    public DateTime ExpirationTime { get; set; }

    /// <summary>
    /// スコープ
    /// </summary>
    [Column("scope")]
    public Scope Scope { get; set; }
  }
}
