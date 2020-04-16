using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("mute_keywords")]
  public class MuteKeyword
  {
    [Key]
    [Column("id")]
    [JsonIgnore]
    public uint Id { get; set; }

    /// <summary>
    /// ミューとした武将
    /// </summary>
    [Column("character_id")]
    [JsonIgnore]
    public uint CharacterId { get; set; }

    /// <summary>
    /// ミュートするキーワード
    /// </summary>
    [Column("keywords")]
    [JsonProperty("keywords")]
    public string Keywords { get; set; }
  }
}
