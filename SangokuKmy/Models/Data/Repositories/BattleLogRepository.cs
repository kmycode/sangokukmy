using System;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Data.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Common;

namespace SangokuKmy.Models.Data.Repositories
{
  public class BattleLogRepository
  {
    private readonly IRepositoryContainer container;

    public BattleLogRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// IDからログを取得する
    /// </summary>
    /// <returns>ログ</returns>
    /// <param name="id">ID</param>
    public async Task<Optional<BattleLog>> GetByIdAsync(uint id)
    {
      try
      {
        return await this.container.Context.BattleLogs
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
    /// IDから、各ラインを含むログを取得する
    /// </summary>
    /// <returns>ログ</returns>
    /// <param name="id">ID</param>
    public async Task<Optional<BattleLog>> GetWithLinesByIdAsync(uint id)
    {
      try
      {
        var log = await this.container.Context.BattleLogs
          .FindAsync(id)
          .ToOptionalAsync();
        if (log.HasData)
        {
          log.Data.Lines = await this.container.Context.BattleLogLines
            .Where(bl => bl.BattleLogId == id)
            .OrderBy(bl => bl.Turn)
            .ToArrayAsync();
          log.Data.AttackerCache = await this.container.Context.CharacterCaches
            .FindAsync(log.Data.AttackerCacheId);
          log.Data.AttackerCache.MainIcon = await this.container.Context.CharacterIcons
            .FindAsync(log.Data.AttackerCache.IconId);
          log.Data.DefenderCache = await this.container.Context.CharacterCaches
            .FindAsync(log.Data.DefenderCacheId);
          if (log.Data.DefenderType == DefenderType.Character)
          {
            log.Data.DefenderCache.MainIcon = await this.container.Context.CharacterIcons
              .FindAsync(log.Data.DefenderCache.IconId);
          }
          log.Data.MapLog = await this.container.Context.MapLogs
            .FindAsync(log.Data.MapLogId);
          log.Data.Town = new TownForAnonymous(await this.container.Context.Towns
            .FindAsync(log.Data.TownId));
        }

        return log;
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// ログを追加する
    /// </summary>
    /// <returns>ログ</returns>
    public async Task AddLogAsync(BattleLog log)
    {
      try
      {
        await this.container.Context.BattleLogs.AddAsync(log);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// ログを追加する。各行も一緒に追加する。途中で保存する
    /// </summary>
    /// <returns>ログ</returns>
    public async Task AddLogWithSaveAsync(BattleLog log, IEnumerable<BattleLogLine> lines, LogCharacterCache attacker, LogCharacterCache defender)
    {
      try
      {
        await this.container.Context.CharacterCaches.AddAsync(attacker);
        if (defender != null)
        {
          await this.container.Context.CharacterCaches.AddAsync(defender);
        }

        await this.container.Context.BattleLogs.AddAsync(log);
        await this.container.Context.SaveChangesAsync();

        log.AttackerCacheId = attacker.Id;
        if (defender != null)
        {
          log.DefenderCacheId = defender.Id;
        }

        foreach (var line in lines)
        {
          line.BattleLogId = log.Id;
        }
        await this.container.Context.BattleLogLines.AddRangeAsync(lines);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 最後に戦闘のあった年月を取得する
    /// </summary>
    public async Task<GameDateTime> GetLastBattleMonthAsync()
    {
      if (this.container.Context.BattleLogs.Any())
      {
        var max = this.container.Context.BattleLogs.Max(b => b.MapLogId);
        var lastMapLog = await this.container.Context.MapLogs.FirstOrDefaultAsync(m => m.Id == max);
        if (lastMapLog != null)
        {
          return lastMapLog.ApiGameDateTime;
        }
      }

      return new GameDateTime
      {
        Year = Config.StartYear,
        Month = Config.StartMonth,
      }.AddMonth(Config.CountryBattleStopDuring);
    }

    /// <summary>
    /// 内容をすべてリセットする
    /// </summary>
    public async Task ResetAsync()
    {
      try
      {
        await this.container.RemoveAllRowsAsync(typeof(BattleLogLine));
        await this.container.RemoveAllRowsAsync(typeof(BattleLog));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
