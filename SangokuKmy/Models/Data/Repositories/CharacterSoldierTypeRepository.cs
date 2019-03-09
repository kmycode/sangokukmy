using System;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Data.Entities;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
namespace SangokuKmy.Models.Data.Repositories
{
  public class CharacterSoldierTypeRepository
  {
    private readonly IRepositoryContainer container;

    public CharacterSoldierTypeRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// IDから兵種を取得する
    /// </summary>
    /// <returns>兵種</returns>
    /// <param name="id">ID</param>
    public async Task<Optional<CharacterSoldierType>> GetByIdAsync(uint id)
    {
      try
      {
        return await this.container.Context.CharacterSoldierTypes
          .FindAsync(id)
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 武将IDから兵種を取得する
    /// </summary>
    /// <returns>兵種</returns>
    /// <param name="id">武将ID</param>
    public async Task<IEnumerable<CharacterSoldierType>> GetByCharacterIdAsync(uint id)
    {
      try
      {
        return await this.container.Context.CharacterSoldierTypes
          .Where(s => s.CharacterId == id)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 兵種を追加する
    /// </summary>
    /// <param name="type">兵種</param>
    public async Task AddAsync(CharacterSoldierType type)
    {
      try
      {
        await this.container.Context.CharacterSoldierTypes
          .AddAsync(type);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 兵種を更新する
    /// </summary>
    /// <param name="type">兵種</param>
    public void Set(CharacterSoldierType type)
    {
      try
      {
        this.container.Context.CharacterSoldierTypes.Attach(type);
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
        await this.container.RemoveAllRowsAsync(typeof(CharacterSoldierType));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
