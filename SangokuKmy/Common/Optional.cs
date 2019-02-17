using System;
using System.Threading.Tasks;
using SangokuKmy.Models.Common.Definitions;
namespace SangokuKmy.Common
{
  /// <summary>
  /// NULLになるかもしれない値をあらわす構造体
  /// </summary>
  public struct Optional<T> where T : class
  {
    /// <summary>
    /// データ
    /// </summary>
    public T Data { get; }

    /// <summary>
    /// この構造体はNULLでないデータを保持しているか
    /// </summary>
    public bool HasData => this.Data != null;

    /// <summary>
    /// 取得を試み、NULLなら例外を投げる
    /// </summary>
    /// <returns>取得したデータ</returns>
    /// <param name="error">エラーコード</param>
    public T GetOrError(ErrorCode error, object extraData = null)
    {
      if (!this.HasData)
      {
        if (extraData == null)
        {
          error.Throw();
        }
        else
        {
          error.Throw(extraData);
        }
      }
      return this.Data;
    }

    /// <summary>
    /// NULLの入った構造体を返す
    /// </summary>
    public static Optional<T> Null() => new Optional<T>(null);

    public Optional(T data)
    {
      this.Data = data;
    }

    /// <summary>
    /// データを保持していた時に、処理を実行する
    /// </summary>
    /// <returns>自身</returns>
    /// <param name="act">実行する処理</param>
    public Optional<T> Some(Action<T> act)
    {
      if (this.HasData) act(this.Data);
      return this;
    }

    /// <summary>
    /// データを保持していた時に、処理を実行し、終了を待機する
    /// </summary>
    /// <param name="act">実行する処理</param>
    public async Task<Optional<T>> SomeAsync(Func<T, Task> act)
    {
      if (this.HasData) await act(this.Data);
      return this;
    }

    /// <summary>
    /// データを保持していなかった時に、処理を実行する
    /// </summary>
    /// <returns>自身</returns>
    /// <param name="act">実行する処理</param>
    public Optional<T> None(Action act)
    {
      if (!this.HasData) act();
      return this;
    }
  }

  public static class OptionalExtensions
  {
    /// <summary>
    /// オブジェクトをOptionalに変換する
    /// </summary>
    /// <returns>変換されたOptional</returns>
    /// <param name="obj">変換するオブジェクト</param>
    /// <typeparam name="T">オブジェクトの型</typeparam>
    public static Optional<T> ToOptional<T>(this T obj) where T : class
    {
      return new Optional<T>(obj);
    }

    /// <summary>
    /// オブジェクトがNULLであればエラーを投げる
    /// </summary>
    /// <typeparam name="T">オブジェクトの型</typeparam>
    /// <param name="obj">調べるオブジェクト</param>
    /// <param name="error">nullだったときに投げるエラーコード</param>
    /// <param name="extraData">エラーの追加データ</param>
    /// <returns></returns>
    public static T Or<T>(this T obj, ErrorCode error, object extraData = null) where T : class
    {
      if (obj == null)
      {
        error.Throw(extraData);
      }
      return obj;
    }

    /// <summary>
    /// 非同期オブジェクトをOptionalに変換する
    /// </summary>
    /// <returns>Optionalに変換されたオブジェクト</returns>
    /// <param name="task">非同期タスク</param>
    /// <typeparam name="T">オブジェクトの型</typeparam>
    public static async Task<Optional<T>> ToOptionalAsync<T>(this Task<T> task) where T : class
    {
      await task;
      return task.Result.ToOptional();
    }

    /// <summary>
    /// 非同期Optionalの中身を取得するよう試み、NULLなら例外を投げる
    /// </summary>
    /// <returns>取得したデータ</returns>
    /// <param name="error">エラーコード</param>
    /// <typeparam name="T">データの型</typeparam>
    public static async Task<T> GetOrErrorAsync<T>(this Task<Optional<T>> optional, ErrorCode error, object extraData = null) where T : class
    {
      return (await optional).GetOrError(error, extraData);
    }

    /// <summary>
    /// 非同期Optionalの中身を取得して、NULLであってもそのまま返す
    /// </summary>
    /// <returns>取得したデータ</returns>
    /// <typeparam name="T">データの型</typeparam>
    public static async Task<T> GetUnsafelyAsync<T>(this Task<Optional<T>> optional) where T : class
    {
      return (await optional).Data;
    }

    /// <summary>
    /// 非同期Optionalの中身が存在すれば処理を実行する
    /// </summary>
    /// <returns>非同期Optional</returns>
    /// <param name="optional">非同期Optional</param>
    /// <param name="act">処理</param>
    public static async Task<Optional<T>> SomeAsync<T>(this Task<Optional<T>> optional, Action<T> act) where T : class
    {
      return (await optional).Some(act);
    }

    /// <summary>
    /// 非同期Optionalの中身が存在しなければ処理を実行する
    /// </summary>
    /// <returns>非同期Optional</returns>
    /// <param name="optional">非同期Optional</param>
    /// <param name="act">処理</param>
    public static async Task<Optional<T>> NoneAsync<T>(this Task<Optional<T>> optional, Action act) where T : class
    {
      return (await optional).None(act);
    }
  }
}
