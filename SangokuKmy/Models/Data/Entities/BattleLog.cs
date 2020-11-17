using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System.Collections.Generic;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("battle_logs")]
  public class BattleLog
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 関連する都市ID
    /// </summary>
    [Column("town_id")]
    [JsonProperty("townId")]
    public uint TownId { get; set; }

    /// <summary>
    /// 関連する都市
    /// </summary>
    [NotMapped]
    [JsonProperty("town")]
    public TownForAnonymous Town { get; set; }

    /// <summary>
    /// 攻撃側武将ID
    /// </summary>
    [Column("attacker_character_id")]
    [JsonProperty("attackerCharacterId")]
    public uint AttackerCharacterId { get; set; }

    /// <summary>
    /// 防御側の種類
    /// </summary>
    [Column("defender_type")]
    [JsonProperty("defenderType")]
    public short IntDefenderType { get; set; }

    /// <summary>
    /// 防御側の種類
    /// </summary>
    [NotMapped]
    [JsonIgnore]
    public DefenderType DefenderType
    {
      get => (DefenderType)this.IntDefenderType;
      set => this.IntDefenderType = (short)value;
    }

    /// <summary>
    /// 防御側武将ID
    /// </summary>
    [Column("defender_character_id")]
    [JsonProperty("defenderCharacterId")]
    public uint DefenderCharacterId { get; set; }

    /// <summary>
    /// 攻撃側武将データのキャッシュ
    /// </summary>
    [Column("attacker_cache_id")]
    [JsonProperty("attackerCacheId")]
    public uint AttackerCacheId { get; set; }

    [NotMapped]
    [JsonProperty("attackerCache")]
    public LogCharacterCache AttackerCache { get; set; }

    [Column("attacker_attack_power")]
    [JsonProperty("attackerAttackPower")]
    public int AttackerAttackPower { get; set; }

    /// <summary>
    /// 防御側武将データのキャッシュ
    /// </summary>
    [Column("defender_cache_id")]
    [JsonProperty("defenderCacheId")]
    public uint DefenderCacheId { get; set; }

    [NotMapped]
    [JsonProperty("defenderCache")]
    public LogCharacterCache DefenderCache { get; set; }

    [Column("defender_attack_power")]
    [JsonProperty("defenderAttackPower")]
    public int DefenderAttackPower { get; set; }

    /// <summary>
    /// 関連するマップログ
    /// </summary>
    [Column("maplog_id")]
    [JsonProperty("maplogId")]
    public uint MapLogId { get; set; }

    /// <summary>
    /// 関連するマップログ
    /// </summary>
    [NotMapped]
    [JsonProperty("maplog")]
    public MapLog MapLog { get; set; }

    /// <summary>
    /// 各行詳細データ
    /// </summary>
    [NotMapped]
    [JsonProperty("lines")]
    public IEnumerable<BattleLogLine> Lines { get; set; }

    /// <summary>
    /// 攻撃側と守備側が同じ宗教であるか
    /// </summary>
    [Column("is_same_religion")]
    [JsonProperty("isSameReligion")]
    public bool IsSameReligion { get; set; }

    /// <summary>
    /// 戦闘の原因
    /// </summary>
    [Column("cause")]
    [JsonIgnore]
    public BattleCause Cause { get; set; }
  }

  public enum DefenderType : short
  {
    /// <summary>
    /// 武将
    /// </summary>
    Character = 1,

    /// <summary>
    /// 城壁
    /// </summary>
    Wall = 3,
  }

  public enum BattleCause : short
  {
    /// <summary>
    /// 戦争、その他
    /// </summary>
    War = 0,

    /// <summary>
    /// 攻略
    /// </summary>
    TownWar = 1,
  }
}
