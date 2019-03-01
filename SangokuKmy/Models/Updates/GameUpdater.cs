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
using SangokuKmy.Models.Services;

namespace SangokuKmy.Models.Updates
{
  /// <summary>
  /// 更新処理
  /// </summary>
  public static class GameUpdater
  {
    private static readonly Random rand = new Random(DateTime.Now.Millisecond);
    private static DateTime nextMonthStartDateTime = DateTime.Now;

    public static void BeginUpdate(ILogger logger)
    {
      Task.Run(async () =>
      {
        while (true)
        {
          try
          {
            using (var repo = MainRepository.WithRead())
            {
              await EntityCaches.UpdateCharactersAsync(repo);
              nextMonthStartDateTime = (await repo.System.GetAsync()).CurrentMonthStartDateTime.AddSeconds(Config.UpdateTime);
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
      // 月を進める
      var system = await repo.System.GetAsync();
      system.CurrentMonthStartDateTime = system.CurrentMonthStartDateTime.AddSeconds(Config.UpdateTime);
      system.GameDateTime = system.GameDateTime.NextMonth();
      await repo.SaveChangesAsync();

      // リセット判定
      if (await CheckResetAsync(repo, system))
      {
        return;
      }

      // キャッシュ更新
      await EntityCaches.UpdateCharactersAsync(repo);

      var notificationMapLogs = new List<ApiData<MapLog>>();

      // ログを追加する関数
      async Task AddLogAsync(uint characterId, string message)
      {
        var log = new CharacterLog
        {
          GameDateTime = system.GameDateTime,
          DateTime = DateTime.Now,
          CharacterId = characterId,
          Message = message,
        };
        await repo.Character.AddCharacterLogAsync(characterId, log);
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(log), characterId);
      }
      async Task<MapLog> AddMapLogInnerAsync(bool isImportant, EventType type, string message)
      {
        var log = new MapLog
        {
          ApiGameDateTime = system.GameDateTime,
          Date = DateTime.Now,
          EventType = type,
          IsImportant = isImportant,
          Message = message,
        };
        await repo.MapLog.AddAsync(log);
        notificationMapLogs.Add(ApiData.From(log));
        return log;
      }
      async Task AddMapLogAsync(bool isImportant, EventType type, string message)
      {
        await AddMapLogInnerAsync(isImportant, type, message);
      }
      async Task<uint> AddMapLogAndSaveAsync(bool isImportant, EventType type, string message)
      {
        var log = await AddMapLogInnerAsync(isImportant, type, message);
        await repo.SaveChangesAsync();
        return log.Id;
      }

      var allCharacters = await repo.Character.GetAllAliveAsync();
      var allTowns = await repo.Town.GetAllAsync();
      var allCountries = (await repo.Country.GetAllAsync()).Where(c => !c.HasOverthrown).ToArray();
      var countryData = allCountries
        .GroupJoin(allTowns, c => c.Id, t => t.CountryId, (c, ts) => new { Country = c, Towns = ts, })
        .GroupJoin(allCharacters, d => d.Country.Id, c => c.CountryId, (c, cs) => new { c.Country, c.Towns, Characters = cs, });

      if (system.GameDateTime.Year >= Config.UpdateStartYear)
      {
        // 収入
        if (system.GameDateTime.Month == 1 || system.GameDateTime.Month == 7)
        {
          foreach (var country in countryData)
          {
            // 国ごとの収入を算出
            var salary = new CountrySalary
            {
              CountryId = country.Country.Id,
              AllSalary = (int)country
                .Towns
                .Sum(t => (system.GameDateTime.Month == 1 ? t.Commercial : t.Agriculture) * 8 * t.People * (t.Id == country.Country.CapitalTownId ? 1.4f : 1.0f) / 10000),
              AllContributions = country.Characters.Sum(c => c.Contribution),
            };
            salary.AllSalary = Math.Max(salary.AllSalary, 0);
            if (system.GameDateTime.Month == 1)
            {
              country.Country.LastMoneyIncomes = salary.AllSalary;
            }
            else
            {
              country.Country.LastRiceIncomes = salary.AllSalary;
            }

            // 収入を武将に配る
            foreach (var character in country.Characters)
            {
              var currentLank = Math.Min(Config.LankCount - 1, character.Class / Config.NextLank);
              var add = salary.AllContributions > 0 ?
                (int)(salary.AllSalary * (float)character.Contribution / salary.AllContributions + character.Contribution * 1.3f) : 0;
              var addMax = 1000 + currentLank * 150;
              add = Math.Min(Math.Max(add, 0), addMax);

              if (system.GameDateTime.Month == 1)
              {
                character.Money += add;
                await AddLogAsync(character.Id, "税金で <num>" + add + "</num> の金を徴収しました");
              }
              else
              {
                character.Rice += add;
                await AddLogAsync(character.Id, "収穫で <num>" + add + "</num> の米を徴収しました");
              }

              // 昇格
              if (character.Contribution > 0 && character.Class % Config.NextLank + character.Contribution >= Config.NextLank)
              {
                var newLank = Math.Min(Config.LankCount - 1, (character.Class + character.Contribution) / Config.NextLank);
                var tecName = string.Empty;
                switch (rand.Next(1, 5))
                {
                  case 1:
                    character.Strong++;
                    tecName = "武力";
                    break;
                  case 2:
                    character.Intellect++;
                    tecName = "知力";
                    break;
                  case 3:
                    character.Leadership++;
                    tecName = "統率";
                    break;
                  case 4:
                    character.Popularity++;
                    tecName = "人望";
                    break;
                }
                var newAddMax = 1000 + newLank * 150;
                await AddLogAsync(character.Id, "【昇格】" + tecName + " が <num>+1</num> 上がりました");
                if (currentLank != newLank)
                {
                  await AddLogAsync(character.Id, "【昇格】最大収入が <num>" + newAddMax + "</num> になりました");
                }
              }

              // データ保存
              character.Class += character.Contribution;
              character.Contribution = 0;

              await StatusStreaming.Default.SendCharacterAsync(ApiData.From(character), character.Id);
            }
          }

          await repo.SaveChangesAsync();
        }

        // イベント
        if (rand.Next(0, 100) == 0)
        {
          var targetTown = allTowns[rand.Next(0, allTowns.Count)];
          var targetTowns = allTowns.GetAroundTowns(targetTown);

          var eventId = rand.Next(0, 6);
          switch (eventId)
          {
            case 0:
              await AddMapLogAsync(true, EventType.Event, "<town>" + targetTown.Name + "</town> 周辺でいなごの大群が畑を襲いました");
              foreach (var town in targetTowns)
              {
                town.Agriculture = (int)(town.Agriculture * 0.85f);
              }
              targetTown.Agriculture = (int)(targetTown.Agriculture * 0.75f);
              break;
            case 1:
              await AddMapLogAsync(true, EventType.Event, "<town>" + targetTown.Name + "</town> 周辺で洪水がおこりました");
              foreach (var town in targetTowns)
              {
                town.Agriculture = (int)(town.Agriculture * 0.94f);
                town.Commercial = (int)(town.Commercial * 0.94f);
                town.Wall = (int)(town.Wall * 0.94f);
              }
              targetTown.Agriculture = (int)(targetTown.Agriculture * 0.85f);
              targetTown.Commercial = (int)(targetTown.Commercial * 0.85f);
              targetTown.Wall = (int)(targetTown.Wall * 0.85f);
              break;
            case 2:
              await AddMapLogAsync(true, EventType.Event, "<town>" + targetTown.Name + "</town> を中心に疫病が広がっています。町の人も苦しんでいます");
              foreach (var town in targetTowns)
              {
                town.People = (int)(town.People * 0.85f);
              }
              targetTown.People = (int)(targetTown.People * 0.75f);
              break;
            case 3:
              await AddMapLogAsync(true, EventType.Event, "<town>" + targetTown.Name + "</town> 周辺は、今年は豊作になりそうです");
              foreach (var town in targetTowns)
              {
                town.Agriculture = Math.Min((int)(town.Agriculture * 1.12f), town.AgricultureMax);
              }
              targetTown.Agriculture = Math.Min((int)(targetTown.Agriculture * 1.24f), targetTown.AgricultureMax);
              break;
            case 4:
              await AddMapLogAsync(true, EventType.Event, "<town>" + targetTown.Name + "</town> 周辺で地震が起こりました");
              foreach (var town in targetTowns)
              {
                town.Agriculture = (int)(town.Agriculture * 0.86f);
                town.Commercial = (int)(town.Commercial * 0.86f);
                town.Wall = (int)(town.Wall * 0.86f);
                town.People = (int)(town.People * 0.94f);
              }
              targetTown.Agriculture = (int)(targetTown.Agriculture * 0.76f);
              targetTown.Commercial = (int)(targetTown.Commercial * 0.76f);
              targetTown.Wall = (int)(targetTown.Wall * 0.76f);
              targetTown.People = (int)(targetTown.People * 0.76f);
              break;
            case 5:
              await AddMapLogAsync(true, EventType.Event, "<town>" + targetTown.Name + "</town> 周辺の市場が賑わっています");
              foreach (var town in targetTowns)
              {
                town.Commercial = Math.Min((int)(town.Commercial * 1.12f), town.CommercialMax);
              }
              targetTown.Commercial = Math.Min((int)(targetTown.Commercial * 1.24f), targetTown.CommercialMax);
              break;
          }

          await StatusStreaming.Default.SendCountryAsync(ApiData.From(targetTown), targetTown.CountryId);
          await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(targetTown));
          foreach (var town in targetTowns)
          {
            await StatusStreaming.Default.SendCountryAsync(ApiData.From(town), town.CountryId);
            await StatusStreaming.Default.SendTownToAllAsync(ApiData.From((Town)town));
          }
          await repo.SaveChangesAsync();
        }

        // 相場・人口変動
        if (system.GameDateTime.Month == 1 || system.GameDateTime.Month == 7)
        {
          var ricePriceBase = 1000000;
          var ricePriceMax = (int)(ricePriceBase * 1.2f);
          var ricePriceMin = (int)(ricePriceBase * 0.8f);
          foreach (var town in allTowns)
          {
            // 相場
            if (rand.Next(0, 2) == 0)
            {
              town.IntRicePrice += (int)(rand.NextDouble() * 0.5 * ricePriceBase / 10);
              if (town.IntRicePrice > ricePriceMax)
              {
                town.IntRicePrice = ricePriceMax;
              }
            }
            else
            {
              town.IntRicePrice -= (int)(rand.NextDouble() * 0.5 * ricePriceBase / 10);
              if (town.IntRicePrice < ricePriceMin)
              {
                town.IntRicePrice = ricePriceMin;
              }
            }

            // 人口
            var peopleAdd = 0;
            if (town.Security > 50)
            {
              peopleAdd = Math.Max(80 * (town.Security - 50), 500);
            }
            else if (town.Security < 50)
            {
              peopleAdd = -80 * (50 - town.Security);
            }
            town.People += peopleAdd;
            if (town.People > town.PeopleMax)
            {
              town.People = town.PeopleMax;
            }
            else if (town.People < 0)
            {
              town.People = 0;
            }

            await StatusStreaming.Default.SendCountryAsync(ApiData.From(town), town.CountryId);
            await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(town));
          }

          await repo.SaveChangesAsync();
        }

        // 同盟破棄・戦争開始
        {
          foreach (var alliance in await repo.CountryDiplomacies.GetBreakingAlliancesAsync())
          {
            alliance.BreakingDelay--;
            if (alliance.BreakingDelay <= 0)
            {
              alliance.Status = CountryAllianceStatus.Broken;
              if (alliance.IsPublic)
              {
                var country1 = allCountries.FirstOrDefault(c => c.Id == alliance.RequestedCountryId);
                var country2 = allCountries.FirstOrDefault(c => c.Id == alliance.InsistedCountryId);
                if (country1 != null && country2 != null)
                {
                  await AddMapLogAsync(true, EventType.AllianceBroken, "<country>" + country1.Name + "</country> と <country>" + country2.Name + "</country> の同盟は破棄されました");
                }
                await StatusStreaming.Default.SendAllAsync(ApiData.From(alliance));
              }
              else
              {
                await StatusStreaming.Default.SendCountryAsync(ApiData.From(alliance), alliance.RequestedCountryId);
                await StatusStreaming.Default.SendCountryAsync(ApiData.From(alliance), alliance.InsistedCountryId);
              }

              var reinforcements = (await repo.Reinforcement.GetByCountryIdAsync(alliance.RequestedCountryId))
                .Concat(await repo.Reinforcement.GetByCountryIdAsync(alliance.InsistedCountryId))
                .Where(r => r.Status == ReinforcementStatus.Active)
                .Where(r => (r.RequestedCountryId == alliance.RequestedCountryId && r.CharacterCountryId == alliance.InsistedCountryId) ||
                            (r.RequestedCountryId == alliance.InsistedCountryId && r.CharacterCountryId == alliance.RequestedCountryId));
              foreach (var reinforcement in reinforcements)
              {
                reinforcement.Status = ReinforcementStatus.Returned;
                var chara = await repo.Character.GetByIdAsync(reinforcement.CharacterId);
                if (chara.HasData && !chara.Data.HasRemoved)
                {
                  var character = chara.Data;
                  var originalCountry = await repo.Country.GetByIdAsync(reinforcement.CharacterCountryId);
                  var country = await repo.Country.GetByIdAsync(character.CountryId);

                  character.CountryId = reinforcement.CharacterCountryId;
                  if (originalCountry.HasData && country.HasData)
                  {
                    character.TownId = originalCountry.Data.CapitalTownId;
                    await AddMapLogAsync(false, EventType.ReinforcementReturned, $"<country>{originalCountry.Data.Name}</country> の援軍 <character>{character.Name}</character> は、同盟破棄に伴い <country>{country.Data.Name}</country> から帰還しました");
                    await AddLogAsync(character.Id, $"同盟破棄に伴い、 <country>{originalCountry.Data.Name}</country> から強制帰還しました");
                  }
                }
              }
            }
          }
          foreach (var war in (await repo.CountryDiplomacies.GetReadyWarsAsync()).Where(cw => cw.Status == CountryWarStatus.InReady))
          {
            if (war.StartGameDate.ToInt() <= system.GameDateTime.ToInt())
            {
              var country1 = allCountries.FirstOrDefault(c => c.Id == war.RequestedCountryId);
              var country2 = allCountries.FirstOrDefault(c => c.Id == war.InsistedCountryId);
              if (country1 != null && country2 != null)
              {
                await AddMapLogAsync(true, EventType.WarStart, "<country>" + country1.Name + "</country> と <country>" + country2.Name + "</country> の戦争が始まりました");
              }
              war.Status = CountryWarStatus.Available;
              await StatusStreaming.Default.SendAllAsync(ApiData.From(war));
            }
          }
          foreach (var war in (await repo.CountryDiplomacies.GetAllTownWarsAsync()))
          {
            if (war.Status == TownWarStatus.InReady)
            {
              if (war.IntGameDate == system.IntGameDateTime)
              {
                war.Status = TownWarStatus.Available;
                var country1 = allCountries.FirstOrDefault(c => c.Id == war.RequestedCountryId);
                var country2 = allCountries.FirstOrDefault(c => c.Id == war.InsistedCountryId);
                var town = allTowns.FirstOrDefault(t => t.Id == war.TownId);
                if (country1 != null && country2 != null)
                {
                  await AddMapLogAsync(true, EventType.TownWarInReady, $"<country>{country1.Name}</country> は、<date>{war.GameDate.ToString()}</date> の間 <country>{country2.Name}</country> の <town>{town.Name}</town> を攻略します");
                }
                await StatusStreaming.Default.SendAllAsync(ApiData.From(war));
              }
              else if (war.IntGameDate < system.IntGameDateTime)
              {
                war.Status = TownWarStatus.Terminated;
              }
            }
            else if (war.Status == TownWarStatus.Available)
            {
              war.Status = TownWarStatus.Terminated;
              await StatusStreaming.Default.SendAllAsync(ApiData.From(war));
            }
          }

          await repo.SaveChangesAsync();
        }

        // 異民族
        if (system.TerroristCount <= 0 && !system.IsWaitingReset && system.GameDateTime.Year >= 194 && rand.Next(0, 200) == 0)
        {
          var isCreated = await AiService.CreateTerroristCountryAsync(repo, (type, message, isImportant) => AddMapLogAsync(isImportant, type, message));
          if (isCreated)
          {
            system.TerroristCount++;
          }
        }
      }

      // 月の更新を保存
      var updateLog = new CharacterUpdateLog { IsFirstAtMonth = true, DateTime = DateTime.Now, GameDateTime = system.GameDateTime, };
      await repo.Character.AddCharacterUpdateLogAsync(updateLog);
      await repo.SaveChangesAsync();

      // キャッシュを更新
      nextMonthStartDateTime = system.CurrentMonthStartDateTime.AddSeconds(Config.UpdateTime);

      // ストリーミング中のユーザに新しいデータを通知する
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(system));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(updateLog));
      await AnonymousStreaming.Default.SendAllAsync(notificationMapLogs);
      await StatusStreaming.Default.SendAllAsync(ApiData.From(system.GameDateTime));
      await StatusStreaming.Default.SendAllAsync(notificationMapLogs);
      foreach (var country in allCountries)
      {
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(country), country.Id);
      }
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
      var anonymousNotifies = new List<ApiData<MapLog>>();
      var anyoneNotifies = new List<Tuple<uint, IApiData>>();
      var currentMonth = character.LastUpdatedGameDate.NextMonth();
      var oldStrong = character.Strong;
      var oldIntellect = character.Intellect;
      var oldLeadership = character.Leadership;
      var oldPopularity = character.Popularity;
      var oldTownId = character.TownId;

