﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  /// <summary>
  /// 武将
  /// </summary>
  [Table("characters")]
  public class Character
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

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
    [JsonProperty("class")]
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
  }
}
