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
    /// 都市IDから武将を取得
    /// </summary>
    /// <param name="townId">都市ID</param>
    /// <returns>その都市に滞在する武将</returns>
    public async Task<IReadOnlyCollection<(Character Character, CharacterIcon Icon)>> GetByTownIdAsync(uint townId)
    {
      try
      {
        return (await this.container.Context.Characters
          .Where(c => c.TownId == townId)
          .GroupJoin(this.container.Context.CharacterIcons,
            c => c.Id,
            i => i.CharacterId,
            (c, i) => new { Character = c, Icons = i, })
          .ToArrayAsync())
          .OrderBy(data => data.Character.LastUpdated)
          .Select(data =>
          {
            return (data.Character, data.Icons.GetMainOrFirst().Data);
          })
          .ToArray();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }

    /// <summary>
    /// 都市IDから守備中の武将を取得
    /// </summary>
    /// <param name="townId">都市ID</param>
    /// <returns>その都市に滞在する武将</returns>
    public async Task<IReadOnlyCollection<(Character Character, CharacterIcon Icon)>> GetDefendersByTownIdAsync(uint townId)
    {
      try
      {
        return (await this.container.Context.TownDefenders
          .Where(td => td.TownId == townId)
          .Join(this.container.Context.Characters,
            td => td.CharacterId,
            c => c.Id,
            (td, c) => new { Character = c, Order = td.Id, })
          .GroupJoin(this.container.Context.CharacterIcons,
            c => c.Character.Id,
            i => i.CharacterId,
            (c, i) => new { c.Character, Icons = i, c.Order, })
          .ToArrayAsync())
          .OrderByDescending(data => data.Order)
          .Select(data =>
           {
             return (data.Character, data.Icons.GetMainOrFirst().Data);
           })
          .ToArray();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }

    /// <summary>
    /// 守備武将を登録する
    /// </summary>
    /// <param name="characterId">武将ID</param>
    /// <param name="townId">都市ID</param>
    public async Task SetDefenderAsync(uint characterId, uint townId)
    {
      try
      {
        var old = await this.container.Context.TownDefenders.SingleOrDefaultAsync(td => td.CharacterId == characterId);
        if (old != null)
        {
          this.container.Context.TownDefenders.Remove(old);
        }
        var def = new TownDefender
        {
          CharacterId = characterId,
          TownId = townId,
        };
        await this.container.Context.TownDefenders.AddAsync(def);
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
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
        return await this.container.Context.CharacterLogs
          .Where(l => l.CharacterId == characterId)
          .OrderBy(l => l.DateTime)
          .Take(count)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }

    /// <summary>
    /// 武将のすべてのアイコンを取得
    /// </summary>
    /// <param name="characterId">武将ID</param>
    /// <returns>すべてのアイコン</returns>
    public async Task<IReadOnlyList<CharacterIcon>> GetCharacterAllIconsAsync(uint characterId)
    {
      try
      {
        return await this.container.Context.CharacterIcons
          .Where(i => i.CharacterId == characterId)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }

    /// <summary>
    /// 武将のアイコンをIDから取得する
    /// </summary>
    /// <param name="iconId">アイコンID</param>
    /// <returns>アイコン</returns>
    public async Task<CharacterIcon> GetCharacterIconByIdAsync(uint iconId)
    {
      try
      {
        return await this.container.Context.CharacterIcons.FindAsync(iconId);
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }
  }
}
