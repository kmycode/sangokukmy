using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SangokuKmy.Models.Data.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Models.Data.ApiEntities;
using System.Linq.Expressions;
using SangokuKmy.Common;

namespace SangokuKmy.Models.Data.Repositories
{
  public class ThreadBbsRepository
  {
    private readonly IRepositoryContainer container;

    public ThreadBbsRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// IDから書き込みを取得する
    /// </summary>
    /// <param name="id">ID</param>
    /// <returns>書き込み</returns>
    public async Task<Optional<ThreadBbsItem>> GetByIdAsync(uint id)
    {
      try
      {
        return await this.container.Context.ThreadBbsItems.FindAsync(id).ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// IDから書き込みを取得する
    /// </summary>
    /// <param name="id">ID</param>
    /// <returns>書き込み</returns>
    public async Task<Optional<ThreadBbsItem>> GetWithRelationsByIdAsync(uint id, BbsType type)
    {
      try
      {
        return (await this.GetItemsAsync(b => b.Id == id, type)).FirstOrDefault().ToOptional();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 国のすべての書き込みを取得する
    /// </summary>
    /// <returns></returns>
    public async Task<IReadOnlyList<ThreadBbsItem>> GetCountryBbsByCountryIdAsync(uint countryId)
      => await this.GetItemsAsync(c => c.CountryId == countryId, BbsType.CountryBbs);

    /// <summary>
    /// すべての全国会議室の書き込みを取得する
    /// </summary>
    /// <returns></returns>
    public async Task<IReadOnlyList<ThreadBbsItem>> GetGlobalBbsAsync()
      => await this.GetItemsAsync(c => true, BbsType.GlobalBbs);

    /// <summary>
    /// 書き込みを追加する
    /// </summary>
    /// <param name="item">書き込み</param>
    public async Task AddAsync(ThreadBbsItem item)
    {
      try
      {
        await this.container.Context.ThreadBbsItems.AddAsync(item);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 書き込みを削除する
    /// </summary>
    /// <param name="item">書き込み</param>
    public void Remove(ThreadBbsItem item)
    {
      try
      {
        this.container.Context.ThreadBbsItems.Remove(item);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    private async Task<IReadOnlyList<ThreadBbsItem>> GetItemsAsync(Expression<Func<ThreadBbsItem, bool>> expression, BbsType type)
    {
      try
      {
        var itemData = await this.container.Context.ThreadBbsItems
          .Where(expression)
          .Where(b => b.Type == type)
          .OrderByDescending(b => b.Written)
          .Join(this.container.Context.Characters, b => b.CharacterId, c => c.Id, (b, c) => new { Item = b, Character = c, })
          .Join(this.container.Context.CharacterIcons, b => b.Item.CharacterIconId, i => i.Id, (b, i) => new { b.Item, b.Character, Icon = i, })
          .ToArrayAsync();
        var items = itemData.Select(d =>
        {
          var item = d.Item;
          item.Character = new CharacterForAnonymous(d.Character, d.Icon, CharacterShareLevel.Anonymous);
          return item;
        }).ToArray();
        var threads = items
          .Where(i => i.ParentId == 0)
          .GroupJoin(items, p => p.Id, c => c.ParentId, (p, cs) =>
          {
            p.Children = cs;
            return p;
          })
          .ToArray();
        return threads;
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
        await this.container.RemoveAllRowsAsync(typeof(ThreadBbsItem));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
