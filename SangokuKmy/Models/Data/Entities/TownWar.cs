using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("town_wars")]
  public class TownWar
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 戦争の状態
    /// </summary>
    [Column("status")]
    [JsonIgnore]
    public TownWarStatus Status { get; set; }

    /// <summary>
    /// 戦争の状態（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("status")]
    public short ApiStatus
    {
      get => (short)this.Status;
      set => this.Status = (TownWarStatus)value;
    }

    /// <summary>
    /// 都市ID
    /// </summary>
    [Column("town_id")]
    [JsonProperty("townId")]
    public uint TownId { get; set; }

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
    /// 開戦年月（DB保存用）
    /// </summary>
    [Column("game_date")]
    [JsonIgnore]
    public int IntGameDate
    {
      get => this.GameDate.ToInt();
      set => this.GameDate = GameDateTime.FromInt(value);
    }

    /// <summary>
    /// 開戦年月
    /// </summary>
    [NotMapped]
    [JsonProperty("gameDate")]
    public GameDateTime GameDate { get; set; }
  }

  public enum TownWarStatus : short
  {
    None = 0,
    InReady = 1,
    Available = 2,
    Terminated = 3,
  }
}
