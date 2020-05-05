using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("accounts")]
  public class Account
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 今期の武将ID
    /// </summary>
    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    /// <summary>
    /// ログインに使うID
    /// </summary>
    [Column("alias_id", TypeName = "varchar(32)")]
    [JsonProperty("aliasId")]
    public string AliasId { get; set; }

    /// <summary>
    /// パスワードのハッシュ
    /// </summary>
    [Column("password_hash", TypeName = "varchar(256)")]
    [JsonIgnore]
    public string PasswordHash { get; set; }

    /// <summary>
    /// パスワード（API用）
    /// </summary>
    [NotMapped]
    [JsonProperty("password")]
    public string Password { get; set; }

    /// <summary>
    /// 名前
    /// </summary>
    [Column("name", TypeName = "varchar(64)")]
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    /// パスワードを設定する。平文のパラメータを指定し、実際はハッシュに変換されたパスワードが保存される
    /// </summary>
    /// <param name="password">パスワード</param>
    public void SetPassword(string password)
    {
      this.PasswordHash = Character.GeneratePasswordHash(password);
    }

    /// <summary>
    /// ログインできるか確認する
    /// </summary>
    /// <returns>指定したログイン情報でログイン可能であるか</returns>
    /// <param name="password">パスワード</param>
    public bool TryLogin(string password)
    {
      return this.PasswordHash == Character.GeneratePasswordHash(password);
    }
  }
}
