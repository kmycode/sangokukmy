using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("scouted_town")]
  public class ScoutedTown : TownBase
  {
    public static ScoutedTown From(Town town)
    {
      var t = new ScoutedTown
      {
        ScoutedTownId = town.Id,
        Type = town.Type,
        CountryId = town.CountryId,
        Name = town.Name,
        X = town.X,
        Y = town.Y,
        People = town.People,
        Agriculture = town.Agriculture,
        AgricultureMax = town.AgricultureMax,
        Commercial = town.Commercial,
        CommercialMax = town.CommercialMax,
        Technology = town.Technology,
        TechnologyMax = town.TechnologyMax,
        Wall = town.Wall,
        WallMax = town.WallMax,
        WallGuard = town.WallGuard,
        WallGuardMax = town.WallGuardMax,
        Security = town.Security,
        IntRicePrice = town.IntRicePrice,
      };
      return t;
    }

    /// <summary>
    /// 偵察した都市のID
    /// </summary>
    [Column("scouted_town_id")]
    [JsonProperty("scoutedTownId")]
    public uint ScoutedTownId { get; set; }

    /// <summary>
    /// 偵察した国のID
    /// </summary>
    [Column("scouted_country_id")]
    [JsonProperty("scoutedCountryId")]
    public uint ScoutedCountryId { get; set; }

    /// <summary>
    /// 偵察した武将のID
    /// </summary>
    [Column("scouted_character_id")]
    [JsonProperty("scoutedCharacterId")]
    public uint ScoutedCharacterId { get; set; }

    /// <summary>
    /// 偵察方法
    /// </summary>
    [Column("scout_method")]
    [JsonIgnore]
    public ScoutMethod ScoutMethod { get; set; }

    /// <summary>
    /// 偵察方法（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("scoutMethod")]
    public short ApiScoutMethod
    {
      get => (short)this.ScoutMethod;
      set => this.ScoutMethod = (ScoutMethod)value;
    }

    /// <summary>
    /// 偵察した年月
    /// </summary>
    [NotMapped]
    [JsonProperty("scoutedGameDateTime")]
    public GameDateTime ScoutedDateTime { get; set; }

    /// <summary>
    /// 偵察した年月（DB保存用）
    /// </summary>
    [Column("scouted_game_date_time")]
    [JsonIgnore]
    public int IntScoutedDateTime
    {
      get => this.ScoutedDateTime.ToInt();
      set => this.ScoutedDateTime = GameDateTime.FromInt(value);
    }

    /// <summary>
    /// 都市にいる武将
    /// </summary>
    [NotMapped]
    [JsonProperty("characters")]
    public IEnumerable<CharacterForAnonymous> Characters { get; set; }

    /// <summary>
    /// 都市を守備する武将
    /// </summary>
    [NotMapped]
    [JsonProperty("defenders")]
    public IEnumerable<CharacterForAnonymous> Defenders { get; set; }
  }

  public enum ScoutMethod : short
  {
    /// <summary>
    /// この都市は諜報されていない（クライアント側で識別する用）
    /// </summary>
    NotScouted = 0,

    /// <summary>
    /// 手動
    /// </summary>
    Manual = 1,

    /// <summary>
    /// 密偵
    /// </summary>
    Scouter = 2,
  }
}
