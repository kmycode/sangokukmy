using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SangokuKmy.Models.Data.Entities;
namespace SangokuKmy.Models.Data.ApiEntities
{
  public class ApiData<T>
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
    public T Data { get; set; }
  }

  public static class ApiData
  {
    private static ApiData<DT> From<DT>(int type, DT data)
    {
      return new ApiData<DT>
      {
        Type = type,
        Data = data,
      };
    }

    public static ApiData<ApiDateTime> From(ApiDateTime data) => From(1, data);
    public static ApiData<MapLog> From(MapLog data) => From(4, data);
    public static ApiData<AuthenticationData> From(AuthenticationData data) => From(6, data);
    public static ApiData<ApiError> From(ApiError data) => From(7, data);
  }
}
