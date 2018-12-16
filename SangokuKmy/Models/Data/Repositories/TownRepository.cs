using System;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data.Entities;
using System.Collections;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Models.Data.ApiEntities;
using System.Linq;

namespace SangokuKmy.Models.Data.Repositories
{
  public class TownRepository
  {
    private readonly IRepositoryContainer container;

    public TownRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// IDから都市を取得する
    /// </summary>
    /// <returns>都市</returns>
    /// <param name="id">ID</param>
    public async Task<Optional<Town>> GetByIdAsync(uint id)
    {
      try
      {
        return await this.container.Context.Towns
          .FindAsync(id)
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return Optional<Town>.Null();
      }
    }

    /// <summary>
    /// すべての都市を取得する
    /// </summary>
    /// <returns>都市</returns>
    public async Task<IReadOnlyCollection<Town>> GetAllAsync()
    {
      try
      {
        return await this.container.Context.Towns.ToArrayAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return null;
      }
    }

    /// <summary>
    /// すべての都市を、誰でも見れるデータのみを抽出して取得する
    /// </summary>
    /// <returns>都市</returns>
    public async Task<IReadOnlyCollection<TownForAnonymous>> GetAllForAnonymousAsync()
    {
      try
      {
        return await this.container.Context.Towns.Select(t => new TownForAnonymous(t)).ToArrayAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return null;
      }
    }
  }
}
