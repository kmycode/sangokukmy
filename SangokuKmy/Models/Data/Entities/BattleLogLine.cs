using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
namespace SangokuKmy.Models.Data.Entities
{
  [Table("battle_log_lines")]
  public class BattleLogLine
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// バトルログのID
    /// </summary>
    [Column("battle_log_id")]
    [JsonProperty("battleLogId")]
    public uint BattleLogId { get; set; }

    /// <summary>
    /// ターン
    /// </summary>
    [Column("turn")]
    [JsonProperty("turn")]
    public short Turn { get; set; }

    /// <summary>
    /// 攻撃側のこのターンのダメージ
    /// </summary>
    [Column("attacker_damage")]
    [JsonProperty("attackerDamage")]
    public short AttackerDamage { get; set; }

    /// <summary>
    /// 攻撃側のダメージを受けた後の残り数値
    /// </summary>
    [Column("attacker_number")]
    [JsonProperty("attackerNumber")]
    public short AttackerNumber { get; set; }

    /// <summary>
    /// 防御側のこのターンのダメージ
    /// </summary>
    [Column("defender_damage")]
    [JsonProperty("defenderDamage")]
    public short DefenderDamage { get; set; }

    /// <summary>
    /// 防御側のダメージを受けた後の残り数値
    /// </summary>
    [Column("defender_number")]
    [JsonProperty("defenderNumber")]
    public short DefenderNumber { get; set; }
  }
}
