using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SangokuKmy.Common;

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
    /// このアイコンは有効であるか（削除してもDB内では削除されない）
    /// </summary>
    [Column("is_available")]
    [JsonProperty("isAvailable")]
    public bool IsAvailable { get; set; }

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

  public static class CharacterIconExtensions
  {
    public static Optional<CharacterIcon> GetMainOrFirst(this IEnumerable<CharacterIcon> icons)
    {
      var main = icons.SingleOrDefault(i => i.IsMain);
      if (main != null)
      {
        return main.ToOptional();
      }
      return icons.FirstOrDefault().ToOptional();
    }
  }
}
