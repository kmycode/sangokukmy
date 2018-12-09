﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("character_commands")]
  public class CharacterCommand
  {
    [Key]
    [Column("id")]
    [JsonIgnore]
    public uint Id { get; set; }

    /// <summary>
    /// 武将ID
    /// </summary>
    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    /// <summary>
    /// コマンドの種類
    /// </summary>
    [Column("type")]
    [JsonIgnore]
    public CharacterCommandType Type { get; set; }

    /// <summary>
    /// コマンドの種類（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (CharacterCommandType)value;
    }

    /// <summary>
    /// コマンドを実行するゲーム内の年月（DB保存用）
    /// </summary>
    [Column("game_date")]
    [JsonIgnore]
    public int IntGameDateTime { get; set; }

    /// <summary>
    /// コマンドを実行するゲーム内の年月
    /// </summary>
    [NotMapped]
    [JsonProperty("game_date")]
    public GameDateTime GameDateTime
    {
      get => GameDateTime.FromInt(this.IntGameDateTime);
      set => this.IntGameDateTime = value.ToInt();
    }
  }

  public enum CharacterCommandType : short
  {
    /// <summary>
    /// 何も実行しない
    /// </summary>
    None = 0,
  }
}
