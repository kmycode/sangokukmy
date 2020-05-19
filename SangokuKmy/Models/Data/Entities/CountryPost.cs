using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SangokuKmy.Common;
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
    /// 任命されていない
    /// </summary>
    UnAppointed = 0,

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

    /// <summary>
    /// 君主（一時的に無効）
    /// </summary>
    MonarchDisabled = 8,

    /// <summary>
    /// 建築官
    /// </summary>
    Builder = 9,
  }

  public static class CountryPostExtentions
  {
    /// <summary>
    /// 最高位の役職を取得する
    /// </summary>
    /// <returns>最高位の役職</returns>
    public static Optional<CountryPost> GetTopmostPost(this IEnumerable<CountryPost> posts)
    {
      return posts.OrderBy(p => p.ApiType).First().ToOptional();
    }

    /// <summary>
    /// 任命権限があるか確認する
    /// </summary>
    /// <returns>任命権限があるか</returns>
    public static bool CanAppoint(this CountryPostType type)
    {
      return type == CountryPostType.Monarch || type == CountryPostType.Warrior;
    }
    public static bool CanPunishment(this CountryPostType type)
    {
      return type == CountryPostType.Monarch;
    }

    /// <summary>
    /// 国庫権限があるか確認する
    /// </summary>
    /// <returns>国庫権限があるか</returns>
    public static bool CanSafe(this CountryPostType type)
    {
      return type == CountryPostType.Monarch || type == CountryPostType.Warrior;
    }

    /// <summary>
    /// 政務官権限があるか確認する
    /// </summary>
    /// <returns>政務官権限があるか</returns>
    public static bool CanSecretary(this CountryPostType type)
    {
      return type == CountryPostType.Monarch || type == CountryPostType.Warrior;
    }

    /// <summary>
    /// 外交権限があるか確認する
    /// </summary>
    /// <returns>外交権限があるか</returns>
    public static bool CanDiplomacy(this CountryPostType type)
    {
      return type == CountryPostType.Monarch || type == CountryPostType.Warrior;
    }

    /// <summary>
    /// 国の設定をする権限があるか確認する
    /// </summary>
    /// <returns>設定権限があるか</returns>
    public static bool CanCountrySetting(this CountryPostType type)
    {
      return type == CountryPostType.Monarch || type == CountryPostType.Warrior || type == CountryPostType.GrandGeneral;
    }
    public static bool CanCountryUnifiedMessage(this CountryPostType type)
    {
      return type == CountryPostType.Monarch;
    }
    public static bool CanCommandComment(this CountryPostType type)
    {
      return type == CountryPostType.Monarch || type == CountryPostType.Warrior;
    }

    /// <summary>
    /// 政策権限があるか確認する
    /// </summary>
    /// <returns>政策権限があるか</returns>
    public static bool CanPolicy(this CountryPostType type)
    {
      return type == CountryPostType.Monarch || type == CountryPostType.Warrior;
    }

    /// <summary>
    /// 国の設定をする権限があるか確認する
    /// </summary>
    /// <returns>設定権限があるか</returns>
    public static bool CanCountrySetting(this IEnumerable<CountryPost> posts)
    {
      return posts.Any(p => p.Type.CanCountrySetting());
    }
    public static bool CanCountryUnifiedMessage(this IEnumerable<CountryPost> posts)
    {
      return posts.Any(p => p.Type.CanCountryUnifiedMessage());
    }

    /// <summary>
    /// 国の設定をする権限があるか確認する
    /// </summary>
    /// <returns>設定権限があるか</returns>
    public static bool CanCountrySettingExceptForCommands(this CountryPostType type)
    {
      return type == CountryPostType.Monarch || type == CountryPostType.Warrior;
    }
    public static bool CanCountrySettingExceptForCommands(this IEnumerable<CountryPost> posts)
    {
      return posts.Any(p => p.Type.CanCountrySettingExceptForCommands());
    }

    /// <summary>
    /// 建築物を建てる権限があるか確認する
    /// </summary>
    /// <returns>建築権限があるか</returns>
    public static bool CanBuildTownSubBuildings(this CountryPostType type)
    {
      return type == CountryPostType.Monarch || type == CountryPostType.Warrior || type == CountryPostType.GrandGeneral || type == CountryPostType.Builder;
    }
    public static bool CanBuildTownSubBuildings(this IEnumerable<CountryPost> posts)
    {
      return posts.Any(p => p.Type.CanBuildTownSubBuildings());
    }
  }
}
