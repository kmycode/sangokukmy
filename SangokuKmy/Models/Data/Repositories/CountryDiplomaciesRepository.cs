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
          .Where(ca => ca.IsPublic && ca.Status != CountryAllianceStatus.ChangeRequestingValue)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
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
        this.container.Error(ex);
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
          .Where(ca => ca.Status != CountryAllianceStatus.ChangeRequestingValue)
          .FirstOrDefaultAsync(ca => (ca.RequestedCountryId == country1 && ca.InsistedCountryId == country2) ||
                                     (ca.RequestedCountryId == country2 && ca.InsistedCountryId == country1))
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 特定の国同士の同盟関係を取得
    /// </summary>
    /// <returns>すべての同盟関係</returns>
    /// <param name="country1">国ID</param>
    /// <param name="country2">国ID</param>
    public async Task<Optional<CountryAlliance>> GetCountryAllianceChangingValueAsync(uint country1, uint country2)
    {
      try
      {
        return await this.container.Context.CountryAlliances
          .Where(ca => ca.Status == CountryAllianceStatus.ChangeRequestingValue)
          .FirstOrDefaultAsync(ca => (ca.RequestedCountryId == country1 && ca.InsistedCountryId == country2) ||
                                     (ca.RequestedCountryId == country2 && ca.InsistedCountryId == country1))
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
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
        this.container.Error(ex);
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
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 同盟関係を設定する
    /// </summary>
    /// <param name="alliance">新しい同盟関係</param>
    public async Task SetAllianceChangingValueAsync(CountryAlliance alliance)
    {
      try
      {
        var old = await this.GetCountryAllianceChangingValueAsync(alliance.RequestedCountryId, alliance.InsistedCountryId);
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
        this.container.Error(ex);
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
        this.container.Error(ex);
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
        this.container.Error(ex);
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
        this.container.Error(ex);
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
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// すべての攻略を取得
    /// </summary>
    /// <returns>公開されている同盟情報</returns>
    public async Task<IReadOnlyList<TownWar>> GetAllTownWarsAsync()
    {
      try
      {
        return await this.container.Context.TownWars
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// すべての開戦準備中の攻略を取得
    /// </summary>
    public async Task<IReadOnlyList<TownWar>> GetReadyTownWarsAsync()
    {
      try
      {
        return await this.container.Context.TownWars
          .Where(ca => ca.Status == TownWarStatus.InReady)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 特定都市の攻略戦争を取得する
    /// </summary>
    /// <returns>戦争データ</returns>
    /// <param name="townId">都市</param>
    /// <param name="country1">国１</param>
    /// <param name="country2">国２</param>
    public async Task<Optional<TownWar>> GetTownWarAsync(uint country1, uint country2, uint townId)
    {
      try
      {
        return await this.container.Context.TownWars
          .FirstOrDefaultAsync(ca => ((ca.RequestedCountryId == country1 && ca.InsistedCountryId == country2) ||
                                      (ca.RequestedCountryId == country2 && ca.InsistedCountryId == country1)) &&
                                     ca.TownId == townId)
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 攻略を設定する
    /// </summary>
    /// <param name="war">新しい戦争</param>
    public async Task SetTownWarAsync(TownWar war)
    {
      try
      {
        var old = await this.GetTownWarAsync(war.RequestedCountryId, war.InsistedCountryId, war.TownId);
        old.Some(o =>
        {
          this.container.Context.TownWars.Remove(o);
        });
        if (war.Status != TownWarStatus.None)
        {
          await this.container.Context.TownWars.AddAsync(war);
        }
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 指定した国の外交データを全削除する
    /// </summary>
    /// <param name="countryId">国ID</param>
    public void RemoveByCountryId(uint countryId)
    {
      try
      {
        this.container.Context.CountryAlliances.RemoveRange(this.container.Context.CountryAlliances.Where(ca => ca.InsistedCountryId == countryId || ca.RequestedCountryId == countryId));
        this.container.Context.CountryWars.RemoveRange(this.container.Context.CountryWars.Where(ca => ca.InsistedCountryId == countryId || ca.RequestedCountryId == countryId));
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
        await this.container.RemoveAllRowsAsync(typeof(CountryAlliance));
        await this.container.RemoveAllRowsAsync(typeof(CountryWar));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