      // ログを追加する関数
      async Task AddLogByIdAsync(uint id, string message)
      {
        var log = new CharacterLog
        {
          GameDateTime = currentMonth,
          DateTime = DateTime.Now,
          CharacterId = id,
          Message = message,
        };
        await repo.Character.AddCharacterLogAsync(log);
        anyoneNotifies.Add(new Tuple<uint, IApiData>(id, ApiData.From(log)));
      }
      async Task<MapLog> AddMapLogInnerAsync(bool isImportant, EventType type, string message)
      {
        var log = new MapLog
        {
          ApiGameDateTime = currentMonth,
          Date = DateTime.Now,
          EventType = type,
          IsImportant = isImportant,
          Message = message,
        };
        await repo.MapLog.AddAsync(log);
        anonymousNotifies.Add(ApiData.From(log));
        return log;
      }
      async Task AddMapLogAsync(EventType type, string message, bool isImportant)
      {
        await AddMapLogInnerAsync(isImportant, type, message);
      }
      async Task<uint> AddMapLogAndSaveAsync(EventType type, string message, bool isImportant)
      {
        var log = await AddMapLogInnerAsync(isImportant, type, message);
        await repo.SaveChangesAsync();
        return log.Id;
      }
      async Task AddLogAsync(string message) => await AddLogByIdAsync(character.Id, message);

