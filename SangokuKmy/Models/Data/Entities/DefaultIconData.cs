using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("m_default_icons")]
  public class DefaultIconData
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// ファイル名
    /// </summary>
    [Column("file_name", TypeName = "varchar(256)")]
    [JsonProperty("fileName")]
    public string FileName { get; set; }
  }
}
