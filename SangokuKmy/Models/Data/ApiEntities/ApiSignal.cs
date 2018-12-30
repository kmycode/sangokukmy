﻿using System;
using Newtonsoft.Json;
namespace SangokuKmy.Models.Data.ApiEntities
{
  /// <summary>
  /// 何かの信号
  /// </summary>
  public class ApiSignal
  {
    /// <summary>
    /// シグナルの種類
    /// </summary>
    [JsonIgnore]
    public SignalType Type { get; set; }

    /// <summary>
    /// シグナルの種類（JSON出力用）
    /// </summary>
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (SignalType)value;
    }

    /// <summary>
    /// シグナルの追加データ
    /// </summary>
    [JsonProperty("data")]
    public object Data { get; set; }
  }

  public enum SignalType : short
  {
    /// <summary>
    /// 不明なシグナル
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// 現在の武将が更新された
    /// </summary>
    CurrentCharacterUpdated = 1,

    /// <summary>
    /// 新しい月が始まった
    /// </summary>
    MonthStarted = 2,
  }
}