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
  }
}
