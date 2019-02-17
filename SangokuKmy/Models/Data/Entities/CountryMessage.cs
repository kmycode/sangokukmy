using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("country_messages")]
  public class CountryMessage
  {
    [Key]
    [Column("id")]
    [JsonIgnore]
    public uint Id { get; set; }

    /// <summary>
    /// 国のID
    /// </summary>
    [Column("country_id")]
    [JsonIgnore]
    public uint CountryId { get; set; }

    /// <summary>
    /// メッセージの種類
    /// </summary>
    [Column("type")]
    [JsonIgnore]
    public CountryMessageType Type { get; set; }

    /// <summary>
    /// メッセージ
    /// </summary>
    [Column("message")]
    [JsonProperty("message")]
    public string Message { get; set; }
  }

  public enum CountryMessageType : byte
  {
    /// <summary>
    /// 指令
    /// </summary>
    Commanders = 1,

    /// <summary>
    /// 新規登録者への勧誘
    /// </summary>
    Solicitation = 2,
  }
}
