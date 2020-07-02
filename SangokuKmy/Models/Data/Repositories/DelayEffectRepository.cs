using Microsoft.EntityFrameworkCore;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Repositories
{
  public class DelayEffectRepository
  {
    private readonly IRepositoryContainer container;

    public DelayEffectRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    public async Task<IReadOnlyList<DelayEffect>> GetAllAsync()
    {
      try
      {
        return await this.container.Context.DelayEffects.ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task<IReadOnlyList<DelayEffect>> GetByCharacterIdAsync(uint charaId)
    {
      try
      {
        return await this.container.Context.DelayEffects.Where(de => de.CharacterId == charaId).ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task AddAsync(DelayEffect effect)
    {
      try
      {
        await this.container.Context.DelayEffects.AddAsync(effect);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public void Remove(DelayEffect effect)
    {
      try
      {
        this.container.Context.DelayEffects.Remove(effect);
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
        await this.container.RemoveAllRowsAsync(typeof(DelayEffect));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
