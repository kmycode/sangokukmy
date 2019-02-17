using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Streamings
{
  public class AnonymousStreaming : StreamingBase
  {
    public static AnonymousStreaming Default => _default = _default ?? new AnonymousStreaming();
    private static AnonymousStreaming _default;

    /// <summary>
    /// ストリーミングの対象を追加する
    /// </summary>
    /// <param name="response">レスポンス</param>
    public void Add(HttpResponse response)
    {
      this.Add(response, null);
    }

    public void Add(HttpResponse response, Action onRemoved)
    {
      this.CleanAbortedResponses();

      var data = new StreamingData<object>
      {
        AuthData = null,
        Response = response,
        ExtraData = null,
      };
      if (onRemoved != null)
      {
        data.Removed += (sender, e) => onRemoved();
      }
      this.Add(data);
    }

    /// <summary>
    /// 全員にデータを送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    public async Task SendAllAsync<T>(ApiData<T> data) => await this.SendAsync(data, c => true);

    /// <summary>
    /// 全員にデータを送信する
    /// </summary>
    /// <param name="data">送信するデータ</param>
    public async Task SendAllAsync<T>(IEnumerable<ApiData<T>> data) => await this.SendAsync(data, c => true);
  }
}
