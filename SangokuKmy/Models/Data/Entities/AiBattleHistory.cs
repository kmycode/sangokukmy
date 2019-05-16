using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("ai_battle_histories")]
  public class AiBattleHistory
  {
    [Key]
    [Column("id")]
    public uint Id { get; set; }

    [Column("character_id")]
    public uint CharacterId { get; set; }

    [Column("defender_id")]
    public uint DefenderId { get; set; }

    [Column("country_id")]
    public uint CountryId { get; set; }

    [Column("town_id")]
    public uint TownId { get; set; }

    [Column("town_country_id")]
    public uint TownCountryId { get; set; }

    [Column("game_date_time")]
    public int IntGameDateTime { get; set; }

    [Column("attacker_soldiers_money")]
    public int AttackerSoldiersMoney { get; set; }

    [Column("target_type")]
    public AiBattleTargetType TargetType { get; set; }

    [Column("town_type")]
    public AiBattleTownType TownType { get; set; }

    [Column("rest_defender_count")]
    public int RestDefenderCount { get; set; }
  }

  public enum AiBattleTargetType : short
  {
    Unknown = 0,
    Wall = 1,
    Character = 2,
    CharacterLowSoldiers = 3,
  }

  [Flags]
  public enum AiBattleTownType : short
  {
    Undefined = 0,
    MainTown = 1,
    BorderTown = 2,
    MainAndBorderTown = 3,
    EnemyTown = 4,
    Others = 64,
  }
}
