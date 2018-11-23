using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Repositories
{
  /// <summary>
  /// 認証データのリポジトリ
  /// </summary>
  public class AuthenticationDataRepository
  {
    private readonly IRepositoryContainer container;

    /// <summary>
    /// データベースのキャッシュ
    /// </summary>
    private DatabaseCache<AuthenticationData> Cache
    {
      get
      {
        if (_cache == null)
        {
          this.InitializeCache();
        }
        return _cache;
      }
    }
    private static DatabaseCache<AuthenticationData> _cache;

    public AuthenticationDataRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// データベースのキャッシュを初期化
    /// </summary>
    private void InitializeCache()
    {
      try
      {
        using (this.container.ReadLock())
        {
          _cache = new DatabaseCache<AuthenticationData>(this.container.Context.AuthenticationData);
        }
      }
      catch
      {
        throw new SangokuKmyException(ErrorCode.DatabaseError);
      }
    }

    /// <summary>
    /// 古いデータを消去する
    /// </summary>
    private void CleanUpOldData()
    {
      try
      {
        using (this.container.WriteLock()) {
        var now = DateTime.Now;

          this.Cache.Remove(a => a.ExpirationTime < now);
          this.container.Context.SaveChanges();
        }
      }
      catch
      {
        throw new SangokuKmyException(ErrorCode.DatabaseError);
      }
    }

    /// <summary>
    /// データをトークンから検索する
    /// </summary>
    /// <param name="token">トークン</param>
    /// <returns>認証データ</returns>
    public AuthenticationData FindByToken(string token)
    {
      this.CleanUpOldData();
      using (this.container.ReadLock()) {
        return this.Cache.SingleOrDefault(a => a.AccessToken == token);
      }
    }
  }
}
