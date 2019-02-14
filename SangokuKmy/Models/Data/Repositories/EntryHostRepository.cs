using System;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Data.Entities;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Models.Data.Entities.Caches;
using System.Collections.Generic;

namespace SangokuKmy.Models.Data.Repositories
{
  public class EntryHostRepository
  {
    private readonly IRepositoryContainer container;

    public EntryHostRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// ホストを追加する
    /// </summary>
    /// <param name="host">新しいホストデータ</param>
    public async Task AddAsync(EntryHost host)
    {
      try
      {
        await this.container.Context.EntryHosts.AddAsync(host);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 指定したIPアドレスが存在するか調べる
    /// </summary>
    /// <param name="ip">IPアドレス</param>
    public async Task<bool> ExistsAsync(string ip)
    {
      try
      {
        return await this.container.Context.EntryHosts.AnyAsync(h => h.IpAddress == ip);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 内容をすべてリセットする
    /// </summary>
    public async Task ResetAsync()
    {
      try
      {
        await this.container.RemoveAllRowsAsync(typeof(EntryHost));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
