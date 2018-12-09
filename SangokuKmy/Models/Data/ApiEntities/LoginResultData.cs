using System;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  public class LoginResultData
  {
    /// <summary>
    /// ログインに成功したか
    /// </summary>
    [JsonProperty("isSucceed")]
    public bool IsSucceed { get; set; }

    /// <summary>
    /// ログイン失敗時のエラーコード
    /// </summary>
    [JsonProperty("errorCode")]
    public int ErrorCode { get; set; }

    /// <summary>
    /// アクセストークン
    /// </summary>
    [JsonProperty("accessToken")]
    public string AccessToken { get; set; }
  }
}
