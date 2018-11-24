using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Common.Definitions
{
  /// <summary>
  /// アプリケーションの基本となる例外
  /// </summary>
  public class SangokuKmyException : Exception
  {
    /// <summary>
    /// ステータスコード
    /// </summary>
    public int StatusCode { get; }

    /// <summary>
    /// エラーコード
    /// </summary>
    public ErrorCode ErrorCode { get; }

    /// <summary>
    /// 追加データ
    /// </summary>
    public object AdditionalData { get; }
    
    public SangokuKmyException(Exception inner, int status, ErrorCode code, object data) : base("例外が発生しました Code=" + code, inner)
    {
      this.StatusCode = status;
      this.ErrorCode = code;
      this.AdditionalData = data;
    }

    public SangokuKmyException(Exception inner, int status, ErrorCode code) : this(inner, status, code, null)
    {
    }

    public SangokuKmyException(Exception inner, ErrorCode code) : this(inner, code.StatusCode, code)
    {
    }
  }
}
