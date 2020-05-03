using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("char_message_read")]
  public class ChatMessageRead
  {
    [Key]
    [Column("id")]
    [JsonIgnore]
    public uint Id { get; set; }

    /// <summary>
    /// 武将のID
    /// </summary>
    [Column("character_id")]
    [JsonIgnore]
    public uint CharacterId { get; set; }

    /// <summary>
    /// 最後に読んだ国宛
    /// </summary>
    [Column("last_country_id")]
    [JsonProperty("lastCountryChatMessageId")]
    public uint LastCountryChatMessageId { get; set; }

    /// <summary>
    /// 最後に読んだ全国宛
    /// </summary>
    [Column("last_global_id")]
    [JsonProperty("lastGlobalChatMessageId")]
    public uint LastGlobalChatMessageId { get; set; }

    /// <summary>
    /// 最後に読んだ全国宛
    /// </summary>
    [Column("last_global2_id")]
    [JsonProperty("lastGlobal2ChatMessageId")]
    public uint LastGlobal2ChatMessageId { get; set; }

    /// <summary>
    /// 最後に読んだ登用
    /// </summary>
    [Column("last_promotion_id")]
    [JsonProperty("lastPromotionChatMessageId")]
    public uint LastPromotionChatMessageId { get; set; }
  }
}
