using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("delay_effects")]
  public class DelayEffect
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("character_id")]
    [JsonIgnore]
    public uint CharacterId { get; set; }

    [Column("town_id")]
    [JsonProperty("townId")]
    public uint TownId { get; set; }

    [Column("country_id")]
    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    [Column("type")]
    [JsonProperty("type")]
    public DelayEffectType Type { get; set; }

    [Column("type_data")]
    [JsonProperty("typeData")]
    public int TypeData { get; set; }

    [Column("type_data2")]
    [JsonProperty("typeData2")]
    public int TypeData2 { get; set; }

    [Column("appear_game_date_time")]
    [JsonIgnore]
    public int IntAppearGameDateTime { get; set; }

    [NotMapped]
    [JsonProperty("appearGameDateTime")]
    public GameDateTime AppearGameDateTime
    {
      get => GameDateTime.FromInt(this.IntAppearGameDateTime);
      set => this.IntAppearGameDateTime = value.ToInt();
    }

    [Column("is_queue")]
    [JsonProperty("isQueue")]
    public bool IsQueue { get; set; }
  }

  public enum DelayEffectType : short
  {
    Undefined = 0,

    /// <summary>
    /// 都市投資
    /// </summary>
    TownInvestment = 1,

    /// <summary>
    /// 異民族を敵性化
    /// </summary>
    TerroristEnemy = 2,

    /// <summary>
    /// アイテム生成
    /// </summary>
    GenerateItem = 3,

    /// <summary>
    /// 黄巾出現
    /// </summary>
    AppearKokin = 4,
  }
}
