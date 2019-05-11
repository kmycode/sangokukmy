using Microsoft.EntityFrameworkCore;
using Nito.AsyncEx;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Repositories
{
  public class AiActionHistoryRepository
  {
    private static readonly AsyncReaderWriterLock locker = new AsyncReaderWriterLock();
    private static List<AiBattleHistory> cache = null;
    private readonly IRepositoryContainer container;

    public AiActionHistoryRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    public async Task AddAsync(AiBattleHistory history)
    {
      await this.InitializeCacheAsync();
      try
      {
        using (await locker.WriterLockAsync())
        {
          await this.container.Context.AiBattleHistories.AddAsync(history);
          cache.Add(history);
        }
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public async Task<IReadOnlyList<AiBattleHistory>> GetAsync(uint countryId, uint townId, uint townCountryId)
    {
      await this.InitializeCacheAsync();
      using (await locker.ReaderLockAsync())
      {
        return cache.Where(h => h.CountryId == countryId && h.TownId == townId && h.TownCountryId == townCountryId).ToArray();
      }
    }

    public async Task<IReadOnlyList<AiBattleHistory>> GetAsync(IEnumerable<uint> countryId, uint townId, uint townCountryId)
    {
      await this.InitializeCacheAsync();
      using (await locker.ReaderLockAsync())
      {
        return cache.Where(h => countryId.Contains(h.CountryId) && h.TownId == townId && h.TownCountryId == townCountryId).ToArray();
      }
    }

    public async Task<IReadOnlyList<AiBattleHistory>> GetAsync(IEnumerable<uint> countryId, AiBattleTownType townType, uint townCountryId)
    {
      await this.InitializeCacheAsync();
      using (await locker.ReaderLockAsync())
      {
        return cache.Where(h => countryId.Contains(h.CountryId) && h.TownType.HasFlag(townType) && h.TownCountryId == townCountryId).ToArray();
      }
    }

    public async Task<IReadOnlyList<AiBattleHistory>> GetAsync(uint countryId, AiBattleTownType townType, IEnumerable<uint> townCountryId)
    {
      await this.InitializeCacheAsync();
      using (await locker.ReaderLockAsync())
      {
        return cache.Where(h => countryId == h.CountryId && h.TownType.HasFlag(townType) && townCountryId.Contains(h.TownCountryId)).ToArray();
      }
    }

    private async Task InitializeCacheAsync()
    {
      if (cache != null)
      {
        return;
      }
      try
      {
        using (await locker.WriterLockAsync())
        {
          cache = await this.container.Context.AiBattleHistories.ToListAsync();
        }
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 内容をすべてリセットする
    /// </summary>
    public async Task ResetAsync()
    {
      try
      {
        await this.container.RemoveAllRowsAsync(typeof(AiBattleHistory));
        using (await locker.WriterLockAsync())
        {
          cache.Clear();
        }
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
