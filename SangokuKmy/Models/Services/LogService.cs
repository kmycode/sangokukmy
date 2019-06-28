using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Services
{
  public static class LogService
  {
    public static async Task<MapLog> AddMapLogAsync(MainRepository repo, bool isImportant, EventType type, string message)
    {
      var system = await repo.System.GetAsync();
      var log = new MapLog
      {
        ApiGameDateTime = system.GameDateTime,
        Date = DateTime.Now,
        EventType = type,
        IsImportant = isImportant,
        Message = message,
      };
      await repo.MapLog.AddAsync(log);
      await repo.SaveChangesAsync();
      await StatusStreaming.Default.SendAllAsync(ApiData.From(log));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(log));
      return log;
    }
  }
}
