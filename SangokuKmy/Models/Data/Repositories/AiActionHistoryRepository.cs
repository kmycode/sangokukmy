using Microsoft.EntityFrameworkCore;
using Nito.AsyncEx;
using SangokuKmy.Models.Common;
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
    private static List<AiBattleHistory> battleCache = null;
    private readonly IRepositoryContainer container;

    public AiActionHistoryRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    public async Task AddAsync(AiActionHistory history)
    {
      await this.InitializeCacheAsync();
      try
      {
        using (await locker.WriterLockAsync())
        {
          await this.container.Context.AiActionHistories.AddAsync(history);
        }
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public async Task AddAsync(AiBattleHistory history)
    {
      await this.InitializeCacheAsync();
      try
      {
        using (await locker.WriterLockAsync())
        {
          await this.container.Context.AiBattleHistories.AddAsync(history);
          battleCache.Add(history);
        }
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public async Task<float> GetMinRicePriceAsync(uint charaId, int minGameDateTime)
    {
      try
      {
        var targets = this.container.Context.AiActionHistories
          .Where(a => a.CharacterId == charaId && a.IntGameDateTime >= minGameDateTime);
        if (!targets.Any())
        {
          return default;
        }
        return await targets.MinAsync(a => a.IntRicePrice) / Config.RicePriceBase;
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task<float> GetMaxRicePriceAsync(uint charaId, int minGameDateTime)
    {
      try
      {
        var targets = this.container.Context.AiActionHistories
          .Where(a => a.CharacterId == charaId && a.IntGameDateTime >= minGameDateTime);
        if (!targets.Any())
        {
          return default;
        }
        return await targets.MaxAsync(a => a.IntRicePrice) / Config.RicePriceBase;
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task<IReadOnlyList<AiBattleHistory>> GetAsync(uint countryId, uint townId, uint townCountryId)
    {
      await this.InitializeCacheAsync();
      using (await locker.ReaderLockAsync())
      {
        return battleCache.Where(h => h.CountryId == countryId && h.TownId == townId && h.TownCountryId == townCountryId).ToArray();
      }
    }

    public async Task<IReadOnlyList<AiBattleHistory>> GetAsync(IEnumerable<uint> countryId, uint townId, uint townCountryId)
    {
      await this.InitializeCacheAsync();
      using (await locker.ReaderLockAsync())
      {
        return battleCache.Where(h => countryId.Contains(h.CountryId) && h.TownId == townId && h.TownCountryId == townCountryId).ToArray();
      }
    }

    public async Task<IReadOnlyList<AiBattleHistory>> GetAsync(IEnumerable<uint> countryId, AiBattleTownType townType, uint townCountryId)
    {
      await this.InitializeCacheAsync();
      using (await locker.ReaderLockAsync())
      {
        return battleCache.Where(h => countryId.Contains(h.CountryId) && h.TownType.HasFlag(townType) && h.TownCountryId == townCountryId).ToArray();
      }
    }

    public async Task<IReadOnlyList<AiBattleHistory>> GetAsync(uint countryId, AiBattleTownType townType, IEnumerable<uint> townCountryId)
    {
      await this.InitializeCacheAsync();
      using (await locker.ReaderLockAsync())
      {
        return battleCache.Where(h => countryId == h.CountryId && h.TownType.HasFlag(townType) && townCountryId.Contains(h.TownCountryId)).ToArray();
      }
    }

    private async Task InitializeCacheAsync()
    {
      if (battleCache != null)
      {
        return;
      }
      try
      {
        using (await locker.WriterLockAsync())
        {
          battleCache = await this.container.Context.AiBattleHistories.ToListAsync();
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
        await this.container.RemoveAllRowsAsync(typeof(AiActionHistory));
        if (battleCache != null)
        {
          using (await locker.WriterLockAsync())
          {
            battleCache.Clear();
          }
        }
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