      var gameObj = new CommandSystemData
      {
        CharacterLogAsync = AddLogAsync,
        CharacterLogByIdAsync = AddLogByIdAsync,
        MapLogAsync = AddMapLogAsync,
        MapLogAndSaveAsync = AddMapLogAndSaveAsync,
        GameDateTime = currentMonth,
      };

      // コマンドの実行
      var ai = AiCharacterFactory.Create(character);
      var commandOptional = await ai.GetCommandAsync(repo, currentMonth);
      if (currentMonth.Year >= Config.UpdateStartYear)
      {
        var isCommandExecuted = false;
        if (commandOptional.HasData)
        {
          var command = commandOptional.Data;
          var commandRunnerOptional = Commands.Commands.Get(command.Type);
          if (commandRunnerOptional.HasData)
          {
            var commandRunner = commandRunnerOptional.Data;
            await commandRunner.ExecuteAsync(repo, character, command.Parameters, gameObj);
            isCommandExecuted = true;

            if (commandRunner.Type != CharacterCommandType.None)
            {
              character.DeleteTurn = 0;
            }
          }
        }
        if (!isCommandExecuted)
        {
          await Commands.Commands.EmptyCommand.ExecuteAsync(repo, character, new CharacterCommandParameter[] { }, gameObj);
        }
      }

