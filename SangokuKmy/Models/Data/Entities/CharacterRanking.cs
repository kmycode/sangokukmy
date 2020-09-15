using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("character_ranking")]
  public class CharacterRanking
  {
    [Key]
    [Column("id")]
    public uint Id { get; set; }

    [Column("character_id")]
    public uint CharacterId { get; set; }

    /// <summary>
    /// 戦闘勝利数
    /// </summary>
    [Column("battle_won_count")]
    [JsonProperty("battleWonCount")]
    public int BattleWonCount { get; set; }

    /// <summary>
    /// 戦闘敗北数
    /// </summary>
    [Column("battle_lost_count")]
    [JsonProperty("battleLostCount")]
    public int BattleLostCount { get; set; }

    /// <summary>
    /// 削った城壁の大きさ
    /// </summary>
    [Column("battle_broke_wall_size")]
    [JsonProperty("battleBrokeWallSize")]
    public int BattleBrokeWallSize { get; set; }

    /// <summary>
    /// 支配数
    /// </summary>
    [Column("battle_dominate_count")]
    [JsonProperty("battleDominateCount")]
    public int BattleDominateCount { get; set; }

    /// <summary>
    /// 連戦回数
    /// </summary>
    [Column("battle_continuous_count")]
    [JsonProperty("battleContinuousCount")]
    public int BattleContinuousCount { get; set; }

    /// <summary>
    /// 計略使用回数
    /// </summary>
    [Column("battle_scheme_count")]
    [JsonProperty("battleSchemeCount")]
    public int BattleSchemeCount { get; set; }

    /// <summary>
    /// 戦闘で倒した兵士数
    /// </summary>
    [Column("battle_killed_count")]
    [JsonProperty("battleKilledCount")]
    public int BattleKilledCount { get; set; }

    /// <summary>
    /// 戦闘で倒された兵士数
    /// </summary>
    [Column("battle_being_killed_count")]
    [JsonProperty("battleBeingKilledCount")]
    public int BattleBeingKilledCount { get; set; }
  }
}
