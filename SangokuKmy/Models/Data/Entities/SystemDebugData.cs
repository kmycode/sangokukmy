using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("system_debug")]
  public class SystemDebugData
  {
    [Key]
    [Column("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 更新可能な最後のゲーム内の年月（DB保存用）
    /// </summary>
    [Column("updatable_game_date_time")]
    public int IntUpdatableGameDateTime
    {
      get => this.UpdatableGameDateTime.ToInt();
      set => this.UpdatableGameDateTime = GameDateTime.FromInt(value);
    }

    /// <summary>
    /// 更新可能な最後のゲーム内の年月
    /// </summary>
    [NotMapped]
    public GameDateTime UpdatableGameDateTime { get; set; }
  }
}
