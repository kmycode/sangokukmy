using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using SangokuKmy.Common;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  /// <summary>
  /// 武将
  /// </summary>
  [Table("characters")]
  public class Character
  {
    #region properties

    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// ログインに使うID
    /// </summary>
    [Column("alias_id", TypeName = "varchar(32)")]
    [JsonProperty("aliasId")]
    public string AliasId { get; set; }

    /// <summary>
    /// パスワードのハッシュ
    /// </summary>
    [Column("password_hash", TypeName = "varchar(256)")]
    [JsonIgnore]
    public string PasswordHash { get; set; }

    /// <summary>
    /// 名前
    /// </summary>
    [Column("name", TypeName = "varchar(64)")]
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    /// 国のID
    /// </summary>
    [Column("country_id")]
    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    /// <summary>
    /// 武力
    /// </summary>
    [Column("strong")]
    [JsonProperty("strong")]
    public short Strong { get; set; }

    /// <summary>
    /// 武力経験値
    /// </summary>
    [Column("strong_ex")]
    [JsonProperty("strongEx")]
    public short StrongEx { get; set; }

    /// <summary>
    /// 知力
    /// </summary>
    [Column("intellect")]
    [JsonProperty("intellect")]
    public short Intellect { get; set; }

    /// <summary>
    /// 知力経験値
    /// </summary>
    [Column("intellect_ex")]
    [JsonProperty("intellectEx")]
    public short IntellectEx { get; set; }

    /// <summary>
    /// 統率力
    /// </summary>
    [Column("leadership")]
    [JsonProperty("leadership")]
    public short Leadership { get; set; }

    /// <summary>
    /// 統率力経験値
    /// </summary>
    [Column("leadership_ex")]
    [JsonProperty("leadershipEx")]
    public short LeadershipEx { get; set; }

    /// <summary>
    /// 人望
    /// </summary>
    [Column("popularity")]
    [JsonProperty("popularity")]
    public short Popularity { get; set; }

    /// <summary>
    /// 人望経験値
    /// </summary>
    [Column("popularity_ex")]
    [JsonProperty("popularityEx")]
    public short PopularityEx { get; set; }

    /// <summary>
    /// 兵士の種類
    /// </summary>
    [Column("soldier_type")]
    [JsonIgnore]
    public SoldierType SoldierType { get; set; }

    /// <summary>
    /// 兵士の種類（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("soldierType")]
    public short ApiSoldierType
    {
      get => (short)this.SoldierType;
      set => this.SoldierType = (SoldierType)value;
    }

    /// <summary>
    /// 兵士数
    /// </summary>
    [Column("soldier_number")]
    [JsonProperty("soldierNumber")]
    public int SoldierNumber { get; set; }

    /// <summary>
    /// 訓練値
    /// </summary>
    [Column("proficiency")]
    [JsonProperty("proficiency")]
    public short Proficiency { get; set; }

    /// <summary>
    /// 金
    /// </summary>
    [Column("money")]
    [JsonProperty("money")]
    public int Money { get; set; }

    /// <summary>
    /// 米
    /// </summary>
    [Column("rice")]
    [JsonProperty("rice")]
    public int Rice { get; set; }

    /// <summary>
    /// 貢献
    /// </summary>
    [Column("contribution")]
    [JsonProperty("contribution")]
    public int Contribution { get; set; }

    /// <summary>
    /// 階級
    /// </summary>
    [Column("class")]
    [JsonProperty("classValue")]
    public int Class { get; set; }

    /// <summary>
    /// 削除ターン
    /// </summary>
    [Column("delete_turn")]
    [JsonProperty("deleteTurn")]
    public short DeleteTurn { get; set; }

    /// <summary>
    /// 所属都市のID
    /// </summary>
    [Column("town_id")]
    [JsonProperty("townId")]
    public uint TownId { get; set; }

    /// <summary>
    /// 一言コメント
    /// </summary>
    [Column("message")]
    [JsonProperty("message")]
    public string Message { get; set; }

    /// <summary>
    /// 最終更新時刻
    /// </summary>
    [Column("last_updated")]
    [JsonIgnore]
    public DateTime LastUpdated { get; set; }

    /// <summary>
    /// 最終更新時刻（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("lastUpdated")]
    public ApiDateTime ApiLastUpdated
    {
      get => ApiDateTime.FromDateTime(this.LastUpdated);
      set => this.LastUpdated = value.ToDateTime();
    }

    /// <summary>
    /// 最後に更新された、ゲーム内の年月（DB用）
    /// </summary>
    [Column("last_updated_game_date")]
    [JsonIgnore]
    public int IntLastUpdatedGameDate { get; set; }

    /// <summary>
    /// 最後に更新された、ゲーム内の年月
    /// </summary>
    [NotMapped]
    [JsonProperty("lastUpdatedGameDate")]
    public GameDateTime LastUpdatedGameDate
    {
      get => GameDateTime.FromInt(this.IntLastUpdatedGameDate);
      set => this.IntLastUpdatedGameDate = value.ToInt();
    }

    /// <summary>
    /// 削除されたか
    /// </summary>
    [Column("has_removed")]
    [JsonProperty("hasRemoved")]
    public bool HasRemoved { get; set; }

    #endregion

    public void AddStrongEx(short ex)
    {
      this.StrongEx += ex;
      while (this.StrongEx >= 1000)
      {
        this.Strong++;
        this.StrongEx -= 1000;
      }
    }

    public void AddIntellectEx(short ex)
    {
      this.IntellectEx += ex;
      while (this.IntellectEx >= 1000)
      {
        this.Intellect++;
        this.IntellectEx -= 1000;
      }
    }

    public void AddLeadershipEx(short ex)
    {
      this.LeadershipEx += ex;
      while (this.LeadershipEx >= 1000)
      {
        this.Leadership++;
        this.LeadershipEx -= 1000;
      }
    }

    public void AddPopularityEx(short ex)
    {
      this.PopularityEx += ex;
      while (this.PopularityEx >= 1000)
      {
        this.Popularity++;
        this.PopularityEx -= 1000;
      }
    }

    /// <summary>
    /// パスワードを設定する。平文のパラメータを指定し、実際はハッシュに変換されたパスワードが保存される
    /// </summary>
    /// <param name="password">パスワード</param>
    public void SetPassword(string password)
    {
      this.PasswordHash = GeneratePasswordHash(password);
    }

    /// <summary>
    /// ログインできるか確認する
    /// </summary>
    /// <returns>指定したログイン情報でログイン可能であるか</returns>
    /// <param name="password">パスワード</param>
    public bool TryLogin(string password)
    {
      return this.PasswordHash == GeneratePasswordHash(password);
    }

    /// <summary>
    /// パスワードからハッシュを生成する
    /// </summary>
    /// <returns>生成されたハッシュ</returns>
    /// <param name="password">パスワード</param>
    private static string GeneratePasswordHash(string password)
    {
      var hash = new SHA256CryptoServiceProvider()
        .ComputeHash(Encoding.UTF8.GetBytes($"{password} hello, for you"))
        .Select(b => string.Format("{0:x2}", b));
      var hashText = string.Join(string.Empty, hash);

      return hashText;
    }
  }

  public enum SoldierType : short
  {
    /// <summary>
    /// 雑兵
    /// </summary>
    Common = 1,

    /// <summary>
    /// 護衛兵
    /// </summary>
    Guard = 2,

    /// <summary>
    /// 軽歩兵
    /// </summary>
    LightInfantry = 3,

    /// <summary>
    /// 弓兵
    /// </summary>
    Archer = 4,

    /// <summary>
    /// 軽騎兵
    /// </summary>
    LightCavalry = 5,

    /// <summary>
    /// 強度兵
    /// </summary>
    StrongCrossbow = 6,

    /// <summary>
    /// 神鬼兵
    /// </summary>
    LightIntellect = 7,

    /// <summary>
    /// 重歩兵
    /// </summary>
    HeavyInfantry = 8,

    /// <summary>
    /// 重騎兵
    /// </summary>
    HeavyCavalry = 9,

    /// <summary>
    /// 智攻兵
    /// </summary>
    Intellect = 10,

    /// <summary>
    /// 連弩兵
    /// </summary>
    RepeatingCrossbow = 11,

    /// <summary>
    /// 壁守兵
    /// </summary>
    StrongGuards = 12,

    /// <summary>
    /// 衝車
    /// </summary>
    Shosha = 13,

    /// <summary>
    /// 井闌
    /// </summary>
    Seiran = 14,
  }

  public class SoldierTypeData
  {
    public string Name { get; set; }
    public SoldierType Type { get; set; }
    public short Money { get; set; }
    public short Technology { get; set; }
  }
  public static class SoldierTypes
  {
    private static readonly IEnumerable<SoldierTypeData> data = new SoldierTypeData[]
    {
      new SoldierTypeData { Name = "雑兵", Type = SoldierType.Common, Money = 10, Technology = 0, },
      new SoldierTypeData { Name = "禁兵", Type = SoldierType.Guard, Money = 10, Technology = 0, },
      new SoldierTypeData { Name = "軽歩兵", Type = SoldierType.LightInfantry, Money = 20, Technology = 101, },
      new SoldierTypeData { Name = "弓兵", Type = SoldierType.Archer, Money = 30, Technology = 201, },
      new SoldierTypeData { Name = "軽騎兵", Type = SoldierType.LightCavalry, Money = 50, Technology = 301, },
      new SoldierTypeData { Name = "強弩兵", Type = SoldierType.StrongCrossbow, Money = 70, Technology = 401, },
      new SoldierTypeData { Name = "神鬼兵", Type = SoldierType.LightIntellect, Money = 100, Technology = 501, },
      new SoldierTypeData { Name = "重歩兵", Type = SoldierType.HeavyInfantry, Money = 150, Technology = 601, },
      new SoldierTypeData { Name = "重騎兵", Type = SoldierType.HeavyCavalry, Money = 200, Technology = 701, },
      new SoldierTypeData { Name = "智攻兵", Type = SoldierType.Intellect, Money = 250, Technology = 801, },
      new SoldierTypeData { Name = "連弩兵", Type = SoldierType.RepeatingCrossbow, Money = 300, Technology = 901, },
      new SoldierTypeData { Name = "壁守兵", Type = SoldierType.StrongGuards, Money = 350, Technology = 999, },
    };
    public static Optional<SoldierTypeData> Get(SoldierType type)
    {
      return data.SingleOrDefault(d => d.Type == type).ToOptional();
    }
  }
}
