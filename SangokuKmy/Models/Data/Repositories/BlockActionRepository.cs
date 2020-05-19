using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Common;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Data.Repositories
{
  public class BlockActionRepository
  {
    private readonly IRepositoryContainer container;

    public BlockActionRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    public async Task<IReadOnlyList<BlockActionType>> GetAvailableTypesAsync(uint charaId)
    {
      try
      {
        var now = DateTime.Now;
        return await this.container.Context.BlockActions.Where(b => b.ExpiryDate > now && b.CharacterId == charaId).Select(b => b.Type).ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task<Optional<BlockAction>> GetAsync(uint charaId, BlockActionType type)
    {
      try
      {
        var now = DateTime.Now;
        return await this.container.Context.BlockActions.FirstOrDefaultAsync(b => b.ExpiryDate > now && b.CharacterId == charaId && b.Type == type).ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task<bool> IsBlockedAsync(uint charaId, BlockActionType type)
    {
      try
      {
        var now = DateTime.Now;
        return await this.container.Context.BlockActions.AnyAsync(b => b.ExpiryDate > now && b.CharacterId == charaId && b.Type == type);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task AddAsync(BlockAction action)
    {
      try
      {
        await this.container.Context.BlockActions.AddAsync(action);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public void Remove(BlockAction action)
    {
      try
      {
        this.container.Context.BlockActions.Remove(action);
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
        await this.container.RemoveAllRowsAsync(typeof(BlockAction));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
