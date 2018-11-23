using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Common.Definitions
{
  /// <summary>
  /// アクセストークンのスコープ
  /// </summary>
  [Flags]
  public enum Scope : uint
  {
    All = 0b111_1111_1111_1111_1111_1111_1111_1111,
  }

  /// <summary>
  /// エラーコード
  /// </summary>
  public readonly struct ErrorCode
  {
    /// <summary>
    /// 原因不明の内部エラー
    /// </summary>
    public static ErrorCode InternalError { get; } = new ErrorCode(500, 1);

    /// <summary>
    /// データベース接続のエラー
    /// </summary>
    public static ErrorCode DatabaseError { get; } = new ErrorCode(503, 2);

    /// <summary>
    /// エラーコード
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// HTTPステータスコード
    /// </summary>
    public int StatusCode { get; }

    public ErrorCode(int status, int code)
    {
      this.Code = code;
      this.StatusCode = status;
    }
  }
}
