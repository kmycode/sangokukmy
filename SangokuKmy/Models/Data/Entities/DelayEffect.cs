using SangokuKmy.Models.Data.ApiEntities;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("delay_effects")]
  public class DelayEffect
  {
    [Key]
    [Column("id")]
    public uint Id { get; set; }

    [Column("character_id")]
    public uint CharacterId { get; set; }

    [Column("town_id")]
    public uint TownId { get; set; }

    [Column("country_id")]
    public uint CountryId { get; set; }

    [Column("type")]
    public DelayEffectType Type { get; set; }

    [Column("type_data")]
    public int TypeData { get; set; }

    [Column("type_data2")]
    public int TypeData2 { get; set; }

    [Column("appear_game_date_time")]
    public int IntAppearGameDateTime { get; set; }

    [NotMapped]
    public GameDateTime AppearGameDateTime
    {
      get => GameDateTime.FromInt(this.IntAppearGameDateTime);
      set => this.IntAppearGameDateTime = value.ToInt();
    }
  }

  public enum DelayEffectType : short
  {
    Undefined = 0,

    /// <summary>
    /// 都市投資
    /// </summary>
    TownInvestment = 1,
  }
}
