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
        this.container.Error(ex);
        return Optional<Town>.Null();
      }
    }

    /// <summary>
    /// すべての都市を取得する
    /// </summary>
    /// <returns>都市</returns>
    public async Task<IReadOnlyList<Town>> GetAllAsync()
    {
      try
      {
        return await this.container.Context.Towns
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
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
        this.container.Error(ex);
        return null;
      }
    }

    /// <summary>
    /// 指定した国の都市数を数える
    /// </summary>
    /// <returns>都市数</returns>
    /// <param name="countryId">国ID</param>
    public async Task<int> CountByCountryIdAsync(uint countryId)
    {
      try
      {
        return await this.container.Context.Towns.CountAsync(t => t.CountryId == countryId);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// すべての国が指定した国に支配されてるか調べる
    /// </summary>
    /// <returns>支配されているか</returns>
    /// <param name="countryId">国ID</param>
    public async Task<bool> IsUnifiedAsync(uint countryId)
    {
      try
      {
        var a = await this.container.Context.Towns.CountAsync(t => t.CountryId == countryId);
        return await this.container.Context.Towns.AllAsync(t => t.CountryId == countryId);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 都市IDから武将を取得
    /// </summary>
    /// <param name="townId">都市ID</param>
    /// <returns>その都市に滞在する武将</returns>
    public async Task<IReadOnlyCollection<Character>> GetCharactersAsync(uint townId)
    {
      try
      {
        return (await this.container.Context.Characters
          .Where(c => c.TownId == townId && !c.HasRemoved)
          .OrderBy(c => c.LastUpdated)
          .ToArrayAsync());
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 都市IDから武将を取得
    /// </summary>
    /// <param name="townId">都市ID</param>
    /// <returns>その都市に滞在する武将</returns>
    public async Task<IReadOnlyCollection<(Character Character, CharacterIcon Icon, IReadOnlyList<CharacterCommand> Commands)>> GetCharactersWithIconAsync(uint townId)
    {
      try
      {
        var system = await this.container.Context.SystemData.FirstAsync();
        return (await this.container.Context.Characters
          .Where(c => c.TownId == townId && !c.HasRemoved)
          .GroupJoin(this.container.Context.CharacterIcons,
            c => c.Id,
            i => i.CharacterId,
            (c, i) => new { Character = c, Icons = i, })
          .GroupJoin(this.container.Context.CharacterCommands.Where(c => c.IntGameDateTime <= system.IntGameDateTime + 8)
              .GroupJoin(this.container.Context.CharacterCommandParameters,
                c => c.Id,
                p => p.CharacterCommandId,
                (c, ps) => new { Command = c, Parameters = ps, }),
            c => c.Character.Id,
            c => c.Command.CharacterId,
            (c, cs) => new { c.Character, c.Icons, Commands = cs.ToArray(), })
          .ToArrayAsync())
          .OrderBy(data => data.Character.LastUpdated)
          .Select(data =>
          {
            return (data.Character, data.Icons.GetMainOrFirst().Data, (IReadOnlyList<CharacterCommand>)data.Commands.Select(c =>
            {
              c.Command.SetParameters(c.Parameters);
              return c.Command;
            }).ToArray());
          })
          .ToArray();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 都市IDから守備中の武将を取得
    /// </summary>
    /// <param name="townId">都市ID</param>
    /// <returns>その都市に滞在する武将</returns>
    public async Task<IReadOnlyCollection<(Character Character, CharacterIcon Icon)>> GetDefendersAsync(uint townId)
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
        this.container.Error(ex);
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
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 守備武将を削除する
    /// </summary>
    /// <param name="characterId">武将ID</param>
    public void RemoveDefender(uint characterId)
    {
      try
      {
        var old = this.container.Context.TownDefenders
          .Where(td => td.CharacterId == characterId);
        this.container.Context.TownDefenders.RemoveRange(old);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 初期化に使うデータを取得
    /// </summary>
    public async Task<IReadOnlyList<InitialTown>> GetAllInitialTownsAsync()
    {
      try
      {
        return await this.container.Context.InitialTowns.ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 都市を追加する
    /// </summary>
    /// <param name="towns">都市</param>
    public async Task AddTownsAsync(IEnumerable<Town> towns)
    {
      try
      {
        await this.container.Context.AddRangeAsync(towns);
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
        await this.container.RemoveAllRowsAsync(typeof(Town));
        await this.container.RemoveAllRowsAsync(typeof(TownDefender));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
