﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("country_wars")]
  public class CountryWar
  {
    [Key]
    [Column("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 戦争の状態
    /// </summary>
    [Column("status")]
    [JsonIgnore]
    public CountryWarStatus Status { get; set; }

    /// <summary>
    /// 戦争の状態（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("status")]
    public short ApiStatus
    {
      get => (short)this.Status;
      set => this.Status = (CountryWarStatus)value;
    }

    /// <summary>
    /// 戦争の種類
    /// </summary>
    [Column("mode")]
    [JsonIgnore]
    public CountryWarMode Mode { get; set; }

    [NotMapped]
    [JsonProperty("mode")]
    public short ApiMode
    {
      get => (short)this.Mode;
      set => this.Mode = (CountryWarMode)value;
    }

    /// <summary>
    /// 布告した国のID
    /// </summary>
    [Column("requested_country_id")]
    [JsonProperty("requestedCountryId")]
    public uint RequestedCountryId { get; set; }

    /// <summary>
    /// 布告された国のID
    /// </summary>
    [Column("insisted_country_id")]
    [JsonProperty("insistedCountryId")]
    public uint InsistedCountryId { get; set; }

    /// <summary>
    /// 停戦を要求した国のID
    /// </summary>
    [Column("requested_stop_country_id")]
    [JsonProperty("requestedStopCountryId")]
    public uint RequestedStopCountryId { get; set; }

    /// <summary>
    /// 開戦年月（DB保存用）
    /// </summary>
    [Column("start_game_date")]
    [JsonIgnore]
    public int IntStartGameDate
    {
      get => this.StartGameDate.ToInt();
      set => this.StartGameDate = GameDateTime.FromInt(value);
    }

    /// <summary>
    /// 開戦年月
    /// </summary>
    [NotMapped]
    [JsonProperty("startGameDate")]
    public GameDateTime StartGameDate { get; set; }

    /// <summary>
    /// 布告した国の最大武将数
    /// </summary>
    [Column("requested_country_character_max")]
    [JsonIgnore]
    public short RequestedCountryCharacterMax { get; set; }

    /// <summary>
    /// 布告された国の最大武将数
    /// </summary>
    [Column("insisted_country_character_max")]
    [JsonIgnore]
    public short InsistedCountryCharacterMax { get; set; }
  }

  public enum CountryWarStatus : short
  {
    None = 0,

    /// <summary>
    /// 戦争中、または戦争開始を待機中
    /// </summary>
    Available = 1,

    /// <summary>
    /// 停戦要求中
    /// </summary>
    StopRequesting = 2,

    /// <summary>
    /// 停戦された
    /// </summary>
    Stoped = 3,

    /// <summary>
    /// 準備中
    /// </summary>
    InReady = 4,
  }

  public enum CountryWarMode : short
  {
    /// <summary>
    /// 通常武力
    /// </summary>
    Battle = 0,

    /// <summary>
    /// 宗教戦争
    /// </summary>
    Religion = 1,
  }

  public static class CountryWarExtensions
  {
    public static bool IsJoin(this CountryWar war, uint countryId)
    {
      return war.RequestedCountryId == countryId || war.InsistedCountryId == countryId;
    }

    public static bool IsJoinAvailable(this CountryWar war, uint countryId)
    {
      return war.Status != CountryWarStatus.Stoped && war.IsJoin(countryId);
    }

    public static uint GetEnemy(this CountryWar war, uint countryId)
    {
      return war.RequestedCountryId == countryId ? war.InsistedCountryId : war.RequestedCountryId;
    }
  }
}
