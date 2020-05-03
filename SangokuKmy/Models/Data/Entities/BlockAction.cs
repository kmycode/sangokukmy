using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("block_actions")]
  public class BlockAction
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 武将ID
    /// </summary>
    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    /// <summary>
    /// 制限する行動の種類
    /// </summary>
    [Column("type")]
    [JsonIgnore]
    public BlockActionType Type { get; set; }

    /// <summary>
    /// 制限する行動の種類（API出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (BlockActionType)value;
    }

    /// <summary>
    /// 制限の有効期限
    /// </summary>
    [Column("expiry_date")]
    [JsonIgnore]
    public DateTime ExpiryDate { get; set; }

    /// <summary>
    /// 制限の有効期限（API出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("expiryDate")]
    public ApiDateTime ApiExpiryDate
    {
      get => ApiDateTime.FromDateTime(this.ExpiryDate);
      set => this.ExpiryDate = value.ToDateTime();
    }
  }

  public enum BlockActionType : short
  {
    None = 0,

    /// <summary>
    /// 報告禁止
    /// </summary>
    StopReporting = 1,

    /// <summary>
    /// 全国宛禁止
    /// </summary>
    StopGlobalChat = 2,

    /// <summary>
    /// 自国宛禁止
    /// </summary>
    StopCountryChat = 3,

    /// <summary>
    /// コマンド実行禁止（放置削除は進めない）
    /// </summary>
    StopCommandAndDeleteTurn = 4,

    /// <summary>
    /// コマンド実行禁止（放置削除を進める）
    /// </summary>
    StopCommand = 5,
  }
}
