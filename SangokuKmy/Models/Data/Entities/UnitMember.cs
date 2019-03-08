using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("unit_members")]
  public class UnitMember
  {
    [Key]
    [Column("id")]
    [JsonIgnore]
    public uint Id { get; set; }

    /// <summary>
    /// 武将データ
    /// </summary>
    [NotMapped]
    [JsonProperty("character")]
    public CharacterForAnonymous Character { get; set; }

    /// <summary>
    /// 武将ID
    /// </summary>
    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    /// <summary>
    /// 部隊ID
    /// </summary>
    [Column("unit_id")]
    [JsonProperty("unitId")]
    public uint UnitId { get; set; }

    /// <summary>
    /// 役職
    /// </summary>
    [Column("post")]
    [JsonIgnore]
    public UnitMemberPostType Post { get; set; }

    /// <summary>
    /// 役職（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("post")]
    public byte ApiPost
    {
      get => (byte)this.Post;
      set => this.Post = (UnitMemberPostType)value;
    }
  }

  public enum UnitMemberPostType : byte
  {
    /// <summary>
    /// 一般
    /// </summary>
    Normal = 1,

    /// <summary>
    /// 部隊長
    /// </summary>
    Leader = 2,

    /// <summary>
    /// ヘルパ（集合担当政務官用）
    /// </summary>
    Helper = 3,
  }
}
