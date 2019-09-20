using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("historical_characters")]
  public class HistoricalCharacter
  {
    [Key]
    [Column("id")]
    [JsonIgnore]
    public uint Id { get; set; }

    /// <summary>
    /// 記録のID
    /// </summary>
    [Column("history_id")]
    [JsonIgnore]
    public uint HistoryId { get; set; }

    /// <summary>
    /// 名前
    /// </summary>
    [Column("name", TypeName = "varchar(64)")]
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    /// アイコン
    /// </summary>
    [NotMapped]
    [JsonProperty("mainIcon")]
    public HistoricalCharacterIcon Icon { get; set; }

    /// <summary>
    /// 出身
    /// </summary>
    [Column("from")]
    [JsonIgnore]
    public CharacterFrom From { get; set; }

    /// <summary>
    /// 出身（API用）
    /// </summary>
    [NotMapped]
    [JsonProperty("from")]
    public int ApiFron
    {
      get => (int)this.From;
      set => this.From = (CharacterFrom)value;
    }

    /// <summary>
    /// 国のID
    /// </summary>
    [Column("country_id")]
    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    /// <summary>
    /// 役職の種類
    /// </summary>
    [Column("post_type")]
    [JsonIgnore]
    public CountryPostType Type { get; set; }

    /// <summary>
    /// 役職の種類（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("postType")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (CountryPostType)value;
    }

    /// <summary>
    /// 武力
    /// </summary>
    [Column("strong")]
    [JsonProperty("strong")]
    public short Strong { get; set; }

    /// <summary>
    /// 知力
    /// </summary>
    [Column("intellect")]
    [JsonProperty("intellect")]
    public short Intellect { get; set; }

    /// <summary>
    /// 統率力
    /// </summary>
    [Column("leadership")]
    [JsonProperty("leadership")]
    public short Leadership { get; set; }

    /// <summary>
    /// 人望
    /// </summary>
    [Column("popularity")]
    [JsonProperty("popularity")]
    public short Popularity { get; set; }

    /// <summary>
    /// AIの種類
    /// </summary>
    [Column("ai_type")]
    [JsonIgnore]
    public CharacterAiType AiType { get; set; }

    /// <summary>
    /// AIの種類（API出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("aiType")]
    public int ApiAiType { get; set; }

    public static HistoricalCharacter FromCharacter(Character chara)
    {
      return new HistoricalCharacter
      {
        CountryId = chara.CountryId,
        Name = chara.Name,
        From = chara.From,
        Strong = chara.Strong,
        Intellect = chara.Intellect,
        Leadership = chara.Leadership,
        Popularity = chara.Popularity,
        AiType = chara.AiType,
      };
    }
  }
}
