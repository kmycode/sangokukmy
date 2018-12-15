﻿using SangokuKmy.Models.Common.Definitions;
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
        _cache = new DatabaseCache<AuthenticationData>(this.container.Context.AuthenticationData);
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
      }
    }

    /// <summary>
    /// 古いデータを消去する
    /// </summary>
    private async Task CleanUpOldDataAsync()
    {
      try
      {
        var now = DateTime.Now;

        var removed = this.Cache.Remove(this.container.Context.AuthenticationData, a => a.ExpirationTime < now);
        if (removed.Any())
        {
          await this.container.Context.SaveChangesAsync();
        }
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
      }
    }

    /// <summary>
    /// データをトークンから検索する
    /// </summary>
    /// <param name="token">トークン</param>
    /// <returns>認証データ</returns>
    public async Task<AuthenticationData> FindByTokenAsync(string token)
    {
      await this.CleanUpOldDataAsync();
      try
      {
        return this.Cache.SingleOrDefault(a => a.AccessToken == token);
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return null;
      }
    }

    /// <summary>
    /// 新しい認証結果を追加する
    /// </summary>
    /// <param name="data">認証データ</param>
    public async Task AddAsync(AuthenticationData data)
    {
      try
      {
        this.Cache.Add(this.container.Context.AuthenticationData, data);
        await this.container.Context.SaveChangesAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
      }
    }
  }
}
