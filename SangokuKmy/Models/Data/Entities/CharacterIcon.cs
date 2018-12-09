using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("character_icons")]
  public class CharacterIcon
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 武将のID
    /// </summary>
    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    /// <summary>
    /// メインで使用するアイコンであるか
    /// </summary>
    [Column("is_main")]
    [JsonProperty("isMain")]
    public bool IsMain { get; set; }

    /// <summary>
    /// アイコンの種類
    /// </summary>
    [Column("type")]
    [JsonIgnore]
    public CharacterIconType Type { get; set; }

    /// <summary>
    /// アイコンの種類（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("type")]
    public byte ApiType
    {
      get => (byte)this.Type;
      set => this.Type = (CharacterIconType)value;
    }

    /// <summary>
    /// アイコンのURI（Gravatar）
    /// </summary>
    [Column("uri")]
    [JsonProperty("uri")]
    public string Uri { get; set; }

    /// <summary>
    /// アイコンのファイル名（デフォルト、アップロードされたアイコン）
    /// </summary>
    [Column("file_name")]
    [JsonProperty("fileName")]
    public string FileName { get; set; }
  }

  public enum CharacterIconType: byte
  {
    /// <summary>
    /// デフォルトのアイコン
    /// </summary>
    Default = 1,

    /// <summary>
    /// アップロードされたアイコン
    /// </summary>
    Uploaded = 2,

    /// <summary>
    /// Gravatar
    /// </summary>
    Gravatar = 3,
  }
}
