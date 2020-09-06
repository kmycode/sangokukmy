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
using SangokuKmy.Models.Updates.Ai;
using SangokuKmy.Models.Commands;

namespace SangokuKmy.Models.Updates
{
  /// <summary>
  /// 更新処理
  /// </summary>
  public static class GameUpdater
  {
    private static DateTime nextMonthStartDateTime = DateTime.Now;

    private static ILogger _logger;

    public static void BeginUpdate(ILogger logger)
    {
      // クラスをメモリにロードさせ、static変数を初期化する目的で呼び出す
      RandomService.Next();

      _logger = logger;
      PushNotificationService.Logger = _logger;

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
            await Task.Delay(5000);
          }
        }
      });
    }

    private static async Task UpdateLoop()
    {
      DateTime last = DateTime.Now;

      while (true)
      {
        try
        {
          // 9分59秒更新の武将が月更新より後に処理されないようにする
          var current = DateTime.Now;
          if (last < nextMonthStartDateTime && current > nextMonthStartDateTime)
          {
            last = current;
            current = nextMonthStartDateTime.AddMilliseconds(-1);
          }
          else
          {
            last = current;
          }

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
              await UpdateCharactersAsync(repo, updates.OrderBy(ch => ch.LastUpdated).Select(ch => ch.Id).ToArray());
            }
          }

          // 待機
          await Task.Delay(1000);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "武将更新処理中にエラーが発生しました");
          await Task.Delay(5000);
        }
      }
    }

    private static async Task UpdateMonthAsync(MainRepository repo)
    {
      SystemData system = null;

      try
      {
        // 月を進める
        system = await repo.System.GetAsync();
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
          await repo.Character.AddCharacterLogAsync(log);
          await StatusStreaming.Default.SendCharacterAsync(ApiData.From(log), characterId);
        }
        async Task<MapLog> AddMapLogInnerAsync(bool isImportant, EventType type, string message)
        {
          return await LogService.AddMapLogAsync(repo, isImportant, type, message);
        }
        async Task AddMapLogAsync(bool isImportant, EventType type, string message)
        {
          await AddMapLogInnerAsync(isImportant, type, message);
        }

        var allCharacters = (await repo.Character.GetAllAliveWithIconAsync()).Select(c =>
        {
          c.Character.MainIcon = c.Icon;
          return c.Character;
        }).ToArray();
        var allTowns = await repo.Town.GetAllAsync();
        var allCountries = (await repo.Country.GetAllAsync()).Where(c => !c.HasOverthrown).ToArray();
        var allPolicies = await repo.Country.GetPoliciesAsync();
        var countryData = allCountries
          .GroupJoin(allTowns, c => c.Id, t => t.CountryId, (c, ts) => new { Country = c, Towns = ts, })
          .GroupJoin(allCharacters, d => d.Country.Id, c => c.CountryId, (c, cs) => new { c.Country, c.Towns, Characters = cs, })
          .GroupJoin(allPolicies, d => d.Country.Id, p => p.CountryId, (c, ps) => new { c.Country, c.Towns, c.Characters, Policies = ps, OldPolicyPoint = c.Country.PolicyPoint, });
        var allWars = await repo.CountryDiplomacies.GetAllWarsAsync();

        if (system.GameDateTime.Year == Config.UpdateStartYear && system.GameDateTime.Month == 1)
        {
          await PushNotificationService.SendAllAsync(repo, "武将更新開始", "武将のコマンド実行が開始されました。以降はコマンドが上から順番に実行されます");
        }
        if (system.IntGameDateTime == new GameDateTime
        {
          Year = Config.UpdateStartYear,
          Month = 1,
        }.AddMonth(Config.CountryBattleStopDuring).ToInt())
        {
          await PushNotificationService.SendAllAsync(repo, "戦闘解除", "戦闘が解除されました。以降は戦争が可能です（他国へ攻め込むには宣戦布告が必要です）");
        }

        if (system.GameDateTime.Year >= Config.UpdateStartYear)
        {
          // アイテムの再生成
          await ItemService.RegenerateItemOnTownsAsync(repo, allTowns);

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
                  .Sum(t =>
                  {
                    var val = (long)(system.GameDateTime.Month == 1 ? t.Commercial : t.Agriculture) * 8 * t.People / 10000;

                    if (countryData.Any(c => c.Country.Religion != ReligionType.Any && c.Country.Religion != ReligionType.None && c.Country.Religion != country.Country.Religion))
                    {
                      if (t.Religion == country.Country.Religion)
                      {
                        val = (int)(val * 1.05f);
                      }
                    }

                    return val;
                  }),
                PaidSalary = 0,
                AllContributions = country.Characters.Where(c => !c.AiType.IsSecretary()).Sum(c => c.Contribution),
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

              // 収入から政務官収入をひく
              var secretaries = country.Characters.Where(c => c.AiType.IsSecretary());
              if (secretaries.Any())
              {
                bool PayAndCanContinue(int income)
                {
                  var isContinue = false;
                  if (salary.AllSalary >= income)
                  {
                    isContinue = true;
                    salary.AllSalary -= income;
                  }
                  else
                  {
                    if (country.Country.SafeMoney >= income - salary.AllSalary)
                    {
                      // 収入が足りなければ国庫から絞る
                      isContinue = true;
                      country.Country.SafeMoney -= income - salary.AllSalary;
                      salary.AllSalary = 0;
                    }
                  }
                  return isContinue;
                }

                var secretaryIncome = Config.SecretaryCost;
                var scoutersIncome = Config.ScouterCost;
                foreach (var character in secretaries)
                {
                  var isContinue = PayAndCanContinue(secretaryIncome);

                  // 勤務を継続するか？
                  if (isContinue)
                  {
                    character.Money = 100000;
                    character.Rice = 100000;
                  }
                  else
                  {
                    character.Money = 0;
                    character.Rice = 0;
                  }
                }
              }

              // すべての武将に必要な収入の合計を計算する
              var lankSalary = country.Policies.Any(p => p.Status == CountryPolicyStatus.Available && p.Type == CountryPolicyType.AddSalary) ? 300 : 250;
              var neededAllSalaries = 0;
              var neededAllSalariesAllCharacters = 0;
              foreach (var character in country.Characters.Where(c => !c.AiType.IsSecretary()))
              {
                var currentLank = character.Lank;
                var addMax = 1500 + currentLank * lankSalary;
                if (character.Contribution > 0)
                {
                  neededAllSalaries += addMax;
                }
                neededAllSalariesAllCharacters += addMax;
              }
              country.Country.LastRequestedIncomes = neededAllSalariesAllCharacters;
              var isAllCharactersGetAddMax = neededAllSalaries <= salary.AllSalary;

              // 収入を武将に配る
              foreach (var character in country.Characters.Where(c => !c.AiType.IsSecretary()))
              {
                var currentLank = character.Lank;
                var add = salary.AllContributions > 0 ?
                  (int)(salary.AllSalary * (float)character.Contribution / salary.AllContributions + character.Contribution * 1.3f) : 0;
                var addMax = 1500 + currentLank * lankSalary;

                if (isAllCharactersGetAddMax && character.Contribution > 0)
                {
                  // すべての武将が最大数を受け取れる
                  add = addMax;
                }
                else
                {
                  add = Math.Min(Math.Max(add, 0), addMax);
                }
                salary.PaidSalary += add;

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
                  switch (RandomService.Next(1, 5))
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
                  var newAddMax = 1500 + newLank * lankSalary;
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

              // 徴収
              var policies = country.Policies.GetAvailableTypes();
              var collectionSize = policies.GetSumOfValues(CountryPolicyEffectType.CountrySafeCollectionMax);
              if (collectionSize > 0)
              {
                country.Country.SafeMoney = Math.Min(
                  CountryService.GetCountrySafeMax(
                    country.Policies.Where(p => p.Status == CountryPolicyStatus.Available).Select(p => p.Type)),
                  country.Country.SafeMoney + Math.Min(salary.AllSalary - salary.PaidSalary, collectionSize));
              }

              // 新しい収入、国庫を配信
              await StatusStreaming.Default.SendCountryAsync(ApiData.From(country.Country), country.Country.Id);
            }

            await repo.SaveChangesAsync();
          }

          // イベント
          if (RandomService.Next(0, 95) == 0)
          {
            var targetTown = allTowns[RandomService.Next(0, allTowns.Count)];
            var targetTowns = allTowns.GetAroundTowns(targetTown);

            void SetEvents(
              float size,
              float aroundSize,
              float sizeIfPolicy,
              float aroundSizeIfPolicy,
              CountryPolicyType policy,
              IEnumerable<Tuple<CountryPolicyType, Action<TownBase, Country>>> policyActions,
              Action<TownBase, float, IEnumerable<CountryPolicyType>, Country> func)
            {
              foreach (var town in targetTowns)
              {
                var cd = countryData.FirstOrDefault(c => c.Country.Id == town.CountryId);
                var policies = cd?.Policies.Where(p => p.Status == CountryPolicyStatus.Available).Select(p => p.Type) ?? Enumerable.Empty<CountryPolicyType>();
                var val = policies.Contains(policy) ? aroundSizeIfPolicy : aroundSize;
                func(town, val, policies, cd?.Country);

                if (policyActions != null && cd != null)
                {
                  var acts = cd.Policies
                    .Where(p => p.Status == CountryPolicyStatus.Available)
                    .Join(policyActions, p => p.Type, p => p.Item1, (p1, p2) => p2.Item2);
                  foreach (var a in acts)
                  {
                    a(town, cd.Country);
                  }
                }
              }
              var cd2 = countryData.FirstOrDefault(c => c.Country.Id == targetTown.CountryId);
              var policies2 = cd2?.Policies.Where(p => p.Status == CountryPolicyStatus.Available).Select(p => p.Type) ?? Enumerable.Empty<CountryPolicyType>();
              var val2 = policies2.Contains(policy) ? sizeIfPolicy : size;
              func(targetTown, val2, policies2, cd2?.Country);

              if (policyActions != null && cd2 != null)
              {
                var acts = cd2.Policies
                  .Where(p => p.Status == CountryPolicyStatus.Available)
                  .Join(policyActions, p => p.Type, p => p.Item1, (p1, p2) => p2.Item2);
                foreach (var a in acts)
                {
                  a(targetTown, cd2.Country);
                }
              }
            }

            var eventId = RandomService.Next(0, 8);
            switch (eventId)
            {
              case 0:
                await AddMapLogAsync(true, EventType.Event, "<town>" + targetTown.Name + "</town> 周辺でいなごの大群が畑を襲いました");
                SetEvents(0.65f, 0.75f, 0.8f, 0.9f, CountryPolicyType.Economy, null, (town, val, ps, c) =>
                {
                  town.Agriculture = (int)(town.Agriculture * val);
                });
                break;
              case 1:
                await AddMapLogAsync(true, EventType.Event, "<town>" + targetTown.Name + "</town> 周辺で洪水がおこりました");
                SetEvents(0.75f, 0.85f, 0.85f, 0.95f, CountryPolicyType.SaveWall, new List<Tuple<CountryPolicyType, Action<TownBase, Country>>>
              {
                new Tuple<CountryPolicyType, Action<TownBase, Country>>(CountryPolicyType.HelpRepair, (t, c) =>
                {
                  t.Security = (short)Math.Min(100, t.Security + 10);
                  c.PolicyPoint += 30;
                }),
              }, (town, val, ps, c) =>
              {
                town.Agriculture = (int)(town.Agriculture * val);
                town.Commercial = (int)(town.Commercial * val);
                town.Wall = (int)(town.Wall * val);
              });
                break;
              case 2:
                await AddMapLogAsync(true, EventType.Event, "<town>" + targetTown.Name + "</town> を中心に疫病が広がっています。町の人も苦しんでいます");
                SetEvents(0.75f, 0.85f, 0.85f, 0.95f, CountryPolicyType.Economy, null, (town, val, ps, c) =>
                {
                  town.People = (int)(town.People * val);
                });
                break;
              case 3:
                await AddMapLogAsync(true, EventType.Event, "<town>" + targetTown.Name + "</town> 周辺は、今年は豊作になりそうです");
                SetEvents(1.30f, 1.15f, 1.50f, 1.30f, CountryPolicyType.Economy, null, (town, val, ps, c) =>
                {
                  town.Agriculture = Math.Min((int)(town.Agriculture * val), town.AgricultureMax);
                });
                break;
              case 4:
                await AddMapLogAsync(true, EventType.Event, "<town>" + targetTown.Name + "</town> 周辺で地震が起こりました");
                SetEvents(0.66f, 0.76f, 0.8f, 0.9f, CountryPolicyType.SaveWall, new List<Tuple<CountryPolicyType, Action<TownBase, Country>>>
              {
                new Tuple<CountryPolicyType, Action<TownBase, Country>>(CountryPolicyType.HelpRepair, (t, c) =>
                {
                  t.Security = (short)Math.Min(100, t.Security + 10);
                  c.PolicyPoint += 30;
                }),
              }, (town, val, ps, c) =>
              {
                town.Agriculture = (int)(town.Agriculture * val);
                town.Commercial = (int)(town.Commercial * val);
                town.Wall = (int)(town.Wall * val);
                town.People = (int)(town.People * val);
              });
                break;
              case 5:
                await AddMapLogAsync(true, EventType.Event, "<town>" + targetTown.Name + "</town> 周辺の市場が賑わっています");
                SetEvents(1.30f, 1.15f, 1.50f, 1.30f, CountryPolicyType.Economy, null, (town, val, ps, c) =>
                {
                  town.Commercial = Math.Min((int)(town.Commercial * val), town.CommercialMax);
                });
                break;
              case 6:
                await AddMapLogAsync(true, EventType.Event, "<town>" + targetTown.Name + "</town> 周辺で賊が出現しました");
                SetEvents(0.66f, 0.76f, 0.8f, 0.9f, CountryPolicyType.AntiGang, null, (town, val, ps, c) =>
                {
                  town.Agriculture = (int)(town.Agriculture * val);
                  town.Commercial = (int)(town.Commercial * val);
                  town.People = (int)(town.People * val);
                  town.Security = (short)(town.Security * val);
                  if (ps.Contains(CountryPolicyType.KillGang) && c != null)
                  {
                    c.PolicyPoint += 30;
                  }
                });
                break;
              case 7:
                await AddMapLogAsync(true, EventType.Event, "<town>" + targetTown.Name + "</town> 周辺で義賊が貧しい人に施しをしています");
                SetEvents(1.40f, 1.20f, 1.50f, 1.30f, CountryPolicyType.Justice, null, (town, val, ps, c) =>
                {
                  town.Security = (short)Math.Min(town.Security * val, 100);
                });
                break;
            }

            await StatusStreaming.Default.SendCountryAsync(ApiData.From(targetTown), targetTown.CountryId);
            await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(targetTown), repo);
            foreach (var town in targetTowns)
            {
              await StatusStreaming.Default.SendCountryAsync(ApiData.From(town), town.CountryId);
              await StatusStreaming.Default.SendTownToAllAsync(ApiData.From((Town)town), repo);
            }
            await repo.SaveChangesAsync();
          }

          // アイテム変動
          if (system.GameDateTime.Year % 4 == 0 && system.GameDateTime.Month == 1)
          {
            var items = await repo.CharacterItem.GetAllAsync();
            var targetItems = items.Where(i => i.Status == CharacterItemStatus.TownHidden);
            if (system.GameDateTime.Year % 12 == 4)   // 午前5時
            {
              targetItems = items.Where(i => i.Status == CharacterItemStatus.TownHidden || i.Status == CharacterItemStatus.TownOnSale);
            }
            foreach (var item in targetItems)
            {
              item.TownId = RandomService.Next(allTowns).Id;
            }
            await StatusStreaming.Default.SendAllAsync(targetItems.Select(i => ApiData.From(i)));
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
              if (RandomService.Next(0, 2) == 0)
              {
                town.IntRicePrice += (int)(RandomService.NextDouble() * 0.5 * ricePriceBase);
                if (town.IntRicePrice > ricePriceMax)
                {
                  town.IntRicePrice = ricePriceMax;
                }
              }
              else
              {
                town.IntRicePrice -= (int)(RandomService.NextDouble() * 0.5 * ricePriceBase);
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
                if (town.TownBuilding == TownBuilding.OpenWall)
                {
                  peopleAdd = (int)((((float)town.TownBuildingValue / Config.TownBuildingMax) * 0.3f + 1.0f) * peopleAdd);
                }

                if (countryData
                  .Where(c => c.Country.Religion != ReligionType.None && c.Country.Religion != ReligionType.Any)
                  .GroupBy(c => c.Country.Religion)
                  .Count() > 1)
                {
                  var country = countryData.FirstOrDefault(c => c.Country.Id == town.Id);
                  if (town.Religion != ReligionType.None && town.Religion != ReligionType.Any && town.Religion == country?.Country.Religion)
                  {
                    peopleAdd = (int)(peopleAdd * 1.05f);
                  }
                }
              }
              else if (town.Security < 50)
              {
                peopleAdd = -80 * (50 - town.Security);
              }
              town.People += peopleAdd;
              var peopleMax = (int)(town.TownBuilding != TownBuilding.Houses ? town.PeopleMax : town.PeopleMax + ((float)town.TownBuildingValue / Config.TownBuildingMax) * 10000);
              if (town.People > peopleMax)
              {
                town.People = peopleMax;
              }
              else if (town.People < 0)
              {
                town.People = 0;
              }

              await StatusStreaming.Default.SendCountryAsync(ApiData.From(town), town.CountryId);
              await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(town), repo);
            }

            await repo.SaveChangesAsync();
          }

          // 政策に応じて毎ターン任意の政策ポイントを支給、その他特殊効果
          {
            foreach (var country in countryData)
            {
              var oldPoint = country.Country.PolicyPoint;
              var reinforcements = await repo.Reinforcement.GetByCountryIdAsync(country.Country.Id);

              // 政策開発
              country.Country.PolicyPoint += 5;
              if (system.GameDateTime.Year >= Config.UpdateStartYear + Config.CountryBattleStopDuring / 12 &&
                  country.Characters.Count(c => c.AiType == CharacterAiType.Human) -
                  reinforcements.Count(r => r.Status == ReinforcementStatus.Active && r.CharacterCountryId != country.Country.Id) +
                  reinforcements.Count(r => r.Status == ReinforcementStatus.Active && r.CharacterCountryId == country.Country.Id) <= 2)
              {
                country.Country.PolicyPoint += 3;
              }

              var availablePolicies = country.Policies.Where(p => p.Status == CountryPolicyStatus.Available).Select(p => p.Type);
              if (availablePolicies.Contains(CountryPolicyType.StrongCountry))
              {
                country.Country.PolicyPoint += country.Characters.Count(c => c.AiType == CharacterAiType.Human && c.GetCharacterType() == CharacterType.Strong) * 2;
              }
              if (availablePolicies.Contains(CountryPolicyType.IntellectCountry))
              {
                country.Country.PolicyPoint += country.Characters.Count(c => c.AiType == CharacterAiType.Human && c.GetCharacterType() == CharacterType.Intellect) * 4;
              }
              if (availablePolicies.Contains(CountryPolicyType.PopularityCountry))
              {
                country.Country.PolicyPoint += country.Characters.Count(c => c.AiType == CharacterAiType.Human && c.GetCharacterType() == CharacterType.Popularity) * 8;
              }

              var capital = country.Towns.FirstOrDefault(t => country.Country.CapitalTownId == t.Id);
              if (availablePolicies.Contains(CountryPolicyType.GunKen))
              {
                country.Country.PolicyPoint += country.Towns.Count(t => t.Type == TownType.Large) * 5;
              }
              if (availablePolicies.Contains(CountryPolicyType.AgricultureCountry))
              {
                country.Country.PolicyPoint += country.Towns.Count(t => t.Type == TownType.Agriculture) * 3;
                if (capital?.SubType == TownType.Agriculture)
                {
                  country.Country.PolicyPoint += 3;
                }
              }
              if (availablePolicies.Contains(CountryPolicyType.CommercialCountry))
              {
                country.Country.PolicyPoint += country.Towns.Count(t => t.Type == TownType.Commercial) * 3;
                if (capital?.SubType == TownType.Commercial)
                {
                  country.Country.PolicyPoint += 3;
                }
              }
              if (availablePolicies.Contains(CountryPolicyType.WallCountry))
              {
                country.Country.PolicyPoint += country.Towns.Count(t => t.Type == TownType.Fortress) * 3;
                if (capital?.SubType == TownType.Fortress)
                {
                  country.Country.PolicyPoint += 3;
                }
              }

              if (country.Country.PolicyPoint != oldPoint)
              {
                await StatusStreaming.Default.SendCountryAsync(ApiData.From(country.Country), country.Country.Id);
              }

              // 宗教による追加の政策ポイント
              if (countryData.Any(c => c.Country.Religion != ReligionType.Any && c.Country.Religion != ReligionType.None && c.Country.Religion != country.Country.Religion))
              {
                country.Country.PolicyPoint += allTowns.Count(t => t.Religion == country.Country.Religion) * 3;
              }
            }
          }

          // 都市施設
          {
            foreach (var town in allTowns)
            {
              var size = (float)town.TownBuildingValue / Config.TownBuildingMax;

              if (town.TownBuilding == TownBuilding.TrainStrong ||
                  town.TownBuilding == TownBuilding.TrainIntellect ||
                  town.TownBuilding == TownBuilding.TrainLeadership ||
                  town.TownBuilding == TownBuilding.TrainPopularity ||
                  town.TownBuilding == TownBuilding.TerroristHouse ||
                  town.TownBuilding == TownBuilding.TrainingBuilding ||
                  town.TownBuilding == TownBuilding.Sukiya ||
                  town.TownBuilding == TownBuilding.School)
              {
                var charas = allCharacters.Where(c => c.TownId == town.Id && c.CountryId == town.CountryId);
                var isConnected = countryData
                  .FirstOrDefault(c => c.Country.Id == town.CountryId)?
                  .Policies
                  .Any(p => p.Status == CountryPolicyStatus.Available && p.Type == CountryPolicyType.ConnectionBuildings) == true;
                var shortSize = (short)(size * 11);
                var connectedSize = (short)(size * 18);
                var schoolSize = (short)(size * 2);
                var schoolConnectedSize = (short)(size * 9);
                foreach (var chara in charas)
                {
                  var isNotify = false;
                  if (town.TownBuilding == TownBuilding.TrainStrong)
                  {
                    var o = chara.Strong;
                    chara.AddStrongEx(isConnected ? connectedSize : shortSize);
                    isNotify = chara.Strong != o;
                  }
                  else if (town.TownBuilding == TownBuilding.TrainIntellect)
                  {
                    var o = chara.Intellect;
                    chara.AddIntellectEx(isConnected ? connectedSize : shortSize);
                    isNotify = chara.Intellect != o;
                  }
                  else if (town.TownBuilding == TownBuilding.TrainLeadership)
                  {
                    var o = chara.Leadership;
                    chara.AddLeadershipEx(isConnected ? connectedSize : shortSize);
                    isNotify = chara.Leadership != o;
                  }
                  else if (town.TownBuilding == TownBuilding.TrainPopularity)
                  {
                    var o = chara.Popularity;
                    chara.AddPopularityEx(isConnected ? connectedSize : shortSize);
                    isNotify = chara.Popularity != o;
                  }
                  else if (town.TownBuilding == TownBuilding.School)
                  {
                    var o1 = chara.Strong;
                    var o2 = chara.Intellect;
                    var o3 = chara.Leadership;
                    var o4 = chara.Popularity;
                    chara.AddStrongEx((short)(isConnected && chara.CountryId == town.CountryId ? schoolConnectedSize : schoolSize));
                    chara.AddIntellectEx((short)(isConnected && chara.CountryId == town.CountryId ? schoolConnectedSize : schoolSize));
                    chara.AddLeadershipEx((short)(isConnected && chara.CountryId == town.CountryId ? schoolConnectedSize : schoolSize));
                    chara.AddPopularityEx((short)(isConnected && chara.CountryId == town.CountryId ? schoolConnectedSize : schoolSize));
                    isNotify = chara.Strong != o1 || chara.Intellect != o2 || chara.Leadership != o3 || chara.Popularity != o4;
                  }
                  else if (town.TownBuilding == TownBuilding.TerroristHouse)
                  {
                    chara.Proficiency = Math.Max(chara.Proficiency, (short)(60 * size));
                  }
                  else if (town.TownBuilding == TownBuilding.TrainingBuilding)
                  {
                    var formation = await repo.Character.GetFormationAsync(chara.Id, chara.FormationType);
                    formation.Experience += (int)(6 * size);
                    await StatusStreaming.Default.SendCharacterAsync(ApiData.From(formation), chara.Id);
                  }
                  else if (town.TownBuilding == TownBuilding.Sukiya)
                  {
                    if (RandomService.Next(1, 1001) <= (int)(8 * size))
                    {
                      var info = await ItemService.PickTownHiddenItemAsync(repo, chara.TownId, chara);
                      if (info.HasData)
                      {
                        await LogService.AddCharacterLogAsync(repo, system.GameDateTime, chara.Id, $"数寄屋から <town>{town.Name}</town> に隠されたアイテム {info.Data.Name} を手に入れました");
                      }
                    }
                  }

                  if (isNotify)
                  {
                    await CharacterService.StreamCharacterAsync(repo, chara);
                  }
                  else
                  {
                    await StatusStreaming.Default.SendCharacterAsync(ApiData.From(chara), chara.Id);
                  }
                }
              }
              else if (town.TownBuilding == TownBuilding.RepairWall)
              {
                town.Wall = Math.Min((int)(town.Wall + 15 * size), town.WallMax);
              }
              else if (town.TownBuilding == TownBuilding.MilitaryStation)
              {
                if (town.Security >= 10)
                {
                  town.Security = (short)Math.Min((int)(town.Security + 8 * size), 100);
                }
              }
              else if (town.TownBuilding == TownBuilding.Extension)
              {
                if (system.GameDateTime.Month % 6 == 1)
                {
                  town.WallMax += (int)(3 * size);
                }
              }
              else if (town.TownBuilding == TownBuilding.Palace ||
                town.TownBuilding == TownBuilding.Casting)
              {
                var cc = await repo.Country.GetAliveByIdAsync(town.CountryId);
                if (cc.HasData)
                {
                  if (town.TownBuilding == TownBuilding.Palace)
                  {
                    cc.Data.PolicyPoint += (short)(9 * size);
                  }
                  else if (town.TownBuilding == TownBuilding.Casting)
                  {
                    cc.Data.SafeMoney += (int)(400 * size);
                  }
                }
              }
            }
          }

          // 都市建築物の建設または撤去
          {
            var subBuildings = await repo.Town.GetSubBuildingsAsync();
            foreach (var subBuilding in subBuildings.Where(s => (s.Status == TownSubBuildingStatus.UnderConstruction || s.Status == TownSubBuildingStatus.Removing) && s.IntStatusFinishGameDateTime <= system.IntGameDateTime))
            {
              var isError = false;

              var infoOptional = TownSubBuildingTypeInfoes.Get(subBuilding.Type);
              if (!infoOptional.HasData)
              {
                isError = true;
              }
              var info = infoOptional.Data;

              var chara = allCharacters.FirstOrDefault(c => c.Id == subBuilding.CharacterId);
              if (chara == null)
              {
                isError = true;
              }

              var town = allTowns.FirstOrDefault(t => t.Id == subBuilding.TownId);
              if (town == null)
              {
                isError = true;
              }

              if (isError)
              {
                repo.Town.RemoveSubBuilding(subBuilding);
              }
              else if (subBuilding.Status == TownSubBuildingStatus.UnderConstruction)
              {
                if (town.CountryId != chara.CountryId)
                {
                  await AddLogAsync(chara.Id, $"<town>{town.Name}</town> で {info.Name} を建設しているところでしたが、都市が別の国に支配されたため作業は中止されました");
                  repo.Town.RemoveSubBuilding(subBuilding);
                  subBuilding.Status = TownSubBuildingStatus.Unknown;
                }
                else
                {
                  await AddLogAsync(chara.Id, $"<town>{town.Name}</town> で {info.Name} の建設が完了しました");
                  subBuilding.Status = TownSubBuildingStatus.Available;
                  info.OnBuilt?.Invoke(town);
                }
              }
              else if (subBuilding.Status == TownSubBuildingStatus.Removing)
              {
                if (town.CountryId != chara.CountryId)
                {
                  await AddLogAsync(chara.Id, $"<town>{town.Name}</town> で {info.Name} を撤去しているところでしたが、都市が別の国に支配されたため作業は中止されました");
                  subBuilding.Status = TownSubBuildingStatus.Available;
                  info.OnBuilt?.Invoke(town);
                }
                else
                {
                  await AddLogAsync(chara.Id, $"<town>{town.Name}</town> で {info.Name} の撤去が完了しました");
                  repo.Town.RemoveSubBuilding(subBuilding);
                  subBuilding.Status = TownSubBuildingStatus.Unknown;
                }
              }
              else
              {
                repo.Town.RemoveSubBuilding(subBuilding);
                isError = true;
              }

              if (isError)
              {
                subBuilding.Status = TownSubBuildingStatus.Unknown;
              }

              if (!isError)
              {
                await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(town), repo);
              }
              await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(subBuilding), repo, town);
            }

            // 建築物の効果（謀略建築物含む）
            var wars = allWars.Where(w => w.Status == CountryWarStatus.InReady || w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.StopRequesting);
            var availableSubBuildings = subBuildings.Where(s => s.Status == TownSubBuildingStatus.Available);
            foreach (var subBuilding in availableSubBuildings)
            {
              var town = allTowns.FirstOrDefault(t => t.Id == subBuilding.TownId);
              if (town == null)
              {
                continue;
              }
              var country = countryData.FirstOrDefault(c => c.Country.Id == town.CountryId);
              if (country == null)
              {
                continue;
              }
              var aroundTowns = allTowns.GetAroundTowns(town);
              var countryWarTargets = wars.Where(w => w.IsJoin(country.Country.Id)).Select(w => w.GetEnemy(country.Country.Id));

              if (subBuilding.Type == TownSubBuildingType.CommercialUnion)
              {
                if (system.GameDateTime.Month == 1 || system.GameDateTime.Month == 7)
                {
                  var max = CountryService.GetCountrySafeMax(country.Policies.GetAvailableTypes());
                  if (country.Country.SafeMoney < max)
                  {
                    country.Country.SafeMoney = Math.Min(max, country.Country.SafeMoney + RandomService.Next(4000, 8001));
                  }
                }
              }
              else if (subBuilding.Type == TownSubBuildingType.BreakWall)
              {
                if (system.GameDateTime.Month % 2 == 0)
                {
                  foreach (var atown in aroundTowns.Where(at => countryWarTargets.Contains(at.CountryId)))
                  {
                    var isDefense = availableSubBuildings.Any(b => b.TownId == atown.Id && b.Type == TownSubBuildingType.DefenseStation);
                    var value = 7 / (isDefense ? 3 : 1);
                    atown.Wall = Math.Max(0, atown.Wall - value);
                  }
                }
              }
              else if (subBuilding.Type == TownSubBuildingType.BreakTechnology)
              {
                if (system.GameDateTime.Month % 2 == 0)
                {
                  foreach (var atown in aroundTowns.Where(at => countryWarTargets.Contains(at.CountryId)))
                  {
                    var isDefense = availableSubBuildings.Any(b => b.TownId == atown.Id && b.Type == TownSubBuildingType.DefenseStation);
                    var value = 7 / (isDefense ? 3 : 1);
                    atown.Technology = Math.Max(0, atown.Technology - value);
                  }
                }
              }
              else if (subBuilding.Type == TownSubBuildingType.Agitation)
              {
                if (system.GameDateTime.Month % 2 == 0)
                {
                  foreach (var atown in aroundTowns.Where(at => countryWarTargets.Contains(at.CountryId)))
                  {
                    var isDefense = availableSubBuildings.Any(b => b.TownId == atown.Id && b.Type == TownSubBuildingType.DefenseStation);
                    var value = Math.Max(4 / (isDefense ? 3 : 1), 1);
                    var value2 = 600 / (isDefense ? 3 : 1);
                    atown.Security = (short)Math.Max(0, atown.Security - value);
                    atown.People = Math.Max(0, atown.People - value2);
                  }
                }
              }
              else if (subBuilding.Type == TownSubBuildingType.Cathedral)
              {
                if (system.GameDateTime.Month % 2 == 0)
                {
                  var religion = country.Country.Religion;

                  if (religion == ReligionType.Confucianism)
                  {
                    town.Confucianism += 8;
                  }
                  if (religion == ReligionType.Buddhism)
                  {
                    town.Buddhism += 8;
                  }
                  if (religion == ReligionType.Taoism)
                  {
                    town.Taoism += 8;
                  }
                  if (religion != ReligionType.Any && religion != ReligionType.None && religion == town.Religion)
                  {
                    town.TechnologyMax += 3;
                    town.WallMax += 7;
                  }

                  foreach (var atown in aroundTowns)
                  {
                    if (religion == ReligionType.Confucianism)
                    {
                      atown.Confucianism += 4;
                    }
                    if (religion == ReligionType.Buddhism)
                    {
                      atown.Buddhism += 4;
                    }
                    if (religion == ReligionType.Taoism)
                    {
                      atown.Taoism += 4;
                    }
                  }
                }
              }
            }

            await repo.SaveChangesAsync();
          }

          // 同盟破棄・戦争開始
          {
            var targetAlliances = system.IsBattleRoyaleMode ?
              await repo.CountryDiplomacies.GetAllAlliancesAsync() :
              await repo.CountryDiplomacies.GetBreakingAlliancesAsync();
            foreach (var alliance in targetAlliances)
            {
              alliance.BreakingDelay--;
              if (alliance.BreakingDelay <= 0 || system.IsBattleRoyaleMode)
              {
                alliance.Status = CountryAllianceStatus.Broken;
                if (alliance.IsPublic && alliance.Status != CountryAllianceStatus.ChangeRequestingValue)
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
            foreach (var war in allWars
              .Where(cw => cw.Status == CountryWarStatus.InReady || cw.Status == CountryWarStatus.StopRequesting)
              .Where(cw => cw.IntStartGameDate == system.IntGameDateTime))
            {
              var country1 = allCountries.FirstOrDefault(c => c.Id == war.RequestedCountryId);
              var country2 = allCountries.FirstOrDefault(c => c.Id == war.InsistedCountryId);
              if (country1 != null && country2 != null)
              {
                await AddMapLogAsync(true, EventType.WarStart, "<country>" + country1.Name + "</country> と <country>" + country2.Name + "</country> の戦争が始まりました");
                await PushNotificationService.SendCountryAsync(repo, "戦争", $"{country2.Name} との戦争が始まりました", country1.Id);
                await PushNotificationService.SendCountryAsync(repo, "戦争", $"{country1.Name} との戦争が始まりました", country2.Id);
              }
              if (war.Status == CountryWarStatus.InReady)
              {
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
                  await StatusStreaming.Default.SendAllAsync(ApiData.From(war));
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

          // 遅延効果
          var isKokinForce = false;
          {
            var delays = await repo.DelayEffect.GetAllAsync();
            foreach (var delay in delays)
            {
              var isRemove = false;
              if (delay.Type == DelayEffectType.TownInvestment)
              {
                isRemove = await TownInvestCommand.ResultAsync(repo, system, delay, AddLogAsync);
              }
              if (delay.Type == DelayEffectType.GenerateItem)
              {
                isRemove = await GenerateItemCommand.ResultAsync(repo, system, delay, AddLogAsync);
              }
              if (delay.Type == DelayEffectType.TerroristEnemy)
              {
                var targets = allCountries.Where(c => !c.HasOverthrown && c.AiType == CountryAiType.Terrorists);
                if (!targets.Any())
                {
                  var target = await AiService.CreateTerroristCountryAsync(repo, async (type, message, isImportant) => await AddMapLogAsync(isImportant, type, message));
                  if (target != null)
                  {
                    targets = targets.Append(target);
                  }
                }
                foreach (var c in targets)
                {
                  c.AiType = CountryAiType.TerroristsEnemy;
                  await AddMapLogAsync(true, EventType.AppendTerrorists, $"異民族 <country>{c.Name}</country> は敵対化しました");
                  isRemove = true;
                }
              }
              if (delay.Type == DelayEffectType.AppearKokin)
              {
                isKokinForce = true;
                isRemove = true;
              }
              if (isRemove)
              {
                repo.DelayEffect.Remove(delay);
                if (delay.CharacterId != 0)
                {
                  delay.Type = DelayEffectType.Undefined;
                  await StatusStreaming.Default.SendCharacterAsync(ApiData.From(delay), delay.CharacterId);
                }
              }
            }
          }

          // 有効期限の存在する政策
          {
            foreach (var country in countryData)
            {
              foreach (var policy in country.Policies.Where(p => p.Status == CountryPolicyStatus.Availabling))
              {
                var info = CountryPolicyTypeInfoes.Get(policy.Type);
                if (info.HasData)
                {
                  if (system.IntGameDateTime - policy.IntGameDate >= info.Data.AvailableDuring)
                  {
                    policy.Status = CountryPolicyStatus.Available;
                    await StatusStreaming.Default.SendCountryAsync(ApiData.From(policy), country.Country.Id);
                  }
                }
              }
            }
          }

          // 保留中アイテムの廃棄
          {
            var items = await repo.CharacterItem.GetAllAsync();
            foreach (var item in items.Where(i => i.Status == CharacterItemStatus.CharacterPending && i.IntLastStatusChangedGameDate + 288 <= system.IntGameDateTime))
            {
              var chara = allCharacters.FirstOrDefault(c => c.Id == item.CharacterId);
              await ItemService.ReleaseCharacterAsync(repo, item, chara);
              var info = CharacterItemInfoes.Get(item.Type);
              if (info.HasData)
              {
                await AddLogAsync(chara.Id, $"アイテム {info.Data.Name} は保留期限を超過したので手放しました");
              }
            }
          }

          // AI国家
          {
            // 出現
            if (allTowns.Any(t => t.CountryId == 0) &&
              system.ManagementCountryCount < 1 &&
              system.RuleSet != GameRuleSet.SimpleBattle)
            {
              var isCreated = await AiService.CreateManagedCountryAsync(repo, (type, message, isImportant) => AddMapLogAsync(isImportant, type, message));
              if (isCreated)
              {
                system.ManagementCountryCount++;
              }
            }

            // 思考
            foreach (var country in allCountries.Where(c => c.AiType == CountryAiType.Managed))
            {
              var c = await AiCountryFactory.CreateAsync(repo, country);
              await c.RunAsync(repo);
            }
          }

          // 異民族
          var countryCount = allCountries.Count(c => !c.HasOverthrown);
          if (system.TerroristCount < 1 && system.RuleSet != GameRuleSet.SimpleBattle)
          {
            var created = await AiService.CreateTerroristCountryAsync(repo, (type, message, isImportant) => AddMapLogAsync(isImportant, type, message));
            if (created != null)
            {
              system.TerroristCount++;
              await repo.SaveChangesAsync();
            }
          }

          // 農民反乱
          if (!system.IsWaitingReset && !system.IsBattleRoyaleMode && system.RuleSet != GameRuleSet.SimpleBattle && RandomService.Next(0, 40) == 0)
          {
            var isCreated = await AiService.CreateFarmerCountryAsync(repo, (type, message, isImportant) => AddMapLogAsync(isImportant, type, message));
            if (isCreated)
            {
              await repo.SaveChangesAsync();
            }
            else
            {
              _logger.LogInformation("農民反乱の乱数条件を満たしましたが、その他の条件を満たさなかったために出現しませんでした");
            }
          }

          // 黄巾の出現とバトルロワイヤルモード
          var lastBattleMonth = await repo.BattleLog.GetLastBattleMonthAsync();
          if (!system.IsBattleRoyaleMode && !system.IsWaitingReset && system.RuleSet != GameRuleSet.SimpleBattle &&
            (lastBattleMonth.ToInt() + 12 * 12 * 6 == system.IntGameDateTime ||
            (system.GameDateTime.Year == 348 && system.GameDateTime.Month == 1)))
          {
            await AddMapLogAsync(true, EventType.Event, "黄巾が反乱の時期を伺っています");
          }
          if (!system.IsWaitingReset && (!system.IsBattleRoyaleMode && system.RuleSet != GameRuleSet.SimpleBattle && RandomService.Next(0, 70) == 0 || isKokinForce))
          {
            if ((!allWars.Any(w => w.IntStartGameDate + 3 >= system.IntGameDateTime) &&
              lastBattleMonth.ToInt() + 12 * 12 * 7 <= system.IntGameDateTime) ||
              (system.GameDateTime.Year >= 360) || isKokinForce)
            {
              // 候補都市一覧
              var townData = allTowns.Select(t => new
              {
                Town = t,
                AroundTowns = allTowns.GetAroundTowns(t),
                CountryTownCount = allTowns.Count(tt => t.CountryId == tt.CountryId),
              }).ToArray();
              var aroundTownsCountMax = townData.Max(t => t.AroundTowns.Count());
              var candidateBorderCount = Math.Min(aroundTownsCountMax, 4);

              // 対象都市
              var targetTownData = RandomService.Next(townData.Where(td => td.AroundTowns.Count() >= candidateBorderCount && td.CountryTownCount > 1));

              // 蜂起
              await AiService.CreateFarmerCountryAsync(repo, targetTownData.Town, (type, message, isImportant) => AddMapLogAsync(isImportant, type, message), true, true);
              foreach (var town in targetTownData.AroundTowns)
              {
                await AiService.CreateFarmerCountryAsync(repo, (Town)town, (type, message, isImportant) => AddMapLogAsync(isImportant, type, message), true);
              }

              system.IsBattleRoyaleMode = true;
              await AddMapLogAsync(true, EventType.WarStart, "全国戦争状態に突入しました");
              await PushNotificationService.SendAllAsync(repo, "全国戦争", "全国戦争状態に突入しました");
              await repo.SaveChangesAsync();
            }
          }
          if (!system.IsWaitingReset && (system.GameDateTime.Year == 240 || system.GameDateTime.Year == 300) && system.GameDateTime.Month == 1)
          {
            foreach (var town in allTowns)
            {
              town.WallMax /= 2;
              town.Wall = Math.Min(town.Wall, town.WallMax);
            }
            await AddMapLogAsync(true, EventType.Event, "巨大地震が発生し、全都市の城壁が大幅に破壊されました");
            await repo.SaveChangesAsync();
          }
          if (!system.IsWaitingReset && system.GameDateTime.Year == 360 && system.GameDateTime.Month == 1)
          {
            foreach (var town in allTowns)
            {
              town.WallMax = 1;
              town.Wall = Math.Min(town.Wall, town.WallMax);
            }
            await AddMapLogAsync(true, EventType.Event, "巨大隕石が落下し、全都市の城壁が修復不可能になりました");
            await repo.SaveChangesAsync();
          }

          // 全国戦争ルール
          if (system.RuleSet == GameRuleSet.BattleRoyale &&
            !system.IsBattleRoyaleMode &&
            system.GameDateTime.Year >= 60)
          {
            system.IsBattleRoyaleMode = true;
            await AddMapLogAsync(true, EventType.WarStart, "全国戦争状態に突入しました");
            await PushNotificationService.SendAllAsync(repo, "全国戦争", "全国戦争状態に突入しました");
            await repo.SaveChangesAsync();
          }

          // 蛮族
          if (Config.Game.IsThief &&
              allTowns.Any(t => t.CountryId == 0) &&
              allCountries.Count(c => !c.HasOverthrown && c.AiType == CountryAiType.Thiefs) < 2 &&
              RandomService.Next(0, 92) == 0)
          {
            var isCreated = await AiService.CreateThiefCountryAsync(repo, (type, message, isImportant) => AddMapLogAsync(isImportant, type, message));
            if (isCreated)
            {
              await repo.SaveChangesAsync();
            }
            else
            {
              _logger.LogInformation("蛮族出現の乱数条件を満たしましたが、その他の条件を満たさなかったために出現しませんでした");
            }
          }

          // 玉璽
          if (!system.IsWaitingReset && !system.IsBattleRoyaleMode && system.RuleSet != GameRuleSet.SimpleBattle)
          {
            var isSave = false;
            foreach (var country in allCountries.Where(c => c.GyokujiStatus == CountryGyokujiStatus.HasFake && c.IntGyokujiGameDate + 10 * 12 * 12 <= system.IntGameDateTime))
            {
              if (system.RuleSet == GameRuleSet.Gyokuji || !allWars.Any(w => w.IsJoinAvailable(country.Id)))
              {
                country.GyokujiStatus = CountryGyokujiStatus.NotHave;
                await AddMapLogAsync(true, EventType.Gyokuji, $"<country>{country.Name}</country> の持っている玉璽はまがい物でした");
                await StatusStreaming.Default.SendAllExceptForCountryAsync(ApiData.From(new CountryForAnonymous(country)), country.Id);
                isSave = true;
              }
            }

            var gyokujiWinner = allCountries.FirstOrDefault(c => c.GyokujiStatus == CountryGyokujiStatus.HasGenuine && c.IntGyokujiGameDate + 10 * 12 * 12 <= system.IntGameDateTime);
            if (gyokujiWinner != null)
            {
              if (system.RuleSet == GameRuleSet.Gyokuji || !allWars.Any(w => w.IsJoinAvailable(gyokujiWinner.Id)))
              {
                await AddMapLogAsync(true, EventType.Gyokuji, $"<country>{gyokujiWinner.Name}</country> は玉璽を行使し、天下に覇を唱えました");
                await CountryService.UnifyCountryAsync(repo, gyokujiWinner);
                isSave = true;
              }
            }

            if (isSave)
            {
              await repo.SaveChangesAsync();
            }
          }

          // 玉璽の配布
          if (!system.IsWaitingReset &&
            !system.IsBattleRoyaleMode &&
            system.RuleSet != GameRuleSet.SimpleBattle &&
            allCountries.All(c => c.GyokujiStatus == CountryGyokujiStatus.NotHave || c.GyokujiStatus == CountryGyokujiStatus.Refused) && system.GameDateTime.Year >= Config.UpdateStartYear + Config.CountryBattleStopDuring / 12)
          {
            // 異民族や経営国家を含めるため、改めて取得して確認し直す
            var countries2 = await repo.Country.GetAllAsync();
            var countries = countries2.Where(c => c.AiType != CountryAiType.Terrorists);
            if (countries.All(c => c.GyokujiStatus == CountryGyokujiStatus.NotHave || c.GyokujiStatus == CountryGyokujiStatus.Refused))
            {
              var targetCountries = countries.Where(c => c.GyokujiStatus == CountryGyokujiStatus.NotHave && allTowns.Any(t => t.CountryId == c.Id));
              var gets = new List<Country>();

              if (system.RuleSet == GameRuleSet.Gyokuji)
              {
                foreach (var c in targetCountries)
                {
                  gets.Add(c);
                }
              }
              else
              {
                var num = Math.Min(targetCountries.Count(), RandomService.Next(2, 4));
                for (var i = 0; i < num; i++)
                {
                  var isHit = false;
                  while (!isHit)
                  {
                    var tmp = RandomService.Next(targetCountries);
                    if (!gets.Any(c => c.Id == tmp.Id))
                    {
                      gets.Add(tmp);
                      isHit = true;
                    }
                  }
                }
              }

              // 人間の国がなければ異民族にも玉璽をもたせる
              if (!targetCountries.Any(c => c.AiType == CountryAiType.Human))
              {
                var terrorist = countries2.FirstOrDefault(c => c.AiType == CountryAiType.Terrorists);
                if (terrorist != null)
                {
                  gets.Add(terrorist);
                }
              }

              foreach (var country in gets)
              {
                country.GyokujiStatus = CountryGyokujiStatus.HasFake;
                country.IntGyokujiGameDate = system.IntGameDateTime;
                await AddMapLogAsync(true, EventType.Gyokuji, $"<country>{country.Name}</country> は玉璽を手に入れました");
                await StatusStreaming.Default.SendAllExceptForCountryAsync(ApiData.From(new CountryForAnonymous(country)), country.Id);
              }
              RandomService.Next(gets).GyokujiStatus = CountryGyokujiStatus.HasGenuine;

              foreach (var country in countries.Where(c => c.GyokujiStatus == CountryGyokujiStatus.Refused))
              {
                country.GyokujiStatus = CountryGyokujiStatus.NotHave;
              }

              await repo.SaveChangesAsync();
            }
          }

          // 宗教勝利
          if (!system.IsWaitingReset)
          {
            var religionOfTown = allTowns.GroupBy(t => t.Religion);
            var religionOfCountry = allCountries.Where(c => !c.HasOverthrown && c.Religion != ReligionType.Any && c.Religion != ReligionType.None).GroupBy(c => c.Religion);
            if (religionOfTown.Count() == 1 && religionOfCountry.Count() == 1)
            {
              var religion = religionOfTown.First().Key;
              if (religion != ReligionType.Any && religion != ReligionType.None)
              {
                var countryGroup = religionOfCountry.First();
                if (countryGroup.Count() == 1 && countryGroup.Key == religion)
                {
                  var winner = countryGroup.First();
                  await AddMapLogAsync(true, EventType.ReligionWin, $"<country>{winner.Name}</country> がすべての都市の宗教を掌握して勝利しました");
                  await CountryService.UnifyCountryAsync(repo, winner);
                  await repo.SaveChangesAsync();
                }
              }
            }
          }

          // 宣教師の追加
          // if (system.GameDateTime.Year < Config.UpdateStartYear + Config.CountryBattleStopDuring / 12 && system.GameDateTime.Year % 10 == 0 && system.GameDateTime.Month == 1)
          {
            var character = await AiService.CreateCharacterAsync(repo, new CharacterAiType[] { CharacterAiType.FreeEvangelist, }, 0, RandomService.Next(allTowns).Id, system, CharacterFrom.Unknown, CharacterSkillType.Undefined);
            if (character.Any())
            {
              character.First().Name += character.First().Id;

              var religion = character.First().Religion;
              var skill = CharacterSkillType.Undefined;
              if (religion == ReligionType.Confucianism)
              {
                character.First().From = CharacterFrom.Confucianism;
                skill = CharacterSkillType.Confucianism1;
              }
              if (religion == ReligionType.Taoism)
              {
                character.First().From = CharacterFrom.Taoism;
                skill = CharacterSkillType.Taoism1;
              }
              if (religion == ReligionType.Buddhism)
              {
                character.First().From = CharacterFrom.Buddhism;
                skill = CharacterSkillType.Buddhism1;
              }
              await repo.Character.AddSkillAsync(new CharacterSkill
              {
                CharacterId = character.First().Id,
                Status = CharacterSkillStatus.Available,
                Type = skill,
              });
              await AddMapLogAsync(false, EventType.NewEvangelist, $"無所属に新たに <character>{character.First().Name}</character> が出現しました");
            }
          }

          // 時間による勝利
          if (!system.IsWaitingReset && system.GameDateTime.Year >= 500)
          {
            var winner = countryData.FirstOrDefault();
            var targets = countryData.Where(cd => cd.Towns.Any());
            if (targets.Any())
            {
              winner = targets.First();
              if (targets.Count() > 1)
              {
                var townRanking = targets.OrderByDescending(cd => cd.Towns.Count());
                var characterRanking = targets.OrderByDescending(cd => cd.Characters.Count());
                var idRanking = targets.OrderBy(cd => cd.Country.Id);
                if (townRanking.ElementAt(0).Towns.Count() > townRanking.ElementAt(1).Towns.Count())
                {
                  winner = townRanking.First();
                }
                else if (characterRanking.ElementAt(0).Characters.Count() > characterRanking.ElementAt(1).Characters.Count())
                {
                  winner = characterRanking.First();
                }
                else
                {
                  winner = idRanking.First();
                }
              }

              await AddMapLogAsync(true, EventType.TimeWin, $"時間経過により <country>{winner.Country.Name}</country> が勝利しました");
              await CountryService.UnifyCountryAsync(repo, winner.Country);
              await repo.SaveChangesAsync();
            }
            else
            {
              await AddMapLogAsync(true, EventType.TimeWin, $"今期は時間経過により終了しました");
              await CountryService.UnifyCountryAsync(repo, null);
              await repo.SaveChangesAsync();
            }
          }

          // 戦争状態にないAI国家がどっかに布告するようにする
          if (!system.IsBattleRoyaleMode && allCountries.Where(c => !c.HasOverthrown).Any(c => c.AiType != CountryAiType.Human && c.AiType != CountryAiType.Managed))
          {
            var month = AiService.GetWarStartDateTime(system.GameDateTime, AiCountryWarStartDatePolicy.First21);
            var nextMonth = AiService.GetWarStartDateTime(system.GameDateTime.NextMonth(), AiCountryWarStartDatePolicy.First21);
            if (month.ToInt() != nextMonth.ToInt())
            {
              var isCreated = await AiService.CreateWarIfNotWarAsync(repo, month);
              if (isCreated)
              {
                await repo.SaveChangesAsync();
              }
            }
          }
        }

        // 国情報を更新
        foreach (var country in countryData)
        {
          country.Country.LastPolicyPointIncomes = country.Country.PolicyPoint - country.OldPolicyPoint;
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
        await StatusStreaming.Default.SendAllAsync(ApiData.From(system));
        foreach (var country in allCountries)
        {
          await StatusStreaming.Default.SendCountryAsync(ApiData.From(country), country.Id);
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"DATE: {system?.IntGameDateTime ?? 0} 月次更新処理中にエラーが発生しました");
        await Task.Delay(3000);
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
      var system = await repo.System.GetAsync();
      var notifies = new List<IApiData>();
      var anonymousNotifies = new List<ApiData<MapLog>>();
      var anyoneNotifies = new List<Tuple<uint, IApiData>>();
      var currentMonth = character.LastUpdatedGameDate.NextMonth();
      var oldStrong = character.Strong;
      var oldIntellect = character.Intellect;
      var oldLeadership = character.Leadership;
      var oldPopularity = character.Popularity;
      var oldTownId = character.TownId;
      var skills = await repo.Character.GetSkillsAsync(character.Id);

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

      try
      {

        // 技能
        if (currentMonth.Year >= Config.UpdateStartYear)
        {
          var skillStrongEx = skills.GetSumOfValues(CharacterSkillEffectType.StrongExRegularly);
          var skillIntellectEx = skills.GetSumOfValues(CharacterSkillEffectType.IntellectExRegularly);
          var skillLeadershipEx = skills.GetSumOfValues(CharacterSkillEffectType.LeadershipExRegularly);
          var skillPopularityEx = skills.GetSumOfValues(CharacterSkillEffectType.PopularityExRegularly);
          if (character.GetCharacterType() == CharacterType.Strong)
          {
            skillStrongEx += skills.GetSumOfValues(CharacterSkillEffectType.StrongOrIntellectExRegularly);
          }
          else
          {
            skillIntellectEx += skills.GetSumOfValues(CharacterSkillEffectType.StrongOrIntellectExRegularly);
          }
          character.AddStrongEx((short)skillStrongEx);
          character.AddIntellectEx((short)skillIntellectEx);
          character.AddLeadershipEx((short)skillLeadershipEx);
          character.AddPopularityEx((short)skillPopularityEx);

          var skillFormationEx = skills.GetSumOfValues(CharacterSkillEffectType.FormationExRegularly);
          if (skillFormationEx > 0)
          {
            var formation = await repo.Character.GetFormationAsync(character.Id, character.FormationType);
            if (formation != null)
            {
              formation.Experience += skillFormationEx;
              await repo.SaveChangesAsync();
            }
          }

          var addMoney = skills.GetSumOfValues(CharacterSkillEffectType.MoneyRegularly);
          character.Money += addMoney;
        }

        // 技能ポイントが十分にあるときは自動で技能獲得
        {
          var nextInfos = skills.GetNextSkills();
          if (nextInfos.Count == 1 && nextInfos[0].RequestedPoint <= character.SkillPoint)
          {
            await SkillService.SetCharacterAndSaveAsync(repo, new CharacterSkill
            {
              CharacterId = character.Id,
              Status = CharacterSkillStatus.Available,
              Type = nextInfos[0].Type,
            }, character);
            character.SkillPoint -= nextInfos[0].RequestedPoint;
            await AddLogAsync($"技能ポイントが一定まで到達したので、技能 {nextInfos[0].Name} を獲得しました");
          }
        }

        // ルールセットによる支給
        if (currentMonth.Year >= Config.UpdateStartYear)
        {
          if (system.RuleSet == GameRuleSet.BattleRoyale)
          {
            character.Money += 3000;
          }
        }

        var ai = AiCharacterFactory.Create(character);
        var commandOptional = await ai.GetCommandAsync(repo, currentMonth);
        var blockTypes = await repo.BlockAction.GetAvailableTypesAsync(character.Id);
        var isBlockedCommand = false;
        if (blockTypes.Contains(BlockActionType.StopCommand))
        {
          if (!commandOptional.HasData || commandOptional.Data.Type == CharacterCommandType.None)
          {
            character.DeleteTurn++;
          }
          isBlockedCommand = true;
          await AddLogAsync("管理人により行動を制限されています。コマンド実行をスキップしました。放置削除が進行することがあります");
        }
        else if (blockTypes.Contains(BlockActionType.StopCommandAndDeleteTurn))
        {
          isBlockedCommand = true;
          await AddLogAsync("管理人により行動を制限されています。コマンド実行をスキップしました");
        }
        else if (blockTypes.Contains(BlockActionType.StopCommandByMonarch))
        {
          if (!commandOptional.HasData || commandOptional.Data.Type == CharacterCommandType.None)
          {
            character.DeleteTurn++;
          }
          isBlockedCommand = true;
          await AddLogAsync("君主によりコマンド実行が制限されています。コマンド実行をスキップしました。放置削除が進行することがあります");
        }
        else
        {
          var isCommandExecuted = false;
          async Task RunCommandAsync(CharacterCommand cmd)
          {
            var commandRunnerOptional = Commands.Commands.Get(cmd.Type);
            if (commandRunnerOptional.HasData)
            {
              var commandRunner = commandRunnerOptional.Data;
              await commandRunner.ExecuteAsync(repo, character, cmd.Parameters, gameObj);
              isCommandExecuted = true;

              if (commandRunner.Type != CharacterCommandType.None)
              {
                character.DeleteTurn = 0;
              }
            }
          }

          // コマンドの実行
          if (currentMonth.Year >= Config.UpdateStartYear)
          {
            if (commandOptional.HasData)
            {
              var command = commandOptional.Data;
              await RunCommandAsync(command);
            }
            if (!isCommandExecuted)
            {
              await Commands.Commands.EmptyCommand.ExecuteAsync(repo, character, new CharacterCommandParameter[] { }, gameObj);
            }
          }

          // 定期コマンドの実行
          var regulars = await repo.CharacterCommand.GetRegularlyCommandsAsync(character.Id);
          foreach (var r in regulars)
          {
            var command = new CharacterCommand
            {
              Type = r.Type,
              GameDateTime = currentMonth,
              CharacterId = character.Id,
            };
            command.Parameters.Add(new CharacterCommandParameter
            {
              Type = CharacterCommandParameterTypes.Regularly,
            });
            if (r.Option1 != 0)
            {
              command.Parameters.Add(new CharacterCommandParameter
              {
                Type = 1,
                NumberValue = r.Option1,
              });
            }
            if (r.Option2 != 0)
            {
              command.Parameters.Add(new CharacterCommandParameter
              {
                Type = 2,
                NumberValue = r.Option2,
              });
            }
            await RunCommandAsync(command);
          }
        }

        // 放置削除ターンが進んでいることを通知する
        if (character.DeleteTurn > 0 && !isBlockedCommand)
        {
          if (character.DeleteTurn == 1)
          {
            await PushNotificationService.SendCharacterAsync(repo, "コマンド切れ", "入力したすべてのコマンドの実行が完了しました。新しいコマンドを入力してください", character.Id);
          }
          else if (Config.DeleteTurns != character.DeleteTurn && (Config.DeleteTurns - character.DeleteTurn) % 144 == 0)
          {
            var days = (Config.DeleteTurns - character.DeleteTurn) % 144;
            await PushNotificationService.SendCharacterAsync(repo, "コマンド切れ", $"入力したすべてのコマンドの実行が完了したため、放置削除が進んでいます。このまま新しいコマンドを入力しないと、あなたの武将は約 {days} 日後に削除されます", character.Id);
          }
        }
        else
        {
          // 別働隊へ給与
          var ais = await repo.Character.GetManagementByHolderCharacterIdAsync(character.Id);
          foreach (var aii in ais)
          {
            if (character.Money > 2000)
            {
              character.Money -= 2000;
            }
            else
            {
              var aiChara = await repo.Character.GetByIdAsync(aii.CharacterId);
              if (aiChara.HasData)
              {
                aiChara.Data.AiType = CharacterAiType.RemovedFlyingColumn;
                aiChara.Data.DeleteTurn = (short)Config.DeleteTurns;
              }
            }
          }
        }

        // 管理人だけの特別処理
        if (character.AiType == CharacterAiType.Administrator)
        {
          character.DeleteTurn = 0;
        }

        // 政務官の削除
        if (character.AiType.IsSecretary() && (character.Money <= 0 || character.Rice <= 0))
        {
          var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
          character.DeleteTurn = (short)Config.DeleteTurns;
          await AddMapLogAsync(EventType.SecretaryRemovedWithNoSalary, $"<country>{countryOptional.Data?.Name ?? "無所属"}</country> の <character>{character.Name}</character> は、給与不足により解任されました", false);
        }
        if (character.AiType == CharacterAiType.RemovedSecretary)
        {
          var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
          character.DeleteTurn = (short)Config.DeleteTurns;
          await AddMapLogAsync(EventType.SecretaryRemoved, $"<country>{countryOptional.Data?.Name ?? "無所属"}</country> の <character>{character.Name}</character> は、解雇されました", false);
        }
        if (character.AiType == CharacterAiType.RemovedFlyingColumn)
        {
          var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
          character.DeleteTurn = (short)Config.DeleteTurns;
          await AddMapLogAsync(EventType.SecretaryRemoved, $"<country>{countryOptional.Data?.Name ?? "無所属"}</country> の <character>{character.Name}</character> は、削除されました", false);
        }

        // 放置削除の確認
        if (character.DeleteTurn >= Config.DeleteTurns)
        {
          await PushNotificationService.SendCharacterAsync(repo, "放置削除", "あなたの武将は放置削除されました。ゲームを続行するには、再度新規登録が必要です", character.Id);
          var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
          await CharacterService.RemoveAsync(repo, character);
          if (character.AiType == CharacterAiType.Human)
          {
            await AddMapLogAsync(EventType.CharacterRemoved, "<country>" + (countryOptional.Data?.Name ?? "無所属") + "</country> の <character>" + character.Name + "</character> は放置削除されました", false);
          }
          else if (!character.AiType.IsSecretary() && character.AiType != CharacterAiType.RemovedSecretary && character.AiType != CharacterAiType.RemovedFlyingColumn)
          {
            await AddMapLogAsync(EventType.AiCharacterRemoved, "<character>" + character.Name + "</character> は削除されました", false);
          }
          StatusStreaming.Default.Disconnect(character);
        }

        // 兵士の兵糧
        var ricePerSoldier = 1;
        var rice = character.SoldierNumber * ricePerSoldier;
        if (character.Rice >= rice)
        {
          character.Rice -= rice;
        }
        else
        {
          var dec = character.SoldierNumber * ricePerSoldier - character.Rice;
          if (dec > character.SoldierNumber)
          {
            dec = character.SoldierNumber;
          }
          character.SoldierNumber -= dec;
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
      }
      catch (Exception ex)
      {
        await AddLogAsync($"ID {character.Id} DATE {gameObj.GameDateTime.ToInt()} の更新実行中にエラーが発生したため、更新処理はスキップされました。<emerge>IDとDATEを添えて、管理者に連絡してください</emerge>");
        _logger.LogError(ex, $"ID {character.Id} DATE {gameObj.GameDateTime.ToInt()} 武将更新処理中にエラーが発生しました");
        await Task.Delay(3000);
      }

      var updateLog = new CharacterUpdateLog
      {
        IsFirstAtMonth = false,
        CharacterId = character.Id,
        CharacterName = character.Name,
        DateTime = DateTime.Now,
        GameDateTime = gameObj.GameDateTime,
      };

      try
      {
        // 古いコマンドの削除
        character.LastUpdated = character.LastUpdated.AddSeconds(Config.UpdateTime);
        character.LastUpdatedGameDate = currentMonth;
        repo.CharacterCommand.RemoveOlds(character.Id, character.LastUpdatedGameDate);

        // 更新の記録
        await repo.Character.AddCharacterUpdateLogAsync(updateLog);
        await repo.SaveChangesAsync();

        // キャッシュを更新（新しい更新時刻を保存）
        EntityCaches.UpdateCharacter(character);
      }
      catch (Exception ex)
      {
        await AddLogAsync($"ID {character.Id} DATE {gameObj.GameDateTime.ToInt()} の更新記録中にエラーが発生したため、更新処理の一部はスキップされました。再度同じ更新処理が実行される可能性があります。<emerge>IDとDATEを添えて、管理者に連絡してください</emerge>");
        _logger.LogError(ex, $"ID {character.Id} DATE {gameObj.GameDateTime.ToInt()} 武将更新処理中にエラーが発生しました");
        await Task.Delay(3000);
      }

      try
      {
        // 更新の通知
        notifies.Add(ApiData.From(new ApiSignal
        {
          Type = SignalType.CurrentCharacterUpdated,
          Data = new
          {
            gameDate = character.LastUpdatedGameDate,
            secondsNextCommand = (int)((character.LastUpdated.AddSeconds(Config.UpdateTime) - DateTime.Now).TotalSeconds),
            lastUpdated = ApiDateTime.FromDateTime(character.LastUpdated),
          },
        }));

        // ログイン中のユーザに新しい情報を通知する
        await StatusStreaming.Default.SendCharacterAsync(notifies, character.Id);
        if (anonymousNotifies.Any())
        {
          await StatusStreaming.Default.SendAllAsync(anonymousNotifies);
          await AnonymousStreaming.Default.SendAllAsync(anonymousNotifies);
        }
        await AnonymousStreaming.Default.SendAllAsync(ApiData.From(updateLog));
        foreach (var charaData in anyoneNotifies.GroupBy(an => an.Item1, (id, items) => new { CharacterId = id, Items = items.Select(i => i.Item2), }))
        {
          var charaLogs = charaData.Items.OfType<ApiData<CharacterLog>>();
          if (charaLogs.Any())
          {
            // ログを一括追加（戦闘ログの順番がランダムになることがあるので、一括にやってしまう）
            await repo.Character.AddCharacterLogAsync(charaLogs.Select(l => l.Data));
          }
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
          var charas = (await repo.Town.GetCharactersAsync(town.Id))
            .Where(c => c.CountryId != town.CountryId);
          await StatusStreaming.Default.SendCharacterAsync(ApiData.From(town), charas.Select(c => c.Id));
        });

        // 武将情報を全員に送信
        await CharacterService.StreamCharacterAsync(repo, character);

        // 武将の新しいコマンドを全員に送信
        var newCommand = await repo.CharacterCommand.GetAsync(character.Id, currentMonth.AddMonth(4));
        if (newCommand.HasData)
        {
          await StatusStreaming.Default.SendCountryAsync(ApiData.From(newCommand.Data), character.CountryId);
        }

        // 移動した場合、移動情報を全員に配信する
        if (oldTownId != character.TownId)
        {
          var townCharacters = await repo.Town.GetCharactersWithIconAsync(character.TownId);
          await StatusStreaming.Default.SendCharacterAsync(townCharacters.Where(tc => tc.Character.Id != character.Id && (tc.Character.CountryId == character.CountryId || tc.Character.AiType != CharacterAiType.SecretaryScouter)).Select(tc => ApiData.From(new CharacterForAnonymous(tc.Character, tc.Icon, tc.Character.CountryId == character.CountryId ? CharacterShareLevel.SameTownAndSameCountry : CharacterShareLevel.SameTown))), character.Id);

          var oldTown = await repo.Town.GetByIdAsync(oldTownId);
          if (oldTown.HasData && oldTown.Data.CountryId != character.CountryId)
          {
            var oldTownCharacters = await repo.Town.GetCharactersWithIconAsync(oldTownId);
            await StatusStreaming.Default.SendCharacterAsync(oldTownCharacters.Where(tc => tc.Character.CountryId != character.CountryId).Select(tc => ApiData.From(new CharacterForAnonymous(tc.Character, tc.Icon, tc.Character.CountryId == character.CountryId ? CharacterShareLevel.SameCountry : CharacterShareLevel.Anonymous))), character.Id);
            await StatusStreaming.Default.SendCharacterAsync(ApiData.From(new TownForAnonymous(oldTown.Data)), character.Id);

            var subBuildings = await repo.Town.GetSubBuildingsAsync(oldTownId);
            await StatusStreaming.Default.SendCharacterAsync(subBuildings.Select(s => ApiData.From(new TownSubBuilding
            {
              Id = s.Id,
              StatusFinishGameDateTime = s.StatusFinishGameDateTime,
              Status = TownSubBuildingStatus.Unknown,
              TownId = s.TownId,
              Type = s.Type,
            })), character.Id);
          }

          var newTown = await repo.Town.GetByIdAsync(character.TownId);
          if (newTown.HasData && newTown.Data.CountryId != character.CountryId)
          {
            var defenders = await repo.Town.GetAllDefendersAsync();
            await StatusStreaming.Default.SendCharacterAsync(defenders.Where(d => d.TownId == newTown.Data.Id).Select(d => ApiData.From(d)), character.Id);

            var subBuildings = await repo.Town.GetSubBuildingsAsync(character.TownId);
            await StatusStreaming.Default.SendCharacterAsync(subBuildings.Select(s => ApiData.From(s)), character.Id);
          }
        }
      }
      catch (Exception ex)
      {
        await AddLogAsync($"ID {character.Id} DATE {gameObj.GameDateTime.ToInt()} の更新データ送出中にエラーが発生しました。<emerge>IDとDATEを添えて、管理者に連絡してください</emerge>");
        _logger.LogError(ex, $"ID {character.Id} DATE {gameObj.GameDateTime.ToInt()} 武将更新処理中にエラーが発生しました");
        await Task.Delay(3000);
      }

      try
      {
        await repo.SaveChangesAsync();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, $"ID {character.Id} DATE {gameObj.GameDateTime.ToInt()} 武将更新処理中にエラーが発生しました");
        await Task.Delay(3000);
      }
    }

    private static async Task<bool> CheckResetAsync(MainRepository repo, SystemData system)
    {
      if (system.IsWaitingReset && system.IntResetGameDateTime == system.IntGameDateTime)
      {
        await ResetService.ResetAsync(repo);

        var sys = await repo.System.GetAsync();
        nextMonthStartDateTime = sys.CurrentMonthStartDateTime.AddSeconds(Config.UpdateTime);

        await PushNotificationService.SendAllAsync(repo, "リセット", "第 " + (system.BetaVersion == 0 ? system.Period.ToString() : system.Period + "." + system.BetaVersion) + " 期が始まりました。新規登録して、ゲームを始めましょう！");
        await repo.PushNotificationKey.ResetAsync();

        return true;
      }
      return false;
    }

    private class CountrySalary
    {
      public uint CountryId { get; set; }
      public int AllSalary { get; set; }
      public int PaidSalary { get; set; }
      public int AllContributions { get; set; }
    }
  }
}
