using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("initial_town")]
  public class InitialTown
  {
    [Key]
    [Column("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 特化
    /// </summary>
    [Column("type")]
    public TownType Type { get; set; }

    /// <summary>
    /// 名前
    /// </summary>
    [Column("name", TypeName = "varchar(64)")]
    public string Name { get; set; }

    /// <summary>
    /// 都市の位置X
    /// </summary>
    [Column("x")]
    public short X { get; set; }

    /// <summary>
    /// 都市の位置Y
    /// </summary>
    [Column("y")]
    public short Y { get; set; }
  }
}
