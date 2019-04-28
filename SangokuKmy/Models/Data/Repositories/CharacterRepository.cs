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
    /// すべての武将を取得する
    /// </summary>
    /// <returns>武将</returns>
    public async Task<IReadOnlyList<Character>> GetAllAliveAsync()
    {
      try
      {
        return await this.container.Context.Characters
          .Where(c => !c.HasRemoved)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return null;
      }
    }

    /// <summary>
    /// すべての武将を取得する
    /// </summary>
    /// <returns>武将</returns>
    public async Task<IReadOnlyList<(Character Character, CharacterIcon Icon)>> GetAllAliveWithIconAsync()
    {
      try
      {
        var charaData = await this.container.Context.Characters
          .Where(c => !c.HasRemoved)
          .GroupJoin(this.container.Context.CharacterIcons, c => c.Id, i => i.CharacterId, (c, ics) => new { Character = c, Icons = ics, })
          .ToArrayAsync();
        return charaData.Select(d => (d.Character, d.Icons.GetMainOrFirst().Data)).ToArray();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 武将のキャッシュを取得する
    /// </summary>
    public async Task<IReadOnlyList<CharacterCache>> GetAllCachesAsync()
    {
      return await this.container.Context.Characters
        .Where(c => !c.HasRemoved)
        .Select(c => c.ToCache())
        .ToArrayAsync();
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
    /// IDから武将を取得する
    /// </summary>
    /// <returns>武将</returns>
    /// <param name="ids">ID</param>
    public async Task<IReadOnlyList<Character>> GetByIdAsync(IEnumerable<uint> ids)
    {
      try
      {
        return await this.container.Context.Characters
          .Where(c => ids.Contains(c.Id))
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 指定した名前の武将がいないか調べる
    /// </summary>
    /// <returns>武将</returns>
    /// <param name="id">ID</param>
    public async Task<bool> IsAlreadyExistsAsync(string name, string aliasId)
    {
      try
      {
        return await this.container.Context.Characters
          .AnyAsync(c => c.Name == name || c.AliasId == aliasId);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
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
    /// 武将を追加する
    /// </summary>
    /// <param name="character">武将</param>
    public async Task AddAsync(Character character)
    {
      try
      {
        await this.container.Context.Characters.AddAsync(character);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 武将を削除する
    /// </summary>
    /// <param name="character">武将</param>
    public void Remove(Character character)
    {
      try
      {
        this.container.Context.Characters.Remove(character);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 武将のログを追加する
    /// </summary>
    /// <param name="log">ログ</param>
    public async Task AddCharacterLogAsync(CharacterLog log)
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
    /// 武将のログを追加する
    /// </summary>
    /// <param name="log">ログ</param>
    public async Task AddCharacterLogAsync(IEnumerable<CharacterLog> logs)
    {
      try
      {
        await this.container.Context.CharacterLogs.AddRangeAsync(logs);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 武将のログを追加する
    /// </summary>
    /// <param name="characterId">武将ID</param>
    /// <param name="log">ログ</param>
    [Obsolete("characterId引数は不要")]
    public async Task AddCharacterLogAsync(uint characterId, CharacterLog log)
      => await this.AddCharacterLogAsync(log);

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
          .OrderByDescending(l => l.DateTime)
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
    /// 武将のログを取得
    /// </summary>
    /// <returns>ログのリスト</returns>
    /// <param name="characterId">武将ID</param>
    /// <param name="count">取得数</param>
    public async Task<IReadOnlyList<CharacterLog>> GetCharacterLogsAsync(uint characterId, uint sinceId, int count)
    {
      try
      {
        return (await this.container.Context.CharacterLogs
          .Where(l => l.CharacterId == characterId)
          .Where(l => l.Id < sinceId)
          .OrderByDescending(l => l.DateTime)
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
          .OrderByDescending(l => l.DateTime)
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
          .Where(i => i.CharacterId == characterId && i.IsAvailable)
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
    /// 武将のアイコンを追加する
    /// </summary>
    /// <param name="icon">アイコン</param>
    public async Task AddCharacterIconAsync(CharacterIcon icon)
    {
      try
      {
        await this.container.Context.CharacterIcons.AddAsync(icon);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// デフォルトのアイコンデータを取得する
    /// </summary>
    /// <param name="defaultIconId">アイコンID</param>
    /// <returns>アイコン</returns>
    public async Task<Optional<DefaultIconData>> GetDefaultIconByIdAsync(uint defaultIconId)
    {
      try
      {
        return await this.container.Context.DefaultIconData.FindAsync(defaultIconId).ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task AddFormationAsync(Formation formation)
    {
      try
      {
        await this.container.Context.Formations.AddAsync(formation);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public async Task<Formation> GetFormationAsync(uint characterId, FormationType type)
    {
      try
      {
        var old = await this.container.Context.Formations.FirstOrDefaultAsync(f => f.CharacterId == characterId && f.Type == type);
        if (old == null)
        {
          old = new Formation
          {
            CharacterId = characterId,
            Type = type,
            Level = 1,
          };
          await this.container.Context.Formations.AddAsync(old);
        }
        return old;
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task<IReadOnlyList<Formation>> GetCharacterFormationsAsync(uint characterId)
    {
      try
      {
        return await this.container.Context.Formations.Where(f => f.CharacterId == characterId).ToArrayAsync();
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
        await this.container.RemoveAllRowsAsync(typeof(Formation));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
