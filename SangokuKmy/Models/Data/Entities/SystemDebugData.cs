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
    /// 更新可能な最後の時刻
    /// </summary>
    [Column("updatable_last_date")]
    public DateTime UpdatableLastDateTime { get; set; }
  }
}