      // 放置削除の確認
      if (character.DeleteTurn >= Config.DeleteTurns)
      {
        var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
        await CharacterService.RemoveAsync(repo, character);
        await AddMapLogAsync(EventType.CharacterRemoved, "<country>" + (countryOptional.Data?.Name ?? "無所属") + "</country> の <character>" + character.Name + "</character> は放置削除されました", false);
        StatusStreaming.Default.Disconnect(character);
      }

      // 古いコマンドの削除
      character.LastUpdated = character.LastUpdated.AddSeconds(Config.UpdateTime);
      character.LastUpdatedGameDate = currentMonth;
      repo.CharacterCommand.RemoveOlds(character.Id, character.LastUpdatedGameDate);

      // 兵士の兵糧
      if (character.Rice >= character.SoldierNumber)
      {
        character.Rice -= character.SoldierNumber;
      }
      else
      {
        var dec = character.SoldierNumber - character.Rice;
        character.SoldierNumber = character.Rice;
        character.Rice = 0;
        await AddLogAsync($"米が足りず、兵士 <num>{dec}</num> を解雇しました");
      }

      // 能力上昇
      async Task CheckAttributeAsync(string name, int old, int current, int type)
      {
        if (current > old)
        {
          await AddLogAsync(name + " が <num>+" + (current - old) + "</num>上昇しました");
          notifies.Add(ApiData.From(new ApiSignal
          {
            Type = SignalType.AttributeUp,
            Data = new { type, value = (current - old), }
          }));
        }
      }
      await CheckAttributeAsync("武力", oldStrong, character.Strong, 1);
      await CheckAttributeAsync("知力", oldIntellect, character.Intellect, 2);
      await CheckAttributeAsync("統率", oldLeadership, character.Leadership, 3);
      await CheckAttributeAsync("人望", oldPopularity, character.Popularity, 4);

