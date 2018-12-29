﻿using System;
using System.Collections.Generic;
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
    [JsonProperty("gameDate")]
    public GameDateTime GameDateTime
    {
      get => GameDateTime.FromInt(this.IntGameDateTime);
      set => this.IntGameDateTime = value.ToInt();
    }

    /// <summary>
    /// コマンドパラメータ
    /// </summary>
    [NotMapped]
    [JsonProperty("parameters")]
    public IList<CharacterCommandParameter> Parameters { get; private set; } = new List<CharacterCommandParameter>();

    public CharacterCommand SetParameters(IEnumerable<CharacterCommandParameter> parameters)
    {
      if (this.Parameters == null)
      {
        this.Parameters = new List<CharacterCommandParameter>(parameters);
      }
      else
      {
        foreach (var param in parameters)
        {
          this.Parameters.Add(param);
        }
      }
      return this;
    }
  }

  public enum CharacterCommandType : short
  {
    /// <summary>
    /// 何も実行しない
    /// </summary>
    None = 0,

    /// <summary>
    /// 農業開発
    /// </summary>
    Agriculture = 1,

    /// <summary>
    /// 商業発展
    /// </summary>
    Commercial = 2,

    /// <summary>
    /// 技術開発
    /// </summary>
    Technology = 3,

    /// <summary>
    /// 城壁強化
    /// </summary>
    Wall = 4,

    /// <summary>
    /// 守兵増強
    /// </summary>
    WallGuard = 5,
  }
}
