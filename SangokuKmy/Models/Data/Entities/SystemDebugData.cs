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

    /// <summary>
    /// 重複登録を許可するか
    /// </summary>
    [Column("is_check_duplicate_entry")]
    public bool IsCheckDuplicateEntry { get; set; }

    /// <summary>
    /// ゲーム進行に影響のあるデバッグコマンドの実行を許可するか
    /// </summary>
    [Column("can_use_debug_commands")]
    public bool CanUseDebugCommands { get; set; }

    /// <summary>
    /// デバッグ時にクエリに指定するパスワード。空でなければ有効
    /// </summary>
    [Column("password", TypeName = "varchar(32)")]
    public string DebugPassword { get; set; }
  }
}
