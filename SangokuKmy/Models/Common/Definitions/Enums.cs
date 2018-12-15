using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;

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
    /// サーバへの接続に失敗したエラー（クライアント側でのみ使用する）
    /// </summary>
    public static ErrorCode ServerConnectionFailedError { get; } = new ErrorCode(500, -1);

    /// <summary>
    /// 原因不明の内部エラー
    /// </summary>
    public static ErrorCode InternalError { get; } = new ErrorCode(500, 1);

    /// <summary>
    /// データベース接続のエラー
    /// </summary>
    public static ErrorCode DatabaseError { get; } = new ErrorCode(503, 2);

    /// <summary>
    /// データベースタイムアウトのエラー
    /// </summary>
    public static ErrorCode DatabaseTimeoutError { get; } = new ErrorCode(503, 3);

    /// <summary>
    /// ロック失敗のエラー
    /// </summary>
    public static ErrorCode LockFailedError { get; } = new ErrorCode(409, 4);

    /// <summary>
    /// ログインしている武将が見つからないエラー
    /// </summary>
    public static ErrorCode LoginCharacterNotFoundError { get; } = new ErrorCode(403, 5);

    /// <summary>
    /// ログイン時のパラメータが足りないエラー
    /// </summary>
    public static ErrorCode LoginParameterMissingError { get; } = new ErrorCode(400, 6);

    /// <summary>
    /// ログイン時のパラメータが間違っているエラー
    /// </summary>
    public static ErrorCode LoginParameterIncorrectError { get; } = new ErrorCode(401, 7);

    /// <summary>
    /// ログイントークンが間違っているエラー
    /// </summary>
    public static ErrorCode LoginTokenIncorrectError { get; } = new ErrorCode(401, 8);

    /// <summary>
    /// アクセストークンが空であるエラー
    /// </summary>
    public static ErrorCode LoginTokenEmptyError { get; } = new ErrorCode(401, 9);

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

    public void Throw()
    {
      throw new SangokuKmyException(this);
    }

    public void Throw(Exception original)
    {
      throw new SangokuKmyException(original, this);
    }

    public void Throw(Exception original, object data)
    {
      throw new SangokuKmyException(original, this.StatusCode, this, data);
    }

    public void Throw(Exception original, int statusCode)
    {
      throw new SangokuKmyException(original, statusCode, this);
    }

    public void Throw(Exception original, int statusCode, object data)
    {
      throw new SangokuKmyException(original, statusCode, this, data);
    }

    public override bool Equals(object obj)
    {
      if (obj is ErrorCode code)
      {
        return this.Code == code.Code;
      }
      return false;
    }

    public override int GetHashCode()
    {
      return this.Code.GetHashCode();
    }
  }

  public static class ErrorCodeExtensions {
    /// <summary>
    /// 例外がそのエラーコードを持っているか確認する
    /// </summary>
    /// <returns>例外のエラーコードが一致するか</returns>
    /// <param name="ex">例外</param>
    /// <param name="code">エラーコード</param>
    public static bool Is(this SangokuKmyException ex, ErrorCode code) {
      return ex.ErrorCode.Code == code.Code;
    }
  }
}
