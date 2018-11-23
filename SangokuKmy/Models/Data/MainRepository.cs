using SangokuKmy.Models.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data
{
  /// <summary>
  /// メインとなるリポジトリ
  /// </summary>
  public class MainRepository : IDisposable
  {
    /// <summary>
    /// データベースへアクセスするコンテキスト。アクセスが必要になった場合にのみ生成する
    /// </summary>
    private MainContext Context => this._context = this._context ?? new MainContext();
    private MainContext _context;

    /// <summary>
    /// 認証データ
    /// </summary>
    public AuthenticationDataRepository AuthenticationData => this._auth = this._auth ?? new AuthenticationDataRepository(this.container);
    private AuthenticationDataRepository _auth;

    private readonly IRepositoryContainer container;
    private static readonly ReaderWriterLock locker = new ReaderWriterLock();

    public MainRepository()
    {
      this.container = new Container(this);
    }

    public void Dispose()
    {
      this.Context?.Dispose();
    }

    /// <summary>
    /// 読み込み限定でロックをかける
    /// </summary>
    /// <returns>ロック解除オブジェクト</returns>
    public IDisposable ReadLock()
    {
      locker.AcquireReaderLock(20_000);
      return new Lock(Lock.LockType.ReadOnly);
    }

    /// <summary>
    /// 読み込みと書き込みが可能なロックをかける
    /// </summary>
    /// <returns>ロック解除オブジェクト</returns>
    public IDisposable WriteLock()
    {
      locker.AcquireWriterLock(20_000);
      return new Lock(Lock.LockType.ReadAndWrite);
    }

    private class Lock : IDisposable
    {
      public enum LockType
      {
        ReadOnly,
        ReadAndWrite
      }
      private readonly LockType type;

      public Lock(LockType type)
      {
        this.type = type;
      }

      public void Dispose()
      {
        try
        {
          switch (this.type)
          {
            case LockType.ReadOnly:
              locker.ReleaseReaderLock();
              break;
            case LockType.ReadAndWrite:
              locker.ReleaseWriterLock();
              break;
          }
        }
        catch (Exception ex)
        {
          // TODO: Error log
        }
      }
    }

    private class Container : IRepositoryContainer
    {
      private readonly MainRepository repo;

      public MainContext Context => this.repo.Context;

      public Container(MainRepository repo)
      {
        this.repo = repo;
      }
    }
  }

  /// <summary>
  /// リポジトリのコンテナ。リポジトリクラスをnewするときに渡すデータ
  /// </summary>
  public interface IRepositoryContainer
  {
    /// <summary>
    /// データベースへ直接アクセスするコンテキスト
    /// </summary>
    MainContext Context { get; }
  }
}
