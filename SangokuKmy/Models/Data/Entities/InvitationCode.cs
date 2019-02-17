using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("invitation_code")]
  public class InvitationCode
  {
    [Key]
    [Column("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 招待コードの目的
    /// </summary>
    [Column("aim")]
    public InvitationCodeAim Aim { get; set; }

    /// <summary>
    /// 招待コード
    /// </summary>
    [Column("code", TypeName = "varchar(64)")]
    public string Code { get; set; }

    /// <summary>
    /// すでに使われたか
    /// </summary>
    [Column("has_used")]
    public bool HasUsed { get; set; }

    /// <summary>
    /// 実際に使用した武将ID
    /// </summary>
    [Column("character_id")]
    public uint CharacterId { get; set; }

    /// <summary>
    /// 使われた日時
    /// </summary>
    [Column("used")]
    public DateTime Used { get; set; }
  }

  public enum InvitationCodeAim : short
  {
    /// <summary>
    /// 新規登録そのもの
    /// </summary>
    Entry = 1,
  }
}
