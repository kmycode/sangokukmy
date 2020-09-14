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
    [JsonProperty("id")]
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
    /// 解任であるか
    /// </summary>
    [NotMapped]
    [JsonProperty("isUnAppointed")]
    public bool IsUnAppointed { get; set; }

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

    /// <summary>
    /// 政務官長
    /// </summary>
    SecretaryLeader = 10,

    /// <summary>
    /// 外交官
    /// </summary>
    Diplomat = 11,

    /// <summary>
    /// 金庫番
    /// </summary>
    Safeguard = 12,

    /// <summary>
    /// 政策番
    /// </summary>
    PolicyLeader = 13,

    /// <summary>
    /// 司令官
    /// </summary>
    Commander = 14,

    RoleGroup_1 = 15,
    RoleGroup_2 = 16,
    RoleGroup_3 = 17,
    RoleGroup_4 = 18,
    RoleGroup_5 = 19,
    RoleGroup_6 = 20,
    RoleGroup_7 = 21,
    RoleGroup_8 = 22,
    RoleGroup_9 = 23,
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

    public static bool CanMultiple(this CountryPostType type)
    {
      return type == CountryPostType.Builder || type == CountryPostType.Diplomat || type == CountryPostType.PolicyLeader ||
        type == CountryPostType.Safeguard || type == CountryPostType.SecretaryLeader || type == CountryPostType.Warrior || type == CountryPostType.Commander ||
        type.IsRoleGroup();
    }
    public static bool IsRoleGroup(this CountryPostType type)
    {
      return (short)type >= (short)CountryPostType.RoleGroup_1 && (short)type <= (short)CountryPostType.RoleGroup_9;
    }
    public static int BattleOrder(this CountryPostType type)
    {
      if (type == CountryPostType.GrandGeneral)
      {
        return 1;
      }
      if (type == CountryPostType.CavalryGeneral)
      {
        return 2;
      }
      if (type == CountryPostType.GuardGeneral)
      {
        return 3;
      }
      if (type == CountryPostType.BowmanGeneral)
      {
        return 4;
      }
      if (type == CountryPostType.General)
      {
        return 5;
      }
      return -1;
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
      return type == CountryPostType.Monarch || type == CountryPostType.Warrior || type == CountryPostType.Safeguard;
    }

    /// <summary>
    /// 政務官権限があるか確認する
    /// </summary>
    /// <returns>政務官権限があるか</returns>
    public static bool CanSecretary(this CountryPostType type)
    {
      return type == CountryPostType.Monarch || type == CountryPostType.Warrior || type == CountryPostType.SecretaryLeader;
    }

    /// <summary>
    /// 外交権限があるか確認する
    /// </summary>
    /// <returns>外交権限があるか</returns>
    public static bool CanDiplomacy(this CountryPostType type)
    {
      return type == CountryPostType.Monarch || type == CountryPostType.Warrior || type == CountryPostType.Diplomat;
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
    public static bool CanCountryCommander(this CountryPostType type)
    {
      return type == CountryPostType.Monarch || type == CountryPostType.Warrior || type == CountryPostType.GrandGeneral
        || type == CountryPostType.Commander;
    }

    /// <summary>
    /// 政策権限があるか確認する
    /// </summary>
    /// <returns>政策権限があるか</returns>
    public static bool CanPolicy(this CountryPostType type)
    {
      return type == CountryPostType.Monarch || type == CountryPostType.Warrior || type == CountryPostType.PolicyLeader;
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
