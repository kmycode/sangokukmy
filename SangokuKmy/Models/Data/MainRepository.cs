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
    private readonly ReaderWriterLock locker = new ReaderWriterLock();

    public MainRepository()
    {
      this.container = new Container(this);
    }

    public void Dispose()
    {
      this.Context?.Dispose();
    }

    private class Container : IRepositoryContainer
    {
      private readonly MainRepository repo;

      public MainContext Context => this.repo.Context;

      public Container(MainRepository repo)
      {
        this.repo = repo;
      }

      public IDisposable ReadLock() {
        this.repo.locker.AcquireReaderLock(20_000);
        return new Lock(this.repo.locker, Lock.LockType.ReadOnly);
      }

      public IDisposable WriteLock() {
        this.repo.locker.AcquireWriterLock(20_000);
        return new Lock(this.repo.locker, Lock.LockType.ReadAndWrite);
      }

      private class Lock : IDisposable {

        private readonly ReaderWriterLock locker;

        public enum LockType {
          ReadOnly,
          ReadAndWrite
        }
        private readonly LockType type;

        public Lock(ReaderWriterLock locker, LockType type) {
          this.locker = locker;
          this.type = type;
        }

        public void Dispose() {
          try {
            switch (this.type)
            {
              case LockType.ReadOnly:
                this.locker.ReleaseReaderLock();
                break;
              case LockType.ReadAndWrite:
                this.locker.ReleaseWriterLock();
                break;
            }
          }
          catch (Exception ex) {
            // TODO: Error log
          }
        }
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

    /// <summary>
    /// 読み込み限定でロックをかける
    /// </summary>
    /// <returns>ロック解除オブジェクト</returns>
    IDisposable ReadLock();

    /// <summary>
    /// 読み込みと書き込みが可能なロックをかける
    /// </summary>
    /// <returns>ロック解除オブジェクト</returns>
    IDisposable WriteLock();
  }
}
