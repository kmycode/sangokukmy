using Microsoft.EntityFrameworkCore;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Repositories
{
  public class CharacterItemRepository
  {
    private readonly IRepositoryContainer container;

    public CharacterItemRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    public async Task<IReadOnlyList<CharacterItem>> GetAllAsync()
    {
      try
      {
        return await this.container.Context.CharacterItems.ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task AddAsync(IEnumerable<CharacterItem> items)
    {
      try
      {
        await this.container.Context.CharacterItems.AddRangeAsync(items);
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
        await this.container.RemoveAllRowsAsync(typeof(CharacterItem));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
