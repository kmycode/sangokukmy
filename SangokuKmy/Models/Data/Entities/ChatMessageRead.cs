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

    /// <summary>
    /// 最後に読んだ国内会議室
    /// </summary>
    [Column("last_country_bbs_id")]
    [JsonProperty("lastCountryBbsId")]
    public uint LastCountryBbsId { get; set; }

    /// <summary>
    /// 最後に読んだ全国会議室
    /// </summary>
    [Column("last_global_bbs_id")]
    [JsonProperty("lastGlobalBbsId")]
    public uint LastGlobalBbsId { get; set; }

    /// <summary>
    /// 最後に読んだ全体指令
    /// </summary>
    [Column("last_all_commander_id")]
    [JsonProperty("lastAllCommanderId")]
    public uint LastAllCommanderId { get; set; }

    /// <summary>
    /// 最後に読んだ能力指令
    /// </summary>
    [Column("last_attribute_commander_id")]
    [JsonProperty("lastAttributeCommanderId")]
    public uint LastAttributeCommanderId { get; set; }

    /// <summary>
    /// 最後に読んだ出身指令
    /// </summary>
    [Column("last_from_commander_id")]
    [JsonProperty("lastFromCommanderId")]
    public uint LastFromCommanderId { get; set; }

    /// <summary>
    /// 最後に読んだ個人指令
    /// </summary>
    [Column("last_private_commander_id")]
    [JsonProperty("lastPrivateCommanderId")]
    public uint LastPrivateCommanderId { get; set; }
  }
}
