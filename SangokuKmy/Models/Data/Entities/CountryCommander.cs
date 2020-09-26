using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("country_commanders")]
  public class CountryCommander
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("country_id")]
    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    /// <summary>
    /// 指令の分岐条件
    /// </summary>
    [Column("subject")]
    [JsonIgnore]
    public CountryCommanderSubject Subject { get; set; }

    [NotMapped]
    [JsonProperty("subject")]
    public short ApiSubject
    {
      get => (short)this.Subject;
      set => this.Subject = (CountryCommanderSubject)value;
    }

    /// <summary>
    /// 指令の分岐条件の補足データ
    /// </summary>
    [Column("subject_data")]
    [JsonProperty("subjectData")]
    public uint SubjectData { get; set; }

    /// <summary>
    /// 指令の分岐条件の補足データ
    /// </summary>
    [Column("subject_data2")]
    [JsonProperty("subjectData2")]
    public uint SubjectData2 { get; set; }

    /// <summary>
    /// 指令内容
    /// </summary>
    [Column("message")]
    [JsonProperty("message")]
    public string Message { get; set; }

    /// <summary>
    /// 書いた人
    /// </summary>
    [Column("writer_character_id")]
    [JsonProperty("writerCharacterId")]
    public uint WriterCharacterId { get; set; }

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
  }

  public enum CountryCommanderSubject : short
  {
    Unknown = 0,

    /// <summary>
    /// 全員
    /// </summary>
    All = 1,

    /// <summary>
    /// 能力
    /// </summary>
    Attribute = 2,

    /// <summary>
    /// 出身
    /// </summary>
    From = 3,

    /// <summary>
    /// 個人
    /// </summary>
    Private = 4,

    /// <summary>
    /// 他国から来た援軍以外
    /// </summary>
    ExceptForReinforcements = 5,

    /// <summary>
    /// 自国から派遣された援軍を含む
    /// </summary>
    ContainsMyReinforcements = 6,

    /// <summary>
    /// ５と６の両方
    /// </summary>
    OriginalCountryCharacters = 7,

    /// <summary>
    /// 国のポスト
    /// </summary>
    Post = 8,
  }
}
