using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("character_command_parameters")]
  public class CharacterCommandParameter
  {
    [Key]
    [Column("id")]
    [JsonIgnore]
    public uint Id { get; set; }

    /// <summary>
    /// コマンドのID
    /// </summary>
    [Column("character_command_id")]
    [JsonIgnore]
    public uint CharacterCommandId { get; set; }

    /// <summary>
    /// パラメータの固有値
    /// </summary>
    [Column("type")]
    [JsonProperty("type")]
    public int Type { get; set; }

    /// <summary>
    /// 数値の値
    /// </summary>
    [Column("number_value")]
    [JsonProperty("numberValue")]
    public int? NumberValue { get; set; }

    /// <summary>
    /// 文字列値
    /// </summary>
    [Column("string_value")]
    [JsonProperty("stringValue")]
    public string StringValue { get; set; }
  }
}
