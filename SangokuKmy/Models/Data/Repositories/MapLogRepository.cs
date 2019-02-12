using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SangokuKmy.Models.Data.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using System.Collections;

namespace SangokuKmy.Models.Data.Repositories
{
  public class MapLogRepository
  {
    private readonly IRepositoryContainer container;

    public MapLogRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// 最新のログを取得
    /// </summary>
    /// <returns>最新ログ</returns>
    /// <param name="count">取得数</param>
    public async Task<IEnumerable<MapLog>> GetNewestAsync(int count)
      => await this.GetRangeAsync(0, count, false);

    /// <summary>
    /// 最新の重要ログを取得
    /// </summary>
    /// <returns>最新ログ</returns>
    /// <param name="count">取得数</param>
    public async Task<IEnumerable<MapLog>> GetImportantNewestAsync(int count)
      => await this.GetRangeAsync(0, count, true);

    /// <summary>
    /// 範囲を指定して、ログを取得
    /// </summary>
    /// <returns>ログ</returns>
    /// <param name="startIndex">開始位置</param>
    /// <param name="count">取得数</param>
    public async Task<IEnumerable<MapLog>> GetRangeAsync(int startIndex, int count)
      => await this.GetRangeAsync(startIndex, count, false);

    /// <summary>
    /// 範囲を指定して、重要ログを取得
    /// </summary>
    /// <returns>ログ</returns>
    /// <param name="startIndex">開始位置</param>
    /// <param name="count">取得数</param>
    public async Task<IEnumerable<MapLog>> GetImportantRangeAsync(int startIndex, int count)
      => await this.GetRangeAsync(startIndex, count, true);

    /// <summary>
    /// 範囲を指定して、ログを取得
    /// </summary>
    /// <returns>取得されたログ</returns>
    /// <param name="startIndex">取得開始位置</param>
    /// <param name="count">取得数</param>
    /// <param name="isImportant">重要ログであるか</param>
    private async Task<IReadOnlyList<MapLog>> GetRangeAsync(int startIndex, int count, bool isImportant)
    {
      try
      {
        return (await this.container.Context.MapLogs
          .Where(ml => isImportant ? ml.IsImportant : true)
          .OrderByDescending(ml => ml.Id)
          .Skip(startIndex)
          .Take(count)
          .ToArrayAsync())
          .Reverse()
          .ToArray();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return null;
      }
    }

    /// <summary>
    /// ログを追加
    /// </summary>
    /// <param name="log">追加するログ</param>
    public async Task AddAsync(MapLog log)
    {
      try
      {
        await this.container.Context.MapLogs.AddAsync(log);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 既存のログに、戦闘ログIDを設定する
    /// </summary>
    /// <param name="maplogId">マップログID</param>
    /// <param name="battleLogId">戦闘ログID</param>
    public async Task SetBattleLogIdAsync(uint maplogId, uint battleLogId)
    {
      try
      {
        (await this.container.Context.MapLogs.FindAsync(maplogId)).BattleLogId = battleLogId;
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
        await this.container.RemoveAllRowsAsync(typeof(MapLog));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
