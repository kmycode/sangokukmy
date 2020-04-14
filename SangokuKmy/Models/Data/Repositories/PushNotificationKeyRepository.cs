using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Data.Repositories
{
  public class PushNotificationKeyRepository
  {
    private readonly IRepositoryContainer container;

    public PushNotificationKeyRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    public async Task<IReadOnlyList<PushNotificationKey>> GetAllAsync()
    {
      try
      {
        return await this.container.Context.PushNotificationKeys.ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task<IReadOnlyList<PushNotificationKey>> GetAsync(uint charaId)
    {
      try
      {
        return await this.container.Context.PushNotificationKeys.Where(k => k.CharacterId == charaId).ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task AddAsync(PushNotificationKey key)
    {
      try
      {
        await this.container.Context.PushNotificationKeys.AddAsync(key);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public void RemoveByCharacterId(uint charaId)
    {
      try
      {
        this.container.Context.PushNotificationKeys.RemoveRange(this.container.Context.PushNotificationKeys.Where(k => k.CharacterId == charaId));
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
        await this.container.RemoveAllRowsAsync(typeof(PushNotificationKey));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
