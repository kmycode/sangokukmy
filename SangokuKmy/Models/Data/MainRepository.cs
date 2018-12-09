using SangokuKmy.Models.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SangokuKmy.Models.Common.Definitions;

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
    private MainContext Context
    {
      get
      {
        try
        {
          return this._context = this._context ?? new MainContext();
        }
        catch (Exception ex)
        {
          ErrorCode.DatabaseError.Throw(ex);
          return null;
        }
      }
    }
    private MainContext _context;

    /// <summary>
    /// 認証データ
    /// </summary>
    public AuthenticationDataRepository AuthenticationData => this._auth = this._auth ?? new AuthenticationDataRepository(this.container);
    private AuthenticationDataRepository _auth;

    /// <summary>
    /// 武将
    /// </summary>
    public CharacterRepository Character => this._chara = this._chara ?? new CharacterRepository(this.container);
    private CharacterRepository _chara;

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
      return new ReadOnlyLock();
    }

    /// <summary>
    /// 読み込みと書き込みが可能なロックをかける
    /// </summary>
    /// <returns>ロック解除オブジェクト</returns>
    public IDisposable WriteLock()
    {
      locker.AcquireWriterLock(20_000);
      return new WritableLock();
    }

    private class ReadOnlyLock : IDisposable
    {
      public void Dispose()
      {
        try
        {
          locker.ReleaseReaderLock();
        }
        catch (Exception ex)
        {
          ErrorCode.LockFailedError.Throw(ex);
        }
      }
    }

    private class WritableLock : IDisposable
    {
      public void Dispose()
      {
        try
        {
          locker.ReleaseWriterLock();
        }
        catch (Exception ex)
        {
          ErrorCode.LockFailedError.Throw(ex);
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
