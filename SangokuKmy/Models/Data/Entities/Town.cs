using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("town")]
  public class Town
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 特化
    /// </summary>
    [Column("type")]
    [JsonIgnore]
    public TownType Type { get; set; }

    /// <summary>
    /// 特化（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("type")]
    public byte ApiType
    {
      get => (byte)this.Type;
      set => this.Type = (TownType)value;
    }

    /// <summary>
    /// 支配国のID
    /// </summary>
    [Column("country_id")]
    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    /// <summary>
    /// 名前
    /// </summary>
    [Column("name", TypeName = "varchar(64)")]
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    /// 都市の位置X
    /// </summary>
    [Column("x")]
    [JsonProperty("x")]
    public short X { get; set; }

    /// <summary>
    /// 都市の位置Y
    /// </summary>
    [Column("y")]
    [JsonProperty("y")]
    public short Y { get; set; }

    /// <summary>
    /// 人口
    /// </summary>
    [Column("people")]
    [JsonProperty("people")]
    public int People { get; set; }

    /// <summary>
    /// 農業
    /// </summary>
    [Column("agriculture")]
    [JsonProperty("agriculture")]
    public int Agriculture { get; set; }

    /// <summary>
    /// 農業最大値
    /// </summary>
    [Column("agriculture_max")]
    [JsonProperty("agricultureMax")]
    public int AgricultureMax { get; set; }

    /// <summary>
    /// 商業
    /// </summary>
    [Column("commercial")]
    [JsonProperty("commercial")]
    public int Commercial { get; set; }

    /// <summary>
    /// 商業最大値
    /// </summary>
    [Column("commercial_max")]
    [JsonProperty("commercialMax")]
    public int CommercialMax { get; set; }

    /// <summary>
    /// 技術
    /// </summary>
    [Column("technology")]
    [JsonProperty("technology")]
    public int Technology { get; set; }

    /// <summary>
    /// 技術最大値
    /// </summary>
    [Column("technology_max")]
    [JsonProperty("technologyMax")]
    public int TechnologyMax { get; set; }

    /// <summary>
    /// 城壁
    /// </summary>
    [Column("wall")]
    [JsonProperty("wall")]
    public int Wall { get; set; }

    /// <summary>
    /// 城壁最大値
    /// </summary>
    [Column("wall_max")]
    [JsonProperty("wallMax")]
    public int WallMax { get; set; }

    /// <summary>
    /// 守兵
    /// </summary>
    [Column("wallguard")]
    [JsonProperty("wallguard")]
    public int WallGuard { get; set; }

    /// <summary>
    /// 守兵最大値
    /// </summary>
    [Column("wallguard_max")]
    [JsonProperty("wallguardMax")]
    public int WallGuardMax { get; set; }

    /// <summary>
    /// 民忠
    /// </summary>
    [Column("security")]
    [JsonProperty("security")]
    public short Security { get; set; }

    /// <summary>
    /// 相場（DB保存用）
    /// </summary>
    [Column("rice_price")]
    [JsonIgnore]
    public int IntRicePrice { get; set; }

    /// <summary>
    /// 相場
    /// </summary>
    [NotMapped]
    [JsonProperty("ricePrice")]
    public float RicePrice
    {
      get => this.IntRicePrice / 1000000.0f;
      set => this.IntRicePrice = (int)(value * 1000000);
    }
  }

  public enum TownType : byte
  {
    /// <summary>
    /// 農業都市
    /// </summary>
    Agriculture = 1,

    /// <summary>
    /// 商業都市
    /// </summary>
    Commercial = 2,

    /// <summary>
    /// 城塞都市
    /// </summary>
    Fortress = 3,

    /// <summary>
    /// 大都市
    /// </summary>
    Large = 4,
  }
}
