using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("push_notification_keys")]
  public class PushNotificationKey
  {
    [Key]
    [Column("id")]
    public uint Id { get; set; }

    [Column("character_id")]
    public uint CharacterId { get; set; }

    [Column("platform")]
    public PushNotificationPlatform Platform { get; set; }

    [Column("key", TypeName = "varchar(256)")]
    public string Key { get; set; }
  }

  public enum PushNotificationPlatform : short
  {
    Undefined = 0,
    Android = 1,
    iOS = 2,
  }
}
