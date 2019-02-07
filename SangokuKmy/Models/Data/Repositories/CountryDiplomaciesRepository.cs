using System;
using System.Threading.Tasks;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using SangokuKmy.Common;

namespace SangokuKmy.Models.Data.Repositories
{
  public class CountryDiplomaciesRepository
  {
    private readonly IRepositoryContainer container;

    public CountryDiplomaciesRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// すべての公開されている同盟情報を取得
    /// </summary>
    /// <returns>公開されている同盟情報</returns>
    public async Task<IReadOnlyList<CountryAlliance>> GetAllPublicAlliancesAsync()
    {
      try
      {
        return await this.container.Context.CountryAlliances
          .Where(ca => ca.IsPublic)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }

    /// <summary>
    /// 特定の国の同盟関係をすべて取得
    /// </summary>
    /// <returns>すべての同盟関係</returns>
    /// <param name="countryId">国ID</param>
    public async Task<IReadOnlyList<CountryAlliance>> GetCountryAllAlliancesAsync(uint countryId)
    {
      try
      {
        return await this.container.Context.CountryAlliances
          .Where(ca => ca.RequestedCountryId == countryId || ca.InsistedCountryId == countryId)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }

    /// <summary>
    /// 特定の国同士の同盟関係を取得
    /// </summary>
    /// <returns>すべての同盟関係</returns>
    /// <param name="country1">国ID</param>
    /// <param name="country2">国ID</param>
    public async Task<Optional<CountryAlliance>> GetCountryAllianceAsync(uint country1, uint country2)
    {
      try
      {
        return await this.container.Context.CountryAlliances
          .FirstOrDefaultAsync(ca => (ca.RequestedCountryId == country1 && ca.InsistedCountryId == country2) ||
                                     (ca.RequestedCountryId == country2 && ca.InsistedCountryId == country1))
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }

    /// <summary>
    /// すべての破棄準備中の同盟を取得
    /// </summary>
    public async Task<IReadOnlyList<CountryAlliance>> GetBreakingAlliancesAsync()
    {
      try
      {
        return await this.container.Context.CountryAlliances
          .Where(ca => ca.Status == CountryAllianceStatus.InBreaking)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }

    /// <summary>
    /// 同盟関係を設定する
    /// </summary>
    /// <param name="alliance">新しい同盟関係</param>
    public async Task SetAllianceAsync(CountryAlliance alliance)
    {
      try
      {
        var old = await this.GetCountryAllianceAsync(alliance.RequestedCountryId, alliance.InsistedCountryId);
        old.Some(o =>
        {
          this.container.Context.CountryAlliances.Remove(o);
        });
        if (alliance.Status != CountryAllianceStatus.None)
        {
          await this.container.Context.CountryAlliances.AddAsync(alliance);
        }
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
      }
    }

    /// <summary>
    /// すべての戦争を取得
    /// </summary>
    /// <returns>公開されている同盟情報</returns>
    public async Task<IReadOnlyList<CountryWar>> GetAllWarsAsync()
    {
      try
      {
        return await this.container.Context.CountryWars
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }

    /// <summary>
    /// すべての開戦準備中の戦争を取得
    /// </summary>
    public async Task<IReadOnlyList<CountryWar>> GetReadyWarsAsync()
    {
      try
      {
        return await this.container.Context.CountryWars
          .Where(ca => ca.Status == CountryWarStatus.InReady)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }

    /// <summary>
    /// 国同士の戦争を取得する
    /// </summary>
    /// <returns>戦争データ</returns>
    /// <param name="country1">国１</param>
    /// <param name="country2">国２</param>
    public async Task<Optional<CountryWar>> GetCountryWarAsync(uint country1, uint country2)
    {
      try
      {
        return await this.container.Context.CountryWars
          .FirstOrDefaultAsync(ca => (ca.RequestedCountryId == country1 && ca.InsistedCountryId == country2) ||
                                     (ca.RequestedCountryId == country2 && ca.InsistedCountryId == country1))
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }

    /// <summary>
    /// 戦争を設定する
    /// </summary>
    /// <param name="war">新しい戦争</param>
    public async Task SetWarAsync(CountryWar war)
    {
      try
      {
        var old = await this.GetCountryWarAsync(war.RequestedCountryId, war.InsistedCountryId);
        old.Some(o =>
        {
          this.container.Context.CountryWars.Remove(o);
        });
        if (war.Status != CountryWarStatus.None)
        {
          await this.container.Context.CountryWars.AddAsync(war);
        }
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
      }
    }
  }
}
