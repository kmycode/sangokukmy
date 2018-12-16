using System;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("scouted_town")]
  public class ScoutedTown : TownBase
  {
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
  }

  public enum ScoutMethod : short
  {
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
