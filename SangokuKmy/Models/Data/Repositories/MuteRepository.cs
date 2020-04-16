using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Common;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Data.Repositories
{
  public class MuteRepository
  {
    private readonly IRepositoryContainer container;

    public MuteRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    public async Task<IReadOnlyList<Mute>> GetCharacterAsync(uint charaId)
    {
      try
      {
        return await this.container.Context.Mutes.Where(m => m.CharacterId == charaId).ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task AddAsync(Mute mute)
    {
      try
      {
        await this.container.Context.Mutes.AddAsync(mute);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public void Remove(Mute mute)
    {
      try
      {
        this.container.Context.Mutes.Remove(mute);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public void RemoveCharacter(uint charaId)
    {
      try
      {
        this.container.Context.Mutes.RemoveRange(this.container.Context.Mutes.Where(m => m.CharacterId == charaId && m.Type == MuteType.Muted));
        this.container.Context.MuteKeywords.RemoveRange(this.container.Context.MuteKeywords.Where(k => k.CharacterId == charaId));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public async Task AddAsync(MuteKeyword keyword)
    {
      try
      {
        await this.container.Context.MuteKeywords.AddAsync(keyword);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public async Task<Optional<MuteKeyword>> GetCharacterKeywordAsync(uint charaId)
    {
      try
      {
        return await this.container.Context.MuteKeywords.FirstOrDefaultAsync(k => k.CharacterId == charaId).ToOptionalAsync();
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
        await this.container.RemoveAllRowsAsync(typeof(Mute));
        await this.container.RemoveAllRowsAsync(typeof(MuteKeyword));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
