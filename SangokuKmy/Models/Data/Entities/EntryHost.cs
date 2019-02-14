using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("entry_hosts")]
  public class EntryHost
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
    /// IPアドレス
    /// </summary>
    [Column("ip_address", TypeName = "varchar(128)")]
    public string IpAddress { get; set; }
  }
}
