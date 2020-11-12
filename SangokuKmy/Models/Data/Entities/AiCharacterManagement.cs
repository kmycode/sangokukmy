using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("ai_character_managements")]
  public class AiCharacterManagement
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// AI武将ID
    /// </summary>
    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    /// <summary>
    /// 所持している武将ID
    /// </summary>
    [Column("holder_character_id")]
    [JsonProperty("holderCharacterId")]
    public uint HolderCharacterId { get; set; }

    /// <summary>
    /// 行動
    /// </summary>
    [Column("action")]
    [JsonIgnore]
    public AiCharacterAction Action { get; set; }

    /// <summary>
    /// 行動（API出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("action")]
    public short ApiAction
    {
      get => (short)this.Action;
      set => this.Action = (AiCharacterAction)value;
    }

    /// <summary>
    /// 兵種
    /// </summary>
    [Column("soldier_type")]
    [JsonIgnore]
    public AiCharacterSoldierType SoldierType { get; set; }

    /// <summary>
    /// 兵種（API出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("soldierType")]
    public short ApiSoldierType
    {
      get => (short)this.SoldierType;
      set => this.SoldierType = (AiCharacterSoldierType)value;
    }

    /// <summary>
    /// 対象都市
    /// </summary>
    [Column("target_town_id")]
    [JsonProperty("targetTownId")]
    public uint TargetTownId { get; set; }

    /// <summary>
    /// 雇用した年月
    /// </summary>
    [Column("start_game_date_time")]
    [JsonIgnore]
    public int IntStartGameDateTime { get; set; }
  }

  public enum AiCharacterAction : short
  {
    None = 0,

    /// <summary>
    /// 内政
    /// </summary>
    DomesticAffairs = 1,

    /// <summary>
    /// 守備
    /// </summary>
    Defend = 2,

    /// <summary>
    /// 特定都市へ攻撃
    /// </summary>
    Attack = 3,

    /// <summary>
    /// 遊撃
    /// </summary>
    Assault = 4,

    Removed = 5,
  }

  public enum AiCharacterSoldierType : short
  {
    Default = 0,
    Infancty = 1,
    Cavalry = 2,
    Archer = 3,
  }
}
