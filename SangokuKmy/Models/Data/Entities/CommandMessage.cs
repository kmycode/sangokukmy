using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("command_messages")]
  public class CommandMessage
  {
    [Key]
    [Column("id")]
    [JsonIgnore]
    public uint Id { get; set; }

    /// <summary>
    /// 国ID
    /// </summary>
    [Column("country_id")]
    [JsonIgnore]
    public uint CountryId { get; set; }

    /// <summary>
    /// コマンドを実行するゲーム内の年月（DB保存用）
    /// </summary>
    [Column("game_date")]
    [JsonIgnore]
    public int IntGameDateTime { get; set; }

    /// <summary>
    /// コマンドを実行するゲーム内の年月
    /// </summary>
    [NotMapped]
    [JsonProperty("gameDate")]
    public GameDateTime GameDateTime
    {
      get => GameDateTime.FromInt(this.IntGameDateTime);
      set => this.IntGameDateTime = value.ToInt();
    }

    /// <summary>
    /// メッセージ
    /// </summary>
    [Column("message")]
    [JsonProperty("message")]
    public string Message { get; set; }
  }
}
