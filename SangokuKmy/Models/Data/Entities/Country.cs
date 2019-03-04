using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("countries")]
  public class Country
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

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
    /// 首都
    /// </summary>
    [Column("capital_town_id")]
    [JsonProperty("capitalTownId")]
    public uint CapitalTownId { get; set; }

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

    /// <summary>
    /// 最後の金収入
    /// </summary>
    [Column("last_money_incomes")]
    [JsonProperty("lastMoneyIncomes")]
    public int LastMoneyIncomes { get; set; }

    /// <summary>
    /// 最後の米収入
    /// </summary>
    [Column("last_rice_incomes")]
    [JsonProperty("lastRiceIncomes")]
    public int LastRiceIncomes { get; set; }

    /// <summary>
    /// 国庫の金
    /// </summary>
    [Column("safe_money")]
    [JsonProperty("safeMoney")]
    public int SafeMoney { get; set; }
  }
}
