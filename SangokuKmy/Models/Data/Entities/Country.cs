﻿using System;
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
    /// AIの種類
    /// </summary>
    [Column("ai_type")]
    [JsonIgnore]
    public CountryAiType AiType { get; set; }

    /// <summary>
    /// AIの種類（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("aiType")]
    public short ApiAiType
    {
      get => (short)this.AiType;
      set => this.AiType = (CountryAiType)value;
    }

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
    /// 最後の政策ポイント収入
    /// </summary>
    [Column("last_policy_point_incomes")]
    [JsonProperty("lastPolicyPointIncomes")]
    public int LastPolicyPointIncomes { get; set; }

    /// <summary>
    /// 最後に要求された収入
    /// </summary>
    [Column("last_requested_incomes")]
    [JsonProperty("lastRequestedIncomes")]
    public int LastRequestedIncomes { get; set; }

    /// <summary>
    /// 国庫の金
    /// </summary>
    [Column("safe_money")]
    [JsonProperty("safeMoney")]
    public int SafeMoney { get; set; }

    /// <summary>
    /// 政策ポイント
    /// </summary>
    [Column("policy_point")]
    [JsonProperty("policyPoint")]
    public int PolicyPoint { get; set; }

    /// <summary>
    /// 玉璽の状態
    /// </summary>
    [Column("gyokuji_status")]
    [JsonIgnore]
    public CountryGyokujiStatus GyokujiStatus { get; set; }

    /// <summary>
    /// 玉璽を持っているか
    /// </summary>
    [NotMapped]
    [JsonProperty("isHaveGyokuji")]
    public bool IsHaveGyokuji
    {
      get => this.GyokujiStatus != CountryGyokujiStatus.NotHave && this.GyokujiStatus != CountryGyokujiStatus.Refused;
      set { }
    }

    [NotMapped]
    [JsonProperty("isGyokujiRefused")]
    public bool IsGyokujiRefused
    {
      get => this.GyokujiStatus == CountryGyokujiStatus.Refused;
      set { }
    }

    /// <summary>
    /// 玉璽を手に入れた、ゲーム内の年月（DB用）
    /// </summary>
    [Column("gyokuji_game_date")]
    [JsonIgnore]
    public int IntGyokujiGameDate { get; set; }

    /// <summary>
    /// 玉璽を手に入れた、ゲーム内の年月
    /// </summary>
    [NotMapped]
    [JsonProperty("gyokujiGameDate")]
    public GameDateTime GyokujiGameDate
    {
      get => GameDateTime.FromInt(this.IntGyokujiGameDate);
      set => this.IntGyokujiGameDate = value.ToInt();
    }

    /// <summary>
    /// 国教
    /// </summary>
    [Column("religion")]
    [JsonIgnore]
    public ReligionType Religion { get; set; }

    [NotMapped]
    [JsonProperty("religion")]
    public short ApiReligion
    {
      get => (short)this.Religion;
      set => this.Religion = (ReligionType)value;
    }

    /// <summary>
    /// 戦争のペナルティを受けている状態であるか
    /// </summary>
    [Column("is_war_penalty")]
    [JsonProperty("isWarPenalty")]
    public bool IsWarPenalty { get; set; }

    /// <summary>
    /// 大国ペナルティを受けている状態であるか
    /// </summary>
    [Column("is_large_country_penalty")]
    [JsonIgnore]
    public bool IsLargeCountryPenalty { get; set; }

    /// <summary>
    /// 建築物の追加敷地
    /// </summary>
    [Column("town_sub_building_extra_space")]
    [JsonProperty("townSubBuildingExtraSpace")]
    public short TownSubBuildingExtraSpace { get; set; }

    /// <summary>
    /// 文明
    /// </summary>
    [Column("civilization")]
    [JsonIgnore]
    public CountryCivilization Civilization { get; set; }

    /// <summary>
    /// 文明
    /// </summary>
    [NotMapped]
    [JsonProperty("civilization")]
    public short ApiCivilization
    {
      get => (short)this.Civilization;
      set => this.Civilization = (CountryCivilization)value;
    }
  }

  public enum CountryAiType : short
  {
    Human = 0,
    Farmers = 1,
    Terrorists = 2,
    Thiefs = 3,
    Managed = 4,
    Puppet = 5,
    TerroristsEnemy = 6,
  }

  public enum CountryGyokujiStatus : short
  {
    /// <summary>
    /// 玉璽を持っていない
    /// </summary>
    NotHave = 0,

    /// <summary>
    /// 本物を持っている
    /// </summary>
    HasGenuine = 1,

    /// <summary>
    /// まがい物を持っている
    /// </summary>
    HasFake = 2,

    /// <summary>
    /// 配布時に拒否する
    /// </summary>
    Refused = 3,
  }

  public enum CountryBattlePolicy : short
  {
    /// <summary>
    /// なし　
    /// </summary>
    None = 0,

    /// <summary>
    /// 統一重視
    /// </summary>
    Unification = 1,

    /// <summary>
    /// まったり
    /// </summary>
    Relax = 2,

    /// <summary>
    /// 放任主義
    /// </summary>
    LaissezFaire = 3,
  }

  public enum CountryCivilization : short
  {
    None = 0,
    A = 1,
    B = 2,
    C = 3,
  }
}
