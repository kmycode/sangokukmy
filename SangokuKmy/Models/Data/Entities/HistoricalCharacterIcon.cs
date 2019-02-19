using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("historical_character_icons")]
  public class HistoricalCharacterIcon
  {
    [Key]
    [Column("id")]
    [JsonIgnore]
    public uint Id { get; set; }

    /// <summary>
    /// 武将のID
    /// </summary>
    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

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

    public static HistoricalCharacterIcon FromCharacterIcon(CharacterIcon icon)
    {
      return new HistoricalCharacterIcon
      {
        CharacterId = icon.CharacterId,
        Type = icon.Type,
        FileName = icon.FileName,
      };
    }
  }
}
