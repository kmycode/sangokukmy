using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Data.Repositories
{
  public class CountryRepository
  {
    private readonly IRepositoryContainer container;

    public CountryRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// すべての国を取得する
    /// </summary>
    /// <returns>すべての国</returns>
    public async Task<IEnumerable<Country>> GetAllAsync()
    {
      try
      {
        return await this.container.Context.Countries.ToArrayAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }

    /// <summary>
    /// IDから国を取得する
    /// </summary>
    /// <returns>国</returns>
    /// <param name="id">ID</param>
    public async Task<Optional<Country>> GetByIdAsync(uint id)
    {
      try
      {
        return await this.container.Context.Countries
          .FindAsync(id)
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return Optional<Country>.Null();
      }
    }
  }
}
