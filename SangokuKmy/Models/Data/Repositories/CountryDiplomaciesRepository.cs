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
    public async Task<IEnumerable<CountryAlliance>> GetCountryAllAlliancesAsync(uint countryId)
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
  }
}
