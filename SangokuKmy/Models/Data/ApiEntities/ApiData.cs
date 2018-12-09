using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SangokuKmy.Models.Data.Entities;
namespace SangokuKmy.Models.Data.ApiEntities
{
  public class ApiData
  {
    /// <summary>
    /// 型の種類
    /// </summary>
    [JsonProperty("type")]
    public int Type { get; set; }

    /// <summary>
    /// 実際のデータ
    /// </summary>
    [JsonProperty("data")]
    public object Data { get; set; }

    public static ApiData From(object data)
    {
      var d = new ApiData
      {
        Data = data,
      };

      d.Type = data is ApiDateTime ? 1 :
                data is MapLog ? 4 :
                data is LoginResultData ? 5 :
                throw new ArgumentException("未定義の型です");

      return d;
    }
  }
}
