using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("units")]
  public class Unit
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 国のID
    /// </summary>
    [Column("country_id")]
    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    /// <summary>
    /// 名前
    /// </summary>
    [Column("name", TypeName = "varchar(64)")]
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    /// 部隊のメッセージ
    /// </summary>
    [Column("message")]
    [JsonProperty("message")]
    public string Message { get; set; }

    /// <summary>
    /// 入隊制限がかかっているか
    /// </summary>
    [Column("is_limited")]
    [JsonProperty("isLimited")]
    public bool IsLimited { get; set; }

    /// <summary>
    /// メンバ一覧
    /// </summary>
    [NotMapped]
    [JsonProperty("members")]
    public IEnumerable<UnitMember> Members { get; set; }
  }
}
