using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using SangokuKmy.Models.Common.Definitions;
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
  public static class OnlineService
  {
    private readonly static List<CharacterOnline> onlines = new List<CharacterOnline>();
    private static CharacterOnline[] onlineCaches = { };
    private readonly static List<CharacterOnline> updates = new List<CharacterOnline>();
    private readonly static AsyncReaderWriterLock locker = new AsyncReaderWriterLock();

    public static async Task<IReadOnlyList<CharacterOnline>> GetAsync()
    {
      try
      {
        using (await locker.ReaderLockAsync())
        {
          return onlineCaches;
        }
      }
      catch (Exception ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
        return default;
      }
    }

    public static async Task SetAsync(Character chara, OnlineStatus status)
    {
      try
      {
        using (await locker.WriterLockAsync())
        {
          var exists = updates.FirstOrDefault(u => u.Character.Id == chara.Id);
          if (exists == null)
          {
            updates.Add(new CharacterOnline
            {
              Status = status,
              Character = new CharacterOnline.CharacterData
              {
                Id = chara.Id,
                Name = chara.Name,
                CountryId = chara.CountryId,
              },
            });
          }
          else
          {
            exists.Status = status;
          }
        }
      }
      catch (Exception ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
      }
    }

    public static void BeginWatch(ILogger logger)
    {
      Task.Run(async () =>
      {
        while (true)
        {
          try
          {
            await UpdateAsync();
            await Task.Delay(5000);
          }
          catch (Exception ex)
          {
            logger.LogError(ex, "オンライン状態処理中にエラーが発生しました");
          }
        }
      });
    }

    private static async Task UpdateAsync()
    {
      try
      {
        using (await locker.WriterLockAsync())
        {
          var streamings = StatusStreaming.Default.GetStreamingCharacters();
          var exists = streamings.Where(s => onlines.Any(o => o.Character.Id == s.Id)).ToArray();
          var newers = streamings.Except(exists).ToArray();
          var leaves = onlines.Where(o => !streamings.Any(s => o.Character.Id == s.Id)).ToArray();
          var countryChanges = streamings
            .Join(onlines, s => s.Id, o => o.Character.Id, (s, o) => new { Streaming = s, Online = o, })
            .Where(d => d.Streaming.CountryId != d.Online.Character.CountryId);

          foreach (var leave in leaves)
          {
            leave.Status = OnlineStatus.Offline;
            onlines.Remove(leave);
            updates.Add(leave);
          }
          foreach (var data in countryChanges)
          {
            data.Online.Character.CountryId = data.Streaming.CountryId;
            updates.Add(data.Online);
          }
          foreach (var chara in newers)
          {
            // ONしてすぐインアクティブになった場合、updatesに値があり、streamingに値がない状態が生まれる
            var existsInUpdates = updates.FirstOrDefault(u => u.Character.Id == chara.Id);

            CharacterOnline data;
            if (existsInUpdates == null)
            {
              data = new CharacterOnline
              {
                Status = OnlineStatus.Active,
                Character = new CharacterOnline.CharacterData
                {
                  Id = chara.Id,
                  Name = chara.Name,
                  CountryId = chara.CountryId,
                },
              };
              updates.Add(data);
            }
            else
            {
              data = existsInUpdates;
            }

            onlines.Add(data);
          }

          onlineCaches = onlines.ToArray();

          // 既存のデータを更新
          if (updates.Any())
          {
            foreach (var update in updates)
            {
              var old = onlines.FirstOrDefault(o => o.Character.Id == update.Character.Id);
              if (old != null)
              {
                old.Status = update.Status;
              }
            }
          }
        }

        // アイコンを取得
        var iconNeeds = updates.Where(d => d.Status != OnlineStatus.Offline);
        if (iconNeeds.Any())
        {
          using (var repo = MainRepository.WithRead())
          {
            foreach (var data in iconNeeds)
            {
              var icon = (await repo.Character.GetCharacterAllIconsAsync(data.Character.Id)).GetMainOrFirst();
              data.Character.Icon = icon.Data;

              var old = onlines.FirstOrDefault(o => o.Character.Id == data.Character.Id);
              if (old != null)
              {
                old.Character.Icon = data.Character.Icon;
              }
            }
          }
        }

        if (updates.Any())
        {
          var apiData = updates.Select(u => ApiData.From(u)).ToArray();
          await Task.WhenAll(
            StatusStreaming.Default.SendAllAsync(apiData),
            AnonymousStreaming.Default.SendAllAsync(apiData));
          updates.Clear();
        }
      }
      catch (Exception ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
      }
    }

    public static async Task ResetAsync()
    {
      try
      {
        using (await locker.WriterLockAsync())
        {
          updates.Clear();
          onlineCaches = new CharacterOnline[] { };
        }
      }
      catch (Exception ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
      }
    }
  }
}
