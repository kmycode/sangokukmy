using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
namespace SangokuKmy.Models.Data.ApiEntities
{
  public class ApiError
  {
    /// <summary>
    /// エラーコード
    /// </summary>
    [JsonProperty("code")]
    public int Code { get; set; }

    /// <summary>
    /// 追加のデータ
    /// </summary>
    [JsonProperty("data")]
    public object Data { get; set; }
  }
}
