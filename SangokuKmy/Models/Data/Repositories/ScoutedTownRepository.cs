using Microsoft.EntityFrameworkCore;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Repositories
{
  public class ScoutedTownRepository
  {
    private readonly IRepositoryContainer container;

    public ScoutedTownRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// 国が諜報したすべての都市データを取得する
    /// </summary>
    /// <returns>都市</returns>
    /// <param name="scoutingCountryId">諜報した国のID</param>
    public async Task<IReadOnlyList<ScoutedTown>> GetByScoutedCountryIdAsync(uint scoutingCountryId)
    {
      try
      {
        var towns = await this.container.Context.ScoutedTowns
          .Where(st => st.ScoutedCountryId == scoutingCountryId)
          .ToArrayAsync();
        foreach (var town in towns)
        {
          await this.SetScoutedTownParametersAsync(town);
        }
        return towns;
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// IDから都市を取得する
    /// </summary>
    /// <returns>都市</returns>
    /// <param name="id">ID</param>
    /// <param name="scoutingCountryId">諜報した国のID</param>
    public async Task<Optional<ScoutedTown>> GetByTownIdAsync(uint id, uint scoutingCountryId)
    {
      try
      {
        var town = await this.container.Context.ScoutedTowns
          .FirstOrDefaultAsync(st => st.ScoutedCountryId == scoutingCountryId && st.ScoutedTownId == id)
          .ToOptionalAsync();
        if (town.HasData)
        {
          await this.SetScoutedTownParametersAsync(town.Data);
        }
        return town;
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    private async Task SetScoutedTownParametersAsync(ScoutedTown t)
    {
      var characters = (await this.container.Context.ScoutedCharacters
        .Where(sc => sc.ScoutId == t.Id)
        .Join(this.container.Context.Characters,
          sc => sc.CharacterId,
          c => c.Id,
          (sc, c) => new { ScoutData = sc, Character = c, })
        .GroupJoin(this.container.Context.CharacterIcons,
          c => c.Character.Id,
          i => i.CharacterId,
          (c, i) => new { c.ScoutData, c.Character, Icons = i, })
        .ToArrayAsync())
        .Select(c =>
        {
          var data = new CharacterForAnonymous(c.Character, c.Icons.GetMainOrFirst().Data, CharacterShareLevel.Anonymous)
          {
            SoldierType = c.ScoutData.SoldierType,
            SoldierNumber = c.ScoutData.SoldierNumber
          };
          return data;
        });
      var defenders = (await this.container.Context.ScoutedDefenders
        .Where(sc => sc.ScoutId == t.Id)
        .Join(this.container.Context.Characters,
          sc => sc.CharacterId,
          c => c.Id,
          (sc, c) => new { ScoutData = sc, Character = c, })
        .GroupJoin(this.container.Context.CharacterIcons,
          c => c.Character.Id,
          i => i.CharacterId,
          (c, i) => new { c.ScoutData, c.Character, Icons = i, })
        .ToArrayAsync())
        .Select(c =>
        {
          var data = new CharacterForAnonymous(c.Character, c.Icons.GetMainOrFirst().Data, CharacterShareLevel.Anonymous)
          {
            SoldierType = c.ScoutData.SoldierType,
            SoldierNumber = c.ScoutData.SoldierNumber
          };
          return data;
        });
      t.Characters = characters;
      t.Defenders = defenders;
    }

    /// <summary>
    /// 諜報データを追加する
    /// </summary>
    /// <param name="town">追加するデータ</param>
    public async Task AddScoutAsync(ScoutedTown town)
    {
      try
      {
        var old = await this.container.Context.ScoutedTowns
          .FirstOrDefaultAsync(st => st.ScoutedCountryId == town.ScoutedCountryId && st.ScoutedTownId == town.ScoutedTownId);
        if (old != null)
        {
          this.container.Context.ScoutedCharacters.RemoveRange(
            this.container.Context.ScoutedCharacters.Where(sc => sc.ScoutId == old.Id));
          this.container.Context.ScoutedDefenders.RemoveRange(
            this.container.Context.ScoutedDefenders.Where(sd => sd.ScoutId == old.Id));
          this.container.Context.ScoutedTowns.Remove(old);
        }

        await this.container.Context.ScoutedTowns
          .AddAsync(town);
        await this.container.Context.SaveChangesAsync();

        // 都市の武将一覧、守備を記録
        var characters =
          (await this.container.Context.Characters
            .Where(c => c.TownId == town.ScoutedTownId)
            .Where(c => !c.HasRemoved)
            .ToArrayAsync())
            .Select(c => new ScoutedCharacter
            {
              CharacterId = c.Id,
              ScoutId = town.Id,
              SoldierType = c.ApiSoldierType,
              SoldierNumber = c.SoldierNumber,
            });
        var defenders =
          (await this.container.Context.TownDefenders
            .Where(td => td.TownId == town.ScoutedTownId)
            .Join(this.container.Context.Characters,
              td => td.CharacterId,
              c => c.Id,
              (td, c) => c)
            .ToArrayAsync())
            .Select(c => new ScoutedDefender
            {
              CharacterId = c.Id,
              ScoutId = town.Id,
              SoldierType = c.ApiSoldierType,
              SoldierNumber = c.SoldierNumber,
            });
        await this.container.Context.ScoutedCharacters.AddRangeAsync(characters);
        await this.container.Context.ScoutedDefenders.AddRangeAsync(defenders);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 武将データだけを削除する
    /// </summary>
    /// <param name="characterId">武将ID</param>
    public void RemoveCharacter(uint characterId)
    {
      try
      {
        this.container.Context.ScoutedCharacters
          .RemoveRange(this.container.Context.ScoutedCharacters.Where(c => c.CharacterId == characterId));
        this.container.Context.ScoutedDefenders
          .RemoveRange(this.container.Context.ScoutedDefenders.Where(d => d.CharacterId == characterId));
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
        await this.container.RemoveAllRowsAsync(typeof(ScoutedTown));
        await this.container.RemoveAllRowsAsync(typeof(ScoutedCharacter));
        await this.container.RemoveAllRowsAsync(typeof(ScoutedDefender));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
