using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Models.Common.Definitions;

namespace SangokuKmy.Models.Data
{
  /// <summary>
  /// データベースのキャッシュ
  /// </summary>
  public class DatabaseCache<T> : IEnumerable<T> where T : class
  {
    public IReadOnlyList<T> Items => this._items;
    private List<T> _items;

    public DatabaseCache(DbSet<T> table)
    {
      Task.Run(async () => await this.UpdateAsync(table)).Wait();
    }

    /// <summary>
    /// キャッシュを更新する
    /// </summary>
    /// <param name="table">テーブル</param>
    public async Task UpdateAsync(DbSet<T> table)
    {
      this._items = await table.ToListAsync();
    }

    /// <summary>
    /// キャッシュから要素を削除する
    /// </summary>
    /// <returns>削除した要素</returns>
    /// <param name="table">テーブル</param>
    /// <param name="subject">削除条件</param>
    public IList<T> Remove(Func<DbSet<T>> table, Predicate<T> subject)
    {
      var targets = this._items.Where(a => subject(a)).ToList();

      if (targets.Any())
      {
        table().RemoveRange(targets);
        foreach (var target in targets)
        {
          this._items.Remove(target);
        }
      }

      return targets;
    }

    /// <summary>
    /// キャッシュから要素を削除する
    /// </summary>
    /// <returns>削除した要素</returns>
    /// <param name="table">テーブル</param>
    /// <param name="subject">削除条件</param>
    public IList<T> Remove(DbSet<T> table, Predicate<T> subject) =>
      this.Remove(() => table, subject);

    /// <summary>
    /// キャッシュに要素を追加する
    /// </summary>
    /// <param name="table">テーブル</param>
    /// <param name="item">追加する要素</param>
    public void Add(DbSet<T> table, T item)
    {
      table.Add(item);
      this._items.Add(item);
    }

    /// <summary>
    /// キャッシュに複数の要素を追加する
    /// </summary>
    /// <param name="table">テーブル</param>
    /// <param name="items">追加する要素</param>
    public void AddRange(DbSet<T> table, IEnumerable<T> items)
    {
      if (items.Any())
      {
        table.AddRange(items);
        foreach (var item in items)
        {
          this._items.Add(item);
        }
      }
    }

    #region IEnumerable

    public IEnumerator<T> GetEnumerator()
    {
      return this._items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return this._items.GetEnumerator();
    }
    
    #endregion
  }
}
