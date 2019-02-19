using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("historical_countries")]
  public class HistoricalCountry
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
    /// 国のID
    /// </summary>
    [Column("country_id")]
    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    /// <summary>
    /// 国の名前
    /// </summary>
    [Column("name", TypeName = "varchar(64)")]
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    /// 色のID
    /// </summary>
    [Column("country_color_id")]
    [JsonProperty("colorId")]
    public short CountryColorId { get; set; }

    /// <summary>
    /// 建国された年月（DB保存用）
    /// </summary>
    [Column("established")]
    [JsonIgnore]
    public int IntEstablished { get; set; }

    /// <summary>
    /// 建国された年月
    /// </summary>
    [NotMapped]
    [JsonProperty("established")]
    public GameDateTime Established
    {
      get => GameDateTime.FromInt(this.IntEstablished);
      set => this.IntEstablished = value.ToInt();
    }

    /// <summary>
    /// この国はすでに滅亡したか
    /// </summary>
    [Column("has_overthrown")]
    [JsonProperty("hasOverthrown")]
    public bool HasOverthrown { get; set; }

    /// <summary>
    /// 滅亡した、ゲーム内の年月（DB用）
    /// </summary>
    [Column("overthrown_game_date")]
    [JsonIgnore]
    public int IntOverthrownGameDate { get; set; }

    /// <summary>
    /// 滅亡した、ゲーム内の年月
    /// </summary>
    [NotMapped]
    [JsonProperty("overthrownGameDate")]
    public GameDateTime OverthrownGameDate
    {
      get => GameDateTime.FromInt(this.IntOverthrownGameDate);
      set => this.IntOverthrownGameDate = value.ToInt();
    }

    public static HistoricalCountry FromCountry(Country country)
    {
      return new HistoricalCountry
      {
        CountryId = country.Id,
        CountryColorId = country.CountryColorId,
        Established = country.Established,
        OverthrownGameDate = country.OverthrownGameDate,
        Name = country.Name,
        HasOverthrown = country.HasOverthrown,
      };
    }
  }
}
