using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("ai_country_managements")]
  public class AiCountryManagement
  {
    [Key]
    [Column("id")]
    public uint Id { get; set; }

    [Column("country_id")]
    public uint CountryId { get; set; }

    [Column("war_policy")]
    public AiCountryWarPolicy WarPolicy { get; set; }

    [Column("war_target_policy")]
    public AiCountryWarTargetPolicy WarTargetPolicy { get; set; }

    [Column("war_start_date_policy")]
    public AiCountryWarStartDatePolicy WarStartDatePolicy { get; set; }

    [Column("policy_target")]
    public AiCountryPolicyTarget PolicyTarget { get; set; }

    [Column("seiran_policy")]
    public AgainstSeiranPolicy SeiranPolicy { get; set; }

    [Column("is_policy_first")]
    public bool IsPolicyFirst { get; set; }

    [Column("is_policy_second")]
    public bool IsPolicySecond { get; set; }

    [Column("war_style")]
    public AiCountryWarStyle WarStyle { get; set; }

    [Column("virtual_enemy_country_id")]
    public uint VirtualEnemyCountryId { get; set; }

    [Column("character_size")]
    public AiCountryCharacterSize CharacterSize { get; set; }

    [Column("unit_policy")]
    public AiCountryUnitPolicy UnitPolicy { get; set; }

    [Column("unit_gather_policy")]
    public AiCountryUnitGatherPolicy UnitGatherPolicy { get; set; }

    [Column("force_defend_policy")]
    public AiCountryForceDefendPolicy ForceDefendPolicy { get; set; }
  }

  public enum AiCountryWarPolicy : short
  {
    Unknown = 0,
    GoodFight = 1,
    Balance = 2,
    Carefully = 3,
  }

  public enum AiCountryPolicyTarget : short
  {
    Unknown = 0,
    Wall = 1,
    Money = 2,
    People = 3,
  }

  public enum AgainstSeiranPolicy : short
  {
    Unknown = 0,
    Gonorrhea = 1,
    Mindful = 2,
    NotCare = 3,
    NotCareMuch = 4,
  }

  public enum AiCountryWarStyle : short
  {
    Unknown = 0,
    NotCare = 1,
    Negative = 2,
    Normal = 3,
    Aggressive = 4,
  }
  
  public enum AiCountryWarTargetPolicy : short
  {
    Unknown = 0,
    Random = 1,
    Weakest = 2,
    EqualityWeaker = 3,
    EqualityStronger = 4,
  }

  public enum AiCountryWarStartDatePolicy : short
  {
    Unknown = 0,
    First21 = 1,
    FirstBetween19And23 = 2,
    HurryUp = 3,
  }

  public enum AiCountryCharacterSize : short
  {
    Unknown = 0,
    Small = 1,
    Medium = 2,
    Large = 3,
  }

  public enum AiCountryUnitPolicy : short
  {
    Unknown = 0,
    NotCare = 1,
    BorderTownOnly = 2,
    BorderAndMainTown = 3,
  }

  public enum AiCountryUnitGatherPolicy : short
  {
    Unknown = 0,
    Always = 1,
    BeforePeopleChanges = 2,
  }

  public enum AiCountryForceDefendPolicy : short
  {
    Unknown = 0,
    NotCare = 1,
    Negative = 2,
    Medium = 3,
    Aggressive = 4,
  }
}
