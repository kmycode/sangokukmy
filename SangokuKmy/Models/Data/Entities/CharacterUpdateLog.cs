using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("CharacterUpdateLogs")]
  public class CharacterUpdateLog
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// これがこの月最初のログであるか
    /// </summary>
    [Column("is_first_at_month")]
    [JsonProperty("isFirstAtMonth")]
    public bool IsFirstAtMonth { get; set; }

    /// <summary>
    /// 武将ID
    /// </summary>
    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    /// <summary>
    /// 武将名
    /// </summary>
    [NotMapped]
    [JsonProperty("characterName")]
    public string CharacterName { get; set; }

    /// <summary>
    /// 出力された日時
    /// </summary>
    [Column("date")]
    [JsonIgnore]
    public DateTime DateTime { get; set; }

    /// <summary>
    /// 出力された日時（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("date")]
    public ApiDateTime ApiDateTime
    {
      get => ApiDateTime.FromDateTime(this.DateTime);
      set => this.DateTime = value.ToDateTime();
    }

    /// <summary>
    /// 出力されたゲーム内の日時（DB保存用）
    /// </summary>
    [Column("game_date")]
    [JsonIgnore]
    public int IntGameDateTime { get; set; }

    /// <summary>
    /// 出力されたゲーム内の日時
    /// </summary>
    [NotMapped]
    [JsonProperty("gameDate")]
    public GameDateTime GameDateTime
    {
      get => GameDateTime.FromInt(this.IntGameDateTime);
      set => this.IntGameDateTime = value.ToInt();
    }
  }
}
