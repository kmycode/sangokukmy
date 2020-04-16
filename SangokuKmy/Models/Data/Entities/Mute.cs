using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("mutes")]
  public class Mute
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// ミューとした武将
    /// </summary>
    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    /// <summary>
    /// ミュートの種類
    /// </summary>
    [Column("type")]
    [JsonIgnore]
    public MuteType Type { get; set; }

    /// <summary>
    /// ミュートの種類（API出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (MuteType)value;
    }

    /// <summary>
    /// 対象武将（もしあれば）
    /// </summary>
    [Column("target_character_id")]
    [JsonProperty("targetCharacterId")]
    public uint TargetCharacterId { get; set; }

    /// <summary>
    /// 対象メッセージID（もしあれば）
    /// </summary>
    [Column("chat_message_id")]
    [JsonProperty("chatMessageId")]
    public uint ChatMessageId { get; set; }

    /// <summary>
    /// 対象スレッドBBSID（もしあれば）
    /// </summary>
    [Column("thread_bbs_item_id")]
    [JsonProperty("threadBbsItemId")]
    public uint ThreadBbsItemId { get; set; }
  }

  public enum MuteType : short
  {
    None = 0,
    Muted = 1,
    Reported = 2,
  }
}
