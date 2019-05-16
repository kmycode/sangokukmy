using SangokuKmy.Models.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("ai_action_histories")]
  public class AiActionHistory
  {
    [Key]
    [Column("id")]
    public uint Id { get; set; }

    [Column("character_id")]
    public uint CharacterId { get; set; }

    [Column("game_date_time")]
    public int IntGameDateTime { get; set; }

    [Column("rice_price")]
    public int IntRicePrice { get; set; }

    [NotMapped]
    public float RicePrice
    {
      get => this.IntRicePrice / Config.RicePriceBase;
      set => this.IntRicePrice = (int)(value * Config.RicePriceBase);
    }
  }
}
