using System;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using SangokuKmy.Models.Data.Entities.Caches;
using System.Collections.Generic;

namespace SangokuKmy.Models.Data.Repositories
{
  public class CharacterRepository
  {
    private readonly IRepositoryContainer container;

    public CharacterRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// 武将のキャッシュを取得する
    /// </summary>
    public async Task<IEnumerable<CharacterCache>> GetAllCachesAsync()
    {
      return await this.container.Context.Characters.Select(c => c.ToCache()).ToListAsync();
    }

    /// <summary>
    /// エイリアスIDからIDを取得
    /// </summary>
    /// <returns>武将ID</returns>
    /// <param name="aliasId">エイリアスID</param>
    public async Task<uint> GetIdByAliasIdAsync(string aliasId)
    {
      try
      {
        return (await this.GetByAliasIdAsync(aliasId)).Data?.Id ?? (uint)0;
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return 0;
      }
    }

    /// <summary>
    /// IDから武将を取得する
    /// </summary>
    /// <returns>武将</returns>
    /// <param name="id">ID</param>
    public async Task<Optional<Character>> GetByIdAsync(uint id)
    {
      try
      {
        return await this.container.Context.Characters
          .FindAsync(id)
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return Optional<Character>.Null();
      }
    }

    /// <summary>
    /// エイリアスIDから武将を取得する
    /// </summary>
    /// <returns>武将</returns>
    /// <param name="aliasId">エイリアスID</param>
    public async Task<Optional<Character>> GetByAliasIdAsync(string aliasId)
    {
      try
      {
        return await this.container.Context.Characters
          .FirstOrDefaultAsync(c => c.AliasId == aliasId)
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return Optional<Character>.Null();
      }
    }
  }
}
