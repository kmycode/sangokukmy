using SangokuKmy.Models.Common;
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
  public static class ResetService
  {
    public static async Task ResetAsync(MainRepository repo)
    {
      await OnlineService.ResetAsync();

      await repo.AuthenticationData.ResetAsync();
      await repo.BattleLog.ResetAsync();
      await repo.CharacterCommand.ResetAsync();
      await repo.CharacterSoldierType.ResetAsync();
      await repo.Character.ResetAsync();
      await repo.EntryHost.ResetAsync();
      await repo.ChatMessage.ResetAsync();
      await repo.CountryDiplomacies.ResetAsync();
      await repo.AiCountry.ResetAsync();
      await repo.Country.ResetAsync();
      await repo.MapLog.ResetAsync();
      await repo.ScoutedTown.ResetAsync();
      await repo.ThreadBbs.ResetAsync();
      await repo.Town.ResetAsync();
      await repo.Unit.ResetAsync();
      await repo.Reinforcement.ResetAsync();

      // ファイル削除
      try
      {
        foreach (var file in System.IO.Directory.GetFiles(Config.Game.UploadedIconDirectory).Where(f => !f.EndsWith(".txt", StringComparison.Ordinal)))
        {
          System.IO.File.Delete(file);
        }
      }
      catch
      {
        // Loggerがない！
      }

      await ResetTownsAsync(repo);

      var now = DateTime.Now;
      var system = await repo.System.GetAsync();
      system.GameDateTime = new GameDateTime
      {
        Year = Config.StartYear,
        Month = Config.StartMonth,
      };
      system.CurrentMonthStartDateTime = new DateTime(now.Year, now.Month, now.Day, 21, 0, 0, 0);
      system.IsWaitingReset = false;
      system.IntResetGameDateTime = 0;
      system.TerroristCount = 0;
      if (system.IsNextPeriodBeta)
      {
        system.BetaVersion++;
      }
      else
      {
        system.BetaVersion = 0;
        system.Period++;
      }

      await repo.MapLog.AddAsync(new MapLog
      {
        EventType = EventType.Reset,
        Date = DateTime.Now,
        ApiGameDateTime = system.GameDateTime,
        IsImportant = false,
        Message = "ゲームプログラムを開始しました",
      });
      await repo.SaveChangesAsync();

      await StatusStreaming.Default.SendAllAsync(ApiData.From(new ApiSignal
      {
        Type = SignalType.Reseted,
      }));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new ApiSignal
      {
        Type = SignalType.Reseted,
      }));
    }

    public static async Task RequestResetAsync(MainRepository repo)
    {
      var system = await repo.System.GetAsync();
      system.IsWaitingReset = true;

      var currentMonth = system.CurrentMonthStartDateTime;
      var todayResetHour = new DateTime(currentMonth.Year, currentMonth.Month, currentMonth.Day, 21, 0, 0, 0);
      var resetHour = todayResetHour.AddDays(currentMonth.Hour < 21 ? 2 : 3);
      var sinceResetTime = resetHour - currentMonth;
      var resetTurn = (int)Math.Round(sinceResetTime.TotalMinutes / 10.0f);
      system.ResetGameDateTime = GameDateTime.FromInt(system.GameDateTime.ToInt() + resetTurn);

      await RecordHistoryAsync(repo, system);

      await StatusStreaming.Default.SendAllAsync(ApiData.From(system));
    }

    public static async Task Period0_2_SpecialEndAsync(MainRepository repo)
    {
      var system = await repo.System.GetAsync();
      system.IsWaitingReset = true;

      var currentMonth = system.CurrentMonthStartDateTime;
      var todayResetHour = new DateTime(currentMonth.Year, currentMonth.Month, currentMonth.Day, 21, 0, 0, 0);
      var resetHour = todayResetHour.AddDays(currentMonth.Hour < 21 ? 2 : 3);
      var sinceResetTime = resetHour - currentMonth;
      var resetTurn = (int)Math.Round(sinceResetTime.TotalMinutes / 10.0f);
      system.ResetGameDateTime = GameDateTime.FromInt(system.GameDateTime.ToInt() + resetTurn);

      await StatusStreaming.Default.SendAllAsync(ApiData.From(system));
    }

    private static async Task RecordHistoryAsync(MainRepository repo, SystemData system)
    {
      await repo.SaveChangesAsync();
      
      var countries = await repo.Country.GetAllAsync();
      var characters = await repo.Character.GetAllAliveWithIconAsync();
      var maplogs = await repo.MapLog.GetAllImportantsAsync();
      var towns = await repo.Town.GetAllAsync();

      var unifiedCountry = countries.FirstOrDefault(c => !c.HasOverthrown);
      CountryMessage unifiedCountryMessage = null;
      if (unifiedCountry != null)
      {
        var messageOptional = await repo.Country.GetMessageAsync(unifiedCountry.Id, CountryMessageType.Unified);
        messageOptional.Some((message) => unifiedCountryMessage = message);
      }

      var history = new History
      {
        Period = system.Period,
        BetaVersion = system.BetaVersion,
        UnifiedDateTime = DateTime.Now,
        UnifiedCountryMessage = unifiedCountryMessage?.Message ?? string.Empty,
        Characters = characters.Select(c =>
        {
          var chara = HistoricalCharacter.FromCharacter(c.Character);
          chara.Icon = HistoricalCharacterIcon.FromCharacterIcon(c.Icon);
          return chara;
        }).ToArray(),
        Countries = countries.Select(c =>
        {
          var country = HistoricalCountry.FromCountry(c);
          return country;
        }).ToArray(),
        MapLogs = maplogs.Select(m =>
        {
          var maplog = HistoricalMapLog.FromMapLog(m);
          return maplog;
        }).ToArray(),
        Towns = towns.Select(t =>
        {
          var town = HistoricalTown.FromTown(t);
          return town;
        }).ToArray(),
      };

      // アイコンを保存
      var icons = history.Characters.Select(c => c.Icon);
      foreach (var icon in icons.Where(i => i.Type == CharacterIconType.Uploaded))
      {
        try
        {
          System.IO.File.Copy(Config.Game.UploadedIconDirectory + icon.FileName, Config.Game.HistoricalUploadedIconDirectory + icon.FileName);
        }
        catch (Exception ex)
        {
          // loggerがない！
        }
      }

      await repo.History.RecordAndSaveAsync(history);
    }

    private static async Task ResetTownsAsync(MainRepository repo)
    {
      var initialTowns = await repo.Town.GetAllInitialTownsAsync(); //MapService.CreateMap(7);
      var towns = new List<Town>();
      foreach (var itown in initialTowns)
      {
        var typeId = itown.Type;
        var town = MapService.CreateTown(typeId);
        town.Name = itown.Name;
        town.X = itown.X;
        town.Y = itown.Y;
        towns.Add(town);
      }

      await repo.Town.AddTownsAsync(towns);
    }
  }
}
