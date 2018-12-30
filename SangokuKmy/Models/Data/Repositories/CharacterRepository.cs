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
    public async Task<IReadOnlyList<CharacterCache>> GetAllCachesAsync()
    {
      return await this.container.Context.Characters.Select(c => c.ToCache()).ToArrayAsync();
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

    /// <summary>
    /// 武将のログを追加する
    /// </summary>
    /// <param name="characterId">武将ID</param>
    /// <param name="log">ログ</param>
    public async Task AddCharacterLogAsync(uint characterId, CharacterLog log)
    {
      try
      {
        await this.container.Context.CharacterLogs.AddAsync(log);
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
      }
    }

    /// <summary>
    /// 武将のログを取得
    /// </summary>
    /// <returns>ログのリスト</returns>
    /// <param name="characterId">武将ID</param>
    /// <param name="count">取得数</param>
    public async Task<IReadOnlyList<CharacterLog>> GetCharacterLogsAsync(uint characterId, int count)
    {
      try
      {
        return await this.container.Context.CharacterLogs.OrderBy(l => l.DateTime).Take(count).ToArrayAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }
  }
}
