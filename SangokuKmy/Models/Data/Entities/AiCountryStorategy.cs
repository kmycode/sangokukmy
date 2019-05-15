using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace SangokuKmy.Models.Data.Entities
{
  [Table("ai_country_storategies")]
  public class AiCountryStorategy
  {
    [Key]
    [Column("id")]
    public uint Id { get; set; }

    [Column("country_id")]
    public uint CountryId { get; set; }

    [Column("target_order")]
    public BattleTargetOrder TargetOrder { get; set; }

    [Column("target_town_id")]
    public uint TargetTownId { get; set; }

    [Column("border_town_id")]
    public uint BorderTownId { get; set; }

    [Column("next_target_town_id")]
    public uint NextTargetTownId { get; set; }

    [Column("main_town_id")]
    public uint MainTownId { get; set; }

    [Column("develop_town_id")]
    public uint DevelopTownId { get; set; }

    [Column("next_reset_game_date")]
    public int IntNextResetGameDate { get; set; }

    [Column("main_unit_id")]
    public uint MainUnitId { get; set; }

    [Column("border_unit_id")]
    public uint BorderUnitId { get; set; }

    [Column("is_defend_force")]
    public bool IsDefendForce { get; set; }
  }

  public enum BattleTargetOrder : short
  {
    Defenders = 1,
    BreakWall = 2,
    GetTown = 3,
  }
}
