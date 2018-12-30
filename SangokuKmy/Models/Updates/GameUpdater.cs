using System;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections;
using System.Collections.Generic;
using SangokuKmy.Models.Data.Entities.Caches;
using SangokuKmy.Models.Data;
using System.Linq;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Streamings;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SangokuKmy.Models.Updates
{
  /// <summary>
  /// 更新処理
  /// </summary>
  public static class GameUpdater
  {
    private static DateTime nextMonthStartDateTime = DateTime.Now;

    public static void BeginUpdate(ILogger logger)
    {
      Task.Run(async () =>
      {
        while (true)
        {
          try
          {
            // キャッシュを更新
            using (var repo = MainRepository.WithRead())
            {
              Task.Run(async () =>
              {
                await EntityCaches.UpdateCharactersAsync(repo);
                nextMonthStartDateTime = (await repo.System.GetAsync()).CurrentMonthStartDateTime.AddSeconds(Config.UpdateTime);
              }).Wait();
            }

            // 更新ループ
            await UpdateLoop();
          }
          catch (Exception ex)
          {
            logger.LogError(ex, "更新処理中にエラーが発生しました");
          }
        }
      });
    }

    private static async Task UpdateLoop()
    {
      while (true)
      {
        var current = DateTime.Now;

        // 月を更新
        if (current >= nextMonthStartDateTime)
        {
          using (var repo = MainRepository.WithReadAndWrite())
          {
            var sys = await repo.System.GetAsync();
            if (sys.IsDebug)
            {
              var debug = await repo.System.GetDebugDataAsync();
              var updatableTime = debug.UpdatableLastDateTime;
              if (nextMonthStartDateTime <= updatableTime)
              {
                await UpdateMonthAsync(repo);
              }
            }
            else
            {
              await UpdateMonthAsync(repo);
            }
          }
        }

        // 武将を更新
        var updateCharacters = EntityCaches.Characters
          .Where(ch => (current - ch.LastUpdated).TotalSeconds >= Config.UpdateTime);
        if (updateCharacters.Any())
        {
          using (var repo = MainRepository.WithReadAndWrite())
          {
            var updates = updateCharacters;
            var sys = await repo.System.GetAsync();
            if (sys.IsDebug)
            {
              var debug = await repo.System.GetDebugDataAsync();
              var updatableTime = debug.UpdatableLastDateTime.AddSeconds(-Config.UpdateTime);
              updates = updates.Where(ch => ch.LastUpdated < updatableTime);
            }

            updates = updates.Where(ch => ch.LastUpdatedGameDate.ToInt() <= sys.IntGameDateTime);
            await UpdateCharactersAsync(repo, updates.Select(ch => ch.Id).ToArray());
          }
        }

        // 待機
        await Task.Delay(1000);
      }
    }

    private static async Task UpdateMonthAsync(MainRepository repo)
    {
      var system = await repo.System.GetAsync();
      system.CurrentMonthStartDateTime = system.CurrentMonthStartDateTime.AddSeconds(Config.UpdateTime);
      system.GameDateTime = system.GameDateTime.NextMonth();
      await repo.SaveChangesAsync();

      // キャッシュを更新
      nextMonthStartDateTime = system.CurrentMonthStartDateTime.AddSeconds(Config.UpdateTime);

      // ストリーミング中のユーザに新しいデータを通知する
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(system.GameDateTime));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(system.GameDateTime));
    }

    private static async Task UpdateCharactersAsync(MainRepository repo, IReadOnlyCollection<uint> characterIds)
    {
      foreach (var id in characterIds)
      {
        var chara = await repo.Character.GetByIdAsync(id);
        await chara.SomeAsync(async (c) =>
        {
          await UpdateCharacterAsync(repo, c);
        });
      }
    }

    private static async Task UpdateCharacterAsync(MainRepository repo, Character character)
    {
      var notifies = new List<IApiData>();
      var command = repo.CharacterCommand.GetAsync(character.Id, character.LastUpdatedGameDate.NextMonth());

      // 古いコマンドの削除、更新の記録
      character.LastUpdated = character.LastUpdated.AddSeconds(Config.UpdateTime);
      character.LastUpdatedGameDate = character.LastUpdatedGameDate.NextMonth();
      repo.CharacterCommand.RemoveOlds(character.Id, character.LastUpdatedGameDate);
      await repo.SaveChangesAsync();

      // 更新の通知
      notifies.Add(ApiData.From(character));
      notifies.Add(ApiData.From(new ApiSignal
      {
        Type = SignalType.CurrentCharacterUpdated,
        Data = new { gameDate = character.LastUpdatedGameDate, secondsNextCommand = (int)((character.LastUpdated.AddSeconds(Config.UpdateTime) - DateTime.Now).TotalSeconds), },
      }));

      // キャッシュを更新
      EntityCaches.UpdateCharacter(character);

      // ログイン中のユーザに新しい情報を通知する
      await StatusStreaming.Default.SendCharacterAsync(notifies, character.Id);
    }
  }
}
