using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;

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
    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    /// <summary>
    /// メッセージを書いた人のID
    /// </summary>
    [Column("writer_character_id")]
    [JsonIgnore]
    public uint WriterCharacterId { get; set; }

    /// <summary>
    /// メッセージを書いた人
    /// </summary>
    [NotMapped]
    [JsonProperty("writerCharacterName")]
    public string WriterCharacterName { get; set; }

    /// <summary>
    /// メッセージを書いた人の役職
    /// </summary>
    [Column("writer_post")]
    [JsonIgnore]
    public CountryPostType WriterPost { get; set; }

    /// <summary>
    /// メッセージを書いた人の役職（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("writerPost")]
    public short ApiWriterPost
    {
      get => (short)this.WriterPost;
      set => this.WriterPost = (CountryPostType)value;
    }

    /// <summary>
    /// メッセージを書いた人のアイコンID
    /// </summary>
    [Column("writer_icon")]
    [JsonIgnore]
    public uint WriterIconId { get; set; }

    /// <summary>
    /// メッセージを書いた人のアイコン
    /// </summary>
    [NotMapped]
    [JsonProperty("writerIcon")]
    public CharacterIcon WriterIcon { get; set; }

    /// <summary>
    /// メッセージの種類
    /// </summary>
    [Column("type")]
    [JsonIgnore]
    public CountryMessageType Type { get; set; }

    /// <summary>
    /// メッセージの種類（API出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("type")]
    public byte ApiType
    {
      get => (byte)this.Type;
      set => this.Type = (CountryMessageType)value;
    }

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

    /// <summary>
    /// 統一記録
    /// </summary>
    Unified = 3,
  }
}
