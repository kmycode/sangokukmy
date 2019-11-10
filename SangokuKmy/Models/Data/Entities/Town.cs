using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  public abstract class TownBase
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
    /// サブ特化
    /// </summary>
    [Column("sub_type")]
    [JsonIgnore]
    public TownType SubType { get; set; }

    /// <summary>
    /// サブ特化（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("subType")]
    public byte ApiSubType
    {
      get => (byte)this.SubType;
      set => this.SubType = (TownType)value;
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
    /// 人口最大
    /// </summary>
    [Column("people_max")]
    [JsonProperty("peopleMax")]
    public int PeopleMax { get; set; }

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

    /// <summary>
    /// 都市施設
    /// </summary>
    [Column("town_building")]
    [JsonIgnore]
    public TownBuilding TownBuilding { get; set; }

    /// <summary>
    /// 都市施設（JSON用）
    /// </summary>
    [NotMapped]
    [JsonProperty("townBuilding")]
    public short ApiTownBuilding { get => (short)this.TownBuilding; set => this.TownBuilding = (TownBuilding)value; }

    /// <summary>
    /// 都市施設の耐久
    /// </summary>
    [Column("town_building_value")]
    [JsonProperty("townBuildingValue")]
    public int TownBuildingValue { get; set; }
  }

  [Table("town")]
  public class Town : TownBase
  {
  }

  public enum TownType : byte
  {
    /// <summary>
    /// 不問（初期化時用）
    /// </summary>
    Any = 0,

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

  public enum TownBuilding : short
  {
    None = 0,

    /// <summary>
    /// 洪水対策
    /// </summary>
    [Obsolete]
    AntiFlood = 1,

    /// <summary>
    /// いなご対策
    /// </summary>
    [Obsolete]
    AntiLocusts = 2,

    /// <summary>
    /// 疫病対策
    /// </summary>
    [Obsolete]
    AntiSicks = 3,

    /// <summary>
    /// 地震対策
    /// </summary>
    [Obsolete]
    AntiEarthquakes = 4,

    /// <summary>
    /// 経済評論
    /// </summary>
    [Obsolete]
    Economy = 5,

    /// <summary>
    /// 武力
    /// </summary>
    TrainStrong = 6,

    /// <summary>
    /// 知力
    /// </summary>
    TrainIntellect = 7,

    /// <summary>
    /// 統率
    /// </summary>
    TrainLeadership = 8,

    /// <summary>
    /// 人望
    /// </summary>
    TrainPopularity = 9,

    /// <summary>
    /// 人口変動を大きく
    /// </summary>
    OpenWall = 10,

    /// <summary>
    /// 修復拠点
    /// </summary>
    RepairWall = 11,

    /// <summary>
    /// 屯所
    /// </summary>
    MilitaryStation = 12,

    /// <summary>
    /// 災害対策拠点
    /// </summary>
    [Obsolete]
    SaveWall = 13,

    /// <summary>
    /// 太守府
    /// </summary>
    [Obsolete]
    ViceroyHouse = 14,

    /// <summary>
    /// 蛮族の家
    /// </summary>
    TerroristHouse = 15,

    /// <summary>
    /// 宮殿
    /// </summary>
    Palace = 16,

    /// <summary>
    /// 住宅
    /// </summary>
    Houses = 17,

    /// <summary>
    /// 鋳金所
    /// </summary>
    Casting = 18,

    /// <summary>
    /// 訓練施設
    /// </summary>
    TrainingBuilding = 19,

    /// <summary>
    /// 陣
    /// </summary>
    Camp = 20,

    /// <summary>
    /// 増築拠点
    /// </summary>
    Extension = 21,

    /// <summary>
    /// 数寄屋
    /// </summary>
    Sukiya = 22,

    /// <summary>
    /// 小学
    /// </summary>
    School = 23,
  }

  public static class TownExtensions
  {
    public static IEnumerable<TownBase> GetAroundTowns(this IEnumerable<TownBase> towns, TownBase town)
    {
      return towns.Where(t => t.IsNextToTown(town));
    }

    public static IEnumerable<uint> GetAroundCountries(this IEnumerable<TownBase> towns, TownBase town)
    {
      return towns.GetAroundTowns(town).Select(t => t.CountryId).Distinct();
    }

    public static IEnumerable<TownBase> GetAroundTowns(this IEnumerable<TownBase> towns, short x, short y)
    {
      return towns.Where(t => t.IsNextToTown(x, y));
    }

    public static IEnumerable<TownBase> GetAroundTowns(this IEnumerable<TownBase> towns, IEnumerable<TownBase> sources)
    {
      return towns.Where(t => !sources.Any(s => s.Id == t.Id) && sources.Any(s => t.IsNextToTown(s))).Distinct();
    }

    public static IEnumerable<uint> GetAroundCountries(this IEnumerable<TownBase> towns, IEnumerable<TownBase> sources)
    {
      return towns.GetAroundTowns(sources).Select(t => t.CountryId).Distinct();
    }

    public static IEnumerable<TownBase> GetOrderedAroundTowns(this IEnumerable<TownBase> towns, TownBase town)
    {
      return GetOrderedAroundTowns(towns, town.X, town.Y);
    }

    public static IEnumerable<TownBase> GetOrderedAroundTowns(this IEnumerable<TownBase> towns, short x, short y)
    {
      var pos = new int[] { x - 1, y - 1, x, y - 1, x + 1, y - 1, x + 1, y, x + 1, y + 1, x, y + 1, x - 1, y + 1, x - 1, y };
      for (var i = 0; i < 8; i++)
      {
        var tx = pos[i * 2];
        var ty = pos[i * 2 + 1];
        var town = towns.FirstOrDefault(t => t.X == tx && t.Y == ty);
        if (town != null)
        {
          yield return town;
        }
      }
    }

    public static bool IsNextToTown(this IEnumerable<TownBase> towns, TownBase a, TownBase b)
    {
      return towns.GetAroundTowns(a).Contains(b);
    }

    public static bool IsNextToTown<T>(this T a, TownBase b) where T : TownBase
    {
      return Math.Abs(a.X - b.X) <= 1 && Math.Abs(a.Y - b.Y) <= 1 && !(a.X == b.X && a.Y == b.Y);
    }

    public static bool IsNextToTown<T>(this T a, short x, short y) where T : TownBase
    {
      return Math.Abs(a.X - x) <= 1 && Math.Abs(a.Y - y) <= 1 && !(a.X == x && a.Y == y);
    }
  }
}
