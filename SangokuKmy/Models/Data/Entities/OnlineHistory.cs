using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("online_histories")]
  public class OnlineHistory
  {
    [Key]
    [Column("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 武将ID
    /// </summary>
    [Column("character_id")]
    public uint CharacterId { get; set; }

    /// <summary>
    /// 出力されたゲーム内の日時（DB保存用）
    /// </summary>
    [Column("game_date")]
    public int IntGameDateTime { get; set; }
  }
}
