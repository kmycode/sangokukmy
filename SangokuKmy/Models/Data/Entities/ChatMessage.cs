using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("char_messages")]
  public class ChatMessage
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 武将のID
    /// </summary>
    [Column("character_id")]
    [JsonIgnore]
    public uint CharacterId { get; set; }

    /// <summary>
    /// 武将の情報
    /// </summary>
    [NotMapped]
    [JsonProperty("character")]
    public CharacterChatData Character { get; set; }

    /// <summary>
    /// 使用する武将アイコンのID
    /// </summary>
    [Column("character_icon_id")]
    [JsonProperty("characterIconId")]
    public uint CharacterIconId { get; set; }

    /// <summary>
    /// 使用する武将アイコン
    /// </summary>
    [NotMapped]
    [JsonProperty("characterIcon")]
    public CharacterIcon CharacterIcon { get; set; }

    /// <summary>
    /// 送信者の国ID
    /// </summary>
    [Column("character_country_id")]
    [JsonProperty("characterCountryId")]
    public uint CharacterCountryId { get; set; }

    /// <summary>
    /// 受信者の情報
    /// </summary>
    [NotMapped]
    [JsonProperty("receiverName")]
    public string ReceiverName { get; set; }

    /// <summary>
    /// 発言の種類
    /// </summary>
    [Column("type")]
    [JsonIgnore]
    public ChatMessageType Type { get; set; }

    /// <summary>
    /// 発言の種類（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (ChatMessageType)value;
    }

    /// <summary>
    /// 発言の種類に補足するデータ
    /// </summary>
    [Column("type_data")]
    [JsonProperty("typeData")]
    public uint TypeData { get; set; }

    /// <summary>
    /// 発言の種類に補足するデータ２
    /// </summary>
    [Column("type_data_2")]
    [JsonProperty("typeData2")]
    public uint TypeData2 { get; set; }

    /// <summary>
    /// 発言内容
    /// </summary>
    [Column("message")]
    [JsonProperty("message")]
    public string Message { get; set; }

    /// <summary>
    /// 投稿時刻
    /// </summary>
    [Column("posted")]
    [JsonIgnore]
    public DateTime Posted { get; set; }

    /// <summary>
    /// 投稿時刻（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("posted")]
    public ApiDateTime ApiPosted
    {
      get => ApiDateTime.FromDateTime(this.Posted);
      set => this.Posted = value.ToDateTime();
    }
  }

  public enum ChatMessageType : short
  {
    /// <summary>
    /// 国宛（補足：国ID）
    /// </summary>
    SelfCountry = 1,

    /// <summary>
    /// 他国宛（補足：自分国ID、相手国ID）
    /// </summary>
    OtherCountry = 2,

    /// <summary>
    /// 個宛（補足：相手武将ID）
    /// </summary>
    Private = 3,

    /// <summary>
    /// 部隊宛（補足：部隊ID）
    /// </summary>
    Unit = 4,

    /// <summary>
    /// 都市宛（補足：都市ID）
    /// </summary>
    Town = 5,

    /// <summary>
    /// 全国宛
    /// </summary>
    Global = 6,

    /// <summary>
    /// 専用BBS（簡易）
    /// </summary>
    SimpleBbs = 7,

    /// <summary>
    /// 登用（補足は個宛と同じ）
    /// </summary>
    Promotion = 8,

    /// <summary>
    /// 登用承諾
    /// </summary>
    PromotionAccepted = 9,

    /// <summary>
    /// 登用拒否
    /// </summary>
    PromotionRefused = 10,
  }
}