      // 更新の記録
      var updateLog = new CharacterUpdateLog
      {
        IsFirstAtMonth = false,
        CharacterId = character.Id,
        CharacterName = character.Name,
        DateTime = DateTime.Now,
        GameDateTime = gameObj.GameDateTime,
      };
      await repo.Character.AddCharacterUpdateLogAsync(updateLog);
      await repo.SaveChangesAsync();

      // 更新の通知
      notifies.Add(ApiData.From(character));
      notifies.Add(ApiData.From(new ApiSignal
      {
        Type = SignalType.CurrentCharacterUpdated,
        Data = new { gameDate = character.LastUpdatedGameDate, secondsNextCommand = (int)((character.LastUpdated.AddSeconds(Config.UpdateTime) - DateTime.Now).TotalSeconds), },
      }));

      // キャッシュを更新（新しい更新時刻を保存）
      EntityCaches.UpdateCharacter(character);

      // ログイン中のユーザに新しい情報を通知する
      await StatusStreaming.Default.SendCharacterAsync(notifies, character.Id);
      await StatusStreaming.Default.SendAllAsync(anonymousNotifies);
      await AnonymousStreaming.Default.SendAllAsync(anonymousNotifies);
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(updateLog));
      foreach (var charaData in anyoneNotifies.GroupBy(an => an.Item1, (id, items) => new { CharacterId = id, Items = items.Select(i => i.Item2), }))
      {
        await StatusStreaming.Default.SendCharacterAsync(charaData.Items, charaData.CharacterId);
      }

