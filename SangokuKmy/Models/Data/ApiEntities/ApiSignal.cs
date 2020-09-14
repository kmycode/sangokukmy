using System;
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

    /// <summary>
    /// 能力が上昇した
    /// </summary>
    AttributeUp = 3,

    /// <summary>
    /// ストリーミングの初期データの送信完了
    /// </summary>
    EndOfStreamingInitializeData = 4,

    /// <summary>
    /// 部隊が削除された
    /// </summary>
    UnitRemoved = 5,

    /// <summary>
    /// 部隊集合された
    /// </summary>
    UnitGathered = 6,

    /// <summary>
    /// リセットされた
    /// </summary>
    Reseted = 7,

    /// <summary>
    /// 守備で戦闘があった
    /// </summary>
    DefenderBattled = 8,

    /// <summary>
    /// コマンドコメントが更新された
    /// </summary>
    CommandCommentUpdated = 9,

    /// <summary>
    /// 謹慎された
    /// </summary>
    StopCommand = 10,

    /// <summary>
    /// 部隊から除隊された
    /// </summary>
    UnitDischarged = 11,

    /// <summary>
    /// オンライン情報
    /// </summary>
    CharacterOnline = 12,

    /// <summary>
    /// 国変更時の、ストリーミングの初期データの送信開始
    /// </summary>
    StartStreamingNewCountryInitializationData = 13,
  }
}
