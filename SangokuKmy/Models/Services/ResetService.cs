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
      var now = DateTime.Now;
      var system = await repo.System.GetAsync();

      var history = await repo.History.GetAsync(system.Period, system.BetaVersion);
      if (history.HasData)
      {
        var countries = await repo.Country.GetAllAsync();

        var unifiedCountry = countries.FirstOrDefault(c => !c.HasOverthrown);
        CountryMessage unifiedCountryMessage = null;
        if (unifiedCountry != null)
        {
          var messageOptional = await repo.Country.GetMessageAsync(unifiedCountry.Id, CountryMessageType.Unified);
          messageOptional.Some((message) => unifiedCountryMessage = message);
        }
        if (unifiedCountryMessage != null)
        {
          history.Data.UnifiedCountryMessage = unifiedCountryMessage.Message;
        }
      }

      await OnlineService.ResetAsync();

      await repo.AuthenticationData.ResetAsync();
      await repo.BattleLog.ResetAsync();
      await repo.CharacterItem.ResetAsync();
      await repo.CharacterCommand.ResetAsync();
      await repo.Character.ResetAsync();
      await repo.EntryHost.ResetAsync();
      await repo.ChatMessage.ResetAsync();
      await repo.CountryDiplomacies.ResetAsync();
      await repo.AiCountry.ResetAsync();
      await repo.AiActionHistory.ResetAsync();
      await repo.Country.ResetAsync();
      await repo.MapLog.ResetAsync();
      await repo.ScoutedTown.ResetAsync();
      await repo.ThreadBbs.ResetAsync();
      await repo.Town.ResetAsync();
      await repo.Unit.ResetAsync();
      await repo.Reinforcement.ResetAsync();
      await repo.DelayEffect.ResetAsync();
      await repo.Mute.ResetAsync();
      // await repo.PushNotificationKey.ResetAsync();
      await repo.BlockAction.ResetAsync();

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

      await ResetTownsAndSaveAsync(repo, system.RuleSetNextPeriod);

      system.GameDateTime = new GameDateTime
      {
        Year = Config.StartYear,
        Month = Config.StartMonth,
      };
      system.CurrentMonthStartDateTime = new DateTime(now.Year, now.Month, now.Day, 20, 0, 0, 0);
      system.IsWaitingReset = false;
      system.IntResetGameDateTime = 0;
      system.TerroristCount = 0;
      system.ManagementCountryCount = 0;
      system.IsBattleRoyaleMode = false;
      system.RuleSet = system.RuleSetNextPeriod;
      system.RuleSetNextPeriod = system.RuleSetAfterNextPeriod;
      system.RuleSetAfterNextPeriod =
        RandomService.Next(0, 7) <= 3 ? GameRuleSet.Normal : RandomService.Next(new GameRuleSet[] {
          GameRuleSet.SimpleBattle,
          GameRuleSet.Wandering,
          GameRuleSet.BattleRoyale,
          GameRuleSet.Gyokuji,
          GameRuleSet.Religion,
        });
      if (system.IsNextPeriodBeta)
      {
        system.BetaVersion++;
      }
      else
      {
        system.BetaVersion = 0;
        system.Period++;
      }

      if (Config.Game.IsGenerateAdminCharacter)
      {
        var admin = new Character
        {
          Name = Config.Admin.Name,
          AiType = CharacterAiType.Administrator,
          LastUpdated = DateTime.Now,
          LastUpdatedGameDate = system.GameDateTime,
          TownId = (await repo.Town.GetAllAsync()).First().Id,
          AliasId = Config.Admin.AliasId,
          Money = 1000_0000,
        };
        admin.SetPassword(Config.Admin.Password);
        await repo.Character.AddAsync(admin);
        await repo.SaveChangesAsync();

        var adminIcon = new CharacterIcon
        {
          CharacterId = admin.Id,
          IsAvailable = true,
          IsMain = true,
          Type = CharacterIconType.Gravatar,
          FileName = Config.Admin.GravatarMailAddressMD5,
        };
        await repo.Character.AddCharacterIconAsync(adminIcon);
      }


      var ruleSetName = system.RuleSet == GameRuleSet.Normal ? "標準" :
        system.RuleSet == GameRuleSet.Wandering ? "放浪" :
        system.RuleSet == GameRuleSet.SimpleBattle ? "原理" :
        system.RuleSet == GameRuleSet.BattleRoyale ? "全国戦争" :
        system.RuleSet == GameRuleSet.Gyokuji ? "玉璽" : "";
      await repo.MapLog.AddAsync(new MapLog
      {
        EventType = EventType.Reset,
        Date = DateTime.Now,
        ApiGameDateTime = system.GameDateTime,
        IsImportant = true,
        Message = $"ゲームプログラムを開始しました。今期のルールセットは {ruleSetName} です",
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

    public static async Task RequestResetAsync(MainRepository repo, uint unifiedCountryId)
    {
      var system = await repo.System.GetAsync();
      system.IsWaitingReset = true;
      system.IsBattleRoyaleMode = false;

      var currentMonth = system.CurrentMonthStartDateTime;
      var todayResetHour = new DateTime(currentMonth.Year, currentMonth.Month, currentMonth.Day, 20, 0, 0, 0);
      var resetHour = todayResetHour.AddDays(currentMonth.Hour < 20 ? 1 : 2);
      var sinceResetTime = resetHour - currentMonth;
      var resetTurn = (int)Math.Round(sinceResetTime.TotalMinutes / 10.0f);
      system.ResetGameDateTime = GameDateTime.FromInt(system.GameDateTime.ToInt() + resetTurn);

      await RecordHistoryAsync(repo, system, unifiedCountryId);

      await StatusStreaming.Default.SendAllAsync(ApiData.From(system));
    }

    private static async Task RecordHistoryAsync(MainRepository repo, SystemData system, uint unifiedCountryId)
    {
      await repo.SaveChangesAsync();
      
      var countries = await repo.Country.GetAllAsync();
      var characters = await repo.Character.GetAllAliveWithIconAsync();
      var maplogs = await repo.MapLog.GetAllImportantsAsync();
      var towns = await repo.Town.GetAllAsync();

      var unifiedCountry = countries.FirstOrDefault(c => c.Id == unifiedCountryId);
      var posts = Enumerable.Empty<CountryPost>();
      if (unifiedCountry != null)
      {
        posts = await repo.Country.GetPostsAsync(unifiedCountry.Id);
      }

      var history = new History
      {
        Period = system.Period,
        BetaVersion = system.BetaVersion,
        UnifiedDateTime = DateTime.Now,
        UnifiedCountryMessage = string.Empty,
        Characters = characters.Select(c =>
        {
          var chara = HistoricalCharacter.FromCharacter(c.Character);
          chara.Icon = HistoricalCharacterIcon.FromCharacterIcon(c.Icon);
          var post = posts.FirstOrDefault(p => p.CharacterId == c.Character.Id);
          if (post != null)
          {
            chara.Type = post.Type;
          }
          return chara;
        }).ToArray(),
        Countries = countries.Select(c =>
        {
          var country = HistoricalCountry.FromCountry(c);
          country.HasOverthrown = c.Id != unifiedCountryId;
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
        catch
        {
          // loggerがない！
        }
      }

      await repo.History.RecordAndSaveAsync(history);
    }

    private static async Task ResetTownsAndSaveAsync(MainRepository repo, GameRuleSet ruleSet)
    {
      var num = ruleSet == GameRuleSet.Wandering ? 1 : RandomService.Next(14, 19);
      var initialTowns = MapService.CreateMap(num);
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
      await repo.SaveChangesAsync();

      await ItemService.InitializeItemOnTownsAsync(repo, towns);
      await repo.SaveChangesAsync();
    }
  }
}
