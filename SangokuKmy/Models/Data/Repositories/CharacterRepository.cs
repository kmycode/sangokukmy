﻿using System;
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
    /// すべての武将を取得する
    /// </summary>
    /// <returns>武将</returns>
    public async Task<IReadOnlyList<Character>> GetAllAsync()
    {
      try
      {
        return await this.container.Context.Characters.ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return null;
      }
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
        this.container.Error(ex);
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
        this.container.Error(ex);
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
        this.container.Error(ex);
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
        this.container.Error(ex);
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
        return (await this.container.Context.CharacterLogs
          .Where(l => l.CharacterId == characterId)
          .OrderByDescending(l => l.Id)
          .Take(count)
          .ToArrayAsync())
          .Reverse()
          .ToArray();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 武将の更新ログを取得
    /// </summary>
    /// <returns>ログのリスト</returns>
    /// <param name="characterId">武将ID</param>
    /// <param name="count">取得数</param>
    public async Task<IReadOnlyList<CharacterUpdateLog>> GetCharacterUpdateLogsAsync(int count)
    {
      try
      {
        return (await this.container.Context.CharacterUpdateLogs
          .OrderByDescending(l => l.Id)
          .GroupJoin(this.container.Context.Characters, ul => ul.CharacterId, c => c.Id, (ul, cs) => new { Log = ul, Names = cs.Select(c => c.Name), })
          .Take(count)
          .ToArrayAsync())
          .Select(ul =>
          {
            ul.Log.CharacterName = ul.Names.FirstOrDefault() ?? string.Empty;
            return ul.Log;
          })
          .Reverse()
          .ToArray();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 武将の更新ログを追加する
    /// </summary>
    /// <param name="log">追加するログ</param>
    public async Task AddCharacterUpdateLogAsync(CharacterUpdateLog log)
    {
      try
      {
        await this.container.Context.CharacterUpdateLogs.AddAsync(log);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
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
        this.container.Error(ex);
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
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 国を削除する。指定した国に仕官した武将は、全員無所属になる
    /// </summary>
    /// <param name="countryId">国ID</param>
    public async Task<IReadOnlyList<Character>> RemoveCountryAsync(uint countryId)
    {
      try
      {
        var charas = await this.container.Context.Characters.Where(c => c.CountryId == countryId).ToListAsync();
        foreach (var character in charas)
        {
          character.CountryId = 0;
        }
        return charas;
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 内容をすべてリセットする
    /// </summary>
    public async Task ResetAsync()
    {
      try
      {
        await this.container.RemoveAllRowsAsync(typeof(Character));
        await this.container.RemoveAllRowsAsync(typeof(CharacterIcon));
        await this.container.RemoveAllRowsAsync(typeof(CharacterLog));
        await this.container.RemoveAllRowsAsync(typeof(CharacterUpdateLog));
        await this.container.RemoveAllRowsAsync(typeof(LogCharacterCache));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
