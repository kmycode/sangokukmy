using Microsoft.EntityFrameworkCore;
using SangokuKmy.Common;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Repositories
{
  public class ReinforcementRepository
  {
    private readonly IRepositoryContainer container;

    public ReinforcementRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    public async Task AddAsync(Reinforcement forcement)
    {
      try
      {
        var old = await this.container.Context.Reinforcements
          .FirstOrDefaultAsync(f => f.CharacterId == forcement.CharacterId && f.CharacterCountryId == forcement.CharacterCountryId && f.RequestedCountryId == forcement.RequestedCountryId);
        if (old != null)
        {
          this.container.Context.Reinforcements.Remove(old);
        }
        await this.container.Context.Reinforcements.AddAsync(forcement);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public async Task<IReadOnlyList<Reinforcement>> GetByCharacterIdAsync(uint characterId)
    {
      try
      {
        return await this.container.Context.Reinforcements
          .Where(f => f.CharacterId == characterId)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task<IReadOnlyList<Reinforcement>> GetByCountryIdAsync(uint countryId)
    {
      try
      {
        return await this.container.Context.Reinforcements
          .Where(f => f.CharacterCountryId == countryId || f.RequestedCountryId == countryId)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public void RemoveByCountryId(uint countryId)
    {
      try
      {
        this.container.Context.Reinforcements
          .RemoveRange(this.container.Context.Reinforcements
            .Where(r => r.CharacterCountryId == countryId));
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
        await this.container.RemoveAllRowsAsync(typeof(Reinforcement));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
