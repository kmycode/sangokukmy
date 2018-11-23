using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Models.Common.Definitions;

namespace SangokuKmy.Models.Data
{
  /// <summary>
  /// データベースのキャッシュ
  /// </summary>
  public class DatabaseCache<T> : IEnumerable<T> where T : class
  {
    public IReadOnlyList<T> Items => this.Items;
    private IList<T> items;

    private readonly DbSet<T> table;

    public DatabaseCache(DbSet<T> table)
    {
      this.table = table;
      this.Update();
    }

    /// <summary>
    /// キャッシュを更新する
    /// </summary>
    public void Update()
    {
      this.items = this.table.ToList();
    }

    /// <summary>
    /// キャッシュから要素を削除する
    /// </summary>
    /// <returns>削除した要素</returns>
    /// <param name="subject">削除条件</param>
    public IList<T> Remove(Predicate<T> subject)
    {
      var targets = this.items.Where(a => subject(a)).ToList();

      this.table.RemoveRange(targets);
      foreach (var target in targets)
      {
        this.items.Remove(target);
      }

      return targets;
    }

    /// <summary>
    /// キャッシュに要素を追加する
    /// </summary>
    /// <param name="item">追加する要素</param>
    public void Add(T item)
    {
      this.table.Add(item);
      this.items.Add(item);
    }

    #region IEnumerable

    public IEnumerator<T> GetEnumerator()
    {
      return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return items.GetEnumerator();
    }
    
    #endregion
  }
}