      await (await repo.Town.GetByIdAsync(character.TownId)).SomeAsync(async (town) =>
      {
        // 自国の都市であれば、同じ国の武将に、共有情報を通知する（それ以外の都市は諜報経由で）
        if (town.CountryId == character.CountryId)
        {
          await StatusStreaming.Default.SendCountryAsync(ApiData.From(town), character.CountryId);
        }

        // 同じ都市にいる他国の武将にも通知
        // （自分が他国の都市にいる場合は、都市データ受信のさいは、自分もここに含まれる）
        var charas = (await repo.Town.GetCharactersWithIconAsync(town.Id))
          .Select(d => d.Character)
          .Where(c => c.CountryId != town.CountryId);
        foreach (var chara in charas)
        {
          await StatusStreaming.Default.SendCharacterAsync(ApiData.From(town), chara.Id);
        }

        if (oldTownId != town.Id)
        {
          (await repo.Town.GetByIdAsync(oldTownId)).Some(async oldTown =>
          {
            // 支配したときとか、元の都市にいた人に武将数や守備人数の更新を指示する
            await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(oldTown));
          });
        }
      });
    }

    private static async Task<bool> CheckResetAsync(MainRepository repo, SystemData system)
    {
      if (system.IsWaitingReset && system.IntResetGameDateTime == system.IntGameDateTime)
      {
        await ResetService.ResetAsync(repo);
        return true;
      }
      return false;
    }

    private class CountrySalary
    {
      public uint CountryId { get; set; }
      public int AllSalary { get; set; }
      public int AllContributions { get; set; }
    }
  }
}
