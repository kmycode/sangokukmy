using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("country_posts")]
  public class CountryPost
  {
    [Key]
    [Column("id")]
    [JsonIgnore]
    public uint Id { get; set; }

    /// <summary>
    /// 国のID
    /// </summary>
    [Column("country_id")]
    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    /// <summary>
    /// 役職の種類
    /// </summary>
    [Column("type")]
    [JsonIgnore]
    public CountryPostType Type { get; set; }

    /// <summary>
    /// 役職の種類（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (CountryPostType)value;
    }

    /// <summary>
    /// 任命された武将のID
    /// </summary>
    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    [NotMapped]
    [JsonProperty("character")]
    public CharacterForAnonymous Character { get; set; }
  }

  public enum CountryPostType : short
  {
    /// <summary>
    /// 君主
    /// </summary>
    Monarch = 1,

    /// <summary>
    /// 軍師
    /// </summary>
    Warrior = 2,

    /// <summary>
    /// 大将軍
    /// </summary>
    GrandGeneral = 3,

    /// <summary>
    /// 騎兵将軍
    /// </summary>
    CavalryGeneral = 4,

    /// <summary>
    /// 弓兵将軍
    /// </summary>
    BowmanGeneral = 5,

    /// <summary>
    /// 護衛将軍
    /// </summary>
    GuardGeneral = 6,

    /// <summary>
    /// 将軍
    /// </summary>
    General = 7,
  }
}
