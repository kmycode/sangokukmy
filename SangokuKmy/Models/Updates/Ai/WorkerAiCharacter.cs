﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;
using SoldierType = SangokuKmy.Models.Data.Entities.SoldierType;
using SangokuKmy.Models.Services;
using SangokuKmy.Common;
using SangokuKmy.Streamings;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Updates.Ai
{
  public abstract class WorkerAiCharacter : AiCharacter
  {
    private SystemData system;
    private AiCountryData data;
    private CharacterCommand command;
    private IEnumerable<CountryWar> wars;
    private IEnumerable<TownWar> townWars;
    private IEnumerable<Town> towns;
    private IEnumerable<Country> countries;
    private IEnumerable<Country> availableCountries = null;

    protected Town BorderTown => this.data.BorderTown;

    protected Town NextBattleTown => this.data.NextBattleTown;

    protected virtual bool CanSoldierForce => false;

    protected virtual AttackMode AttackType => AttackMode.Normal;

    protected virtual bool IsWarAll => false;

    protected virtual bool IsWarEmpty => false;

    protected virtual bool IsWarAllAvoidLowCharacters => false;

    protected virtual int RequestedTechnologyForSoldier => 0;

    protected virtual ForceDefendPolicyLevel ForceDefendPolicy => ForceDefendPolicyLevel.NotCare;

    protected virtual DefendLevel NeedDefendLevel => DefendLevel.NeedMyDefend;

    protected virtual DefendSeiranLevel NeedDefendSeiranLevel => DefendSeiranLevel.Seirans;

    protected virtual SoldierType FindSoldierType()
    {
      return SoldierType.Common;
    }

    protected virtual async Task<SoldierType> FindSoldierTypeAsync(MainRepository repo, Town willBattleTown)
    {
      return this.FindSoldierType();
    }

    protected virtual int GetSoldierNumberMax(SoldierType type)
    {
      return this.Character.Leadership;
    }

    public WorkerAiCharacter(Character character) : base(character)
    {
    }

    private async Task<bool> InitializeAiDataAsync(MainRepository repo)
    {
      if (!this.towns.Any(t => t.CountryId == this.Character.CountryId))
      {
        return false;
      }

      this.data = new AiCountryData
      {
      };

      var managementOptional = await repo.AiCountry.GetManagementByCountryIdAsync(this.Country.Id);

      var availableWars = this.GetWaringCountries();
      var readyWars = this.GetReadyForWarCountries();
      var nearReadyWars = this.GetNearReadyForWarCountries();
      var mainWars = availableWars.Any() ? availableWars :
                     nearReadyWars.Any() ? nearReadyWars : readyWars;

      if (managementOptional.HasData)
      {
        if (!mainWars.Any())
        {
          var country = await repo.Country.GetByIdAsync(managementOptional.Data.VirtualEnemyCountryId);
          if (country.HasData)
          {
            mainWars = mainWars.Append(country.Data.Id);
          }
        }

        this.data.WillTownWarTown = this.towns.FirstOrDefault(t => t.Id == managementOptional.Data.TownWarTargetTownId);
        if (this.data.WillTownWarTown != null)
        {
          if (this.data.WillTownWarTown.CountryId == this.Country.Id)
          {
            this.data.WillTownWarTown = null;
          }
        }
      }

      var targetOrder = BattleTargetOrder.Defenders;

      // 経営国家
      var old = await repo.AiCountry.GetStorategyByCountryIdAsync(this.Character.CountryId);
      if (old.HasData)
      {
        // 作戦見直し
        if (old.Data.IntNextResetGameDate <= this.GameDateTime.ToInt())
        {
          if (!mainWars.Any())
          {
            old.Data.MainTownId = 0;
            old.Data.TargetTownId = 0;
            old.Data.BorderTownId = 0;
            old.Data.NextTargetTownId = 0;
          }
          else
          {
            if (RandomService.Next(0, 3) == 0)
            {
              old.Data.TargetTownId = 0;
              old.Data.NextTargetTownId = 0;
            }
          }
          old.Data.IntNextResetGameDate = this.GameDateTime.ToInt() + RandomService.Next(20, 40);
        }

        targetOrder = old.Data.TargetOrder;

        var main = this.towns.FirstOrDefault(t => t.Id == old.Data.MainTownId);
        var target = this.towns.FirstOrDefault(t => t.Id == old.Data.TargetTownId);
        var border = this.towns.FirstOrDefault(t => t.Id == old.Data.BorderTownId);
        var nextTarget = this.towns.FirstOrDefault(t => t.Id == old.Data.NextTargetTownId);
        this.data.DevelopTown = this.towns.FirstOrDefault(t => t.Id == old.Data.DevelopTownId);
        this.data.IsDefendForce = old.Data.IsDefendForce && this.ForceDefendPolicy != ForceDefendPolicyLevel.NotCare;

        if (main != null && main.CountryId == this.Country.Id)
        {
          this.data.MainTown = main;
        }

        if (target != null && mainWars.Contains(target.CountryId))
        {
          if (readyWars.Contains(target.CountryId) && availableWars.Any())
          {
            // 目標は、戦争準備中の都市だったが、現在戦争中の国が別にある
          }
          else
          {
            this.data.TargetTown = target;
          }
        }

        if (border != null && border.CountryId == this.Country.Id)
        {
          this.data.BorderTown = border;
        }

        if (nextTarget != null && mainWars.Contains(nextTarget.CountryId) && border != null && border.IsNextToTown(nextTarget))
        {
          this.data.NextBattleTown = nextTarget;
        }
      }

      // 別働隊
      var charaManagementOptional = await repo.Character.GetManagementByAiCharacterIdAsync(this.Character.Id);
      if (charaManagementOptional.HasData)
      {
        var holder = await repo.Character.GetByIdAsync(charaManagementOptional.Data.HolderCharacterId);
        if (holder.HasData)
        {
          this.data.MainTown = this.towns.FirstOrDefault(t => t.Id == this.Country.CapitalTownId && t.CountryId == this.Country.Id);
          this.data.BorderTown = this.towns.FirstOrDefault(t => t.Id == holder.Data.TownId);

          if (charaManagementOptional.Data.Action != AiCharacterAction.Assault)
          {
            this.data.TargetTown = this.data.NextBattleTown = this.towns.FirstOrDefault(t => t.Id == charaManagementOptional.Data.TargetTownId && t.CountryId != this.Country.Id);
            if (charaManagementOptional.Data.Action == AiCharacterAction.DomesticAffairs ||
              charaManagementOptional.Data.Action == AiCharacterAction.Defend)
            {
              this.data.BorderTown = this.data.DevelopTown = this.towns.FirstOrDefault(t => t.Id == charaManagementOptional.Data.TargetTownId);
            }
          }

          // 武将の指定が上書きされないよう、ここで処理を強制的にぶった切る
          return true;
        }
        return false;
      }

      if (this.data.BorderTown != null && this.data.MainTown != null && this.data.BorderTown.Id != this.data.MainTown.Id)
      {
        if (this.towns.GetAroundTowns(this.data.MainTown).Any(t => mainWars.Contains(t.CountryId)))
        {
          // 重要都市が敵国に隣接したら、ひとまず重要都市に退却
          this.data.BorderTown = this.data.MainTown;
        }
      }

      if (availableWars.Any() && this.data.BorderTown != null && this.data.BorderTown.People < 10000)
      {
        this.data.BorderTown = this.GetTownForSoldiers(predicate: t => t.People > 20000 && this.towns.GetAroundTowns(t).Any(tt => availableWars.Contains(tt.CountryId)));
      }

      if (this.data.BorderTown != null &&
        !this.towns.GetAroundTowns(this.data.BorderTown).Any(t => mainWars.Contains(t.CountryId)))
      {
        // 前線都市が敵国と隣接していない
        this.data.BorderTown = null;
        this.data.NextBattleTown = null;
      }

      if (this.data.TargetTown != null)
      {
        if (mainWars.Any())
        {
          var targetStreet = this.GetStreet(this.towns, this.Town, this.data.TargetTown, t => mainWars.Contains(t.CountryId));
          if (targetStreet == null)
          {
            // 現在戦争中の相手国だけで到達できる都市ではない
            this.data.TargetTown = null;
          }
        }
        else
        {
          // 戦争中ではない
          this.data.BorderTown = null;
          this.data.NextBattleTown = null;
          this.data.TargetTown = null;
        }
      }

      // 重要都市を決める
      if (this.data.MainTown == null)
      {
        if (availableWars.Any() || readyWars.Any())
        {
          var match = this.towns
            .Where(t => t.CountryId == this.Country.Id)
            .Where(t => this.towns.GetAroundTowns(t).Any(tt => mainWars.Contains(tt.CountryId)))
            .OrderByDescending(t => t.Wall + t.WallMax + t.Technology + t.TechnologyMax + t.People + t.PeopleMax)
            .OrderByDescending(t => this.towns.GetAroundTowns(t).Count(tt => mainWars.Contains(tt.CountryId)))
            .FirstOrDefault();
          if (match != null)
          {
            this.data.MainTown = match;
          }
        }

        if (this.data.MainTown == null)
        {
          var capital = this.towns.FirstOrDefault(t => t.Id == this.Country.CapitalTownId);
          if (capital == null || capital.CountryId != this.Character.CountryId || (availableWars.Any() || nearReadyWars.Any()))
          {
            if (capital != null && (capital.People < 4000 || capital.CountryId != this.Character.CountryId))
            {
              var main = this.towns
                .Where(t => t.CountryId == this.Character.CountryId)
                .OrderByDescending(t => t.People * (t.Security + t.Technology))
                .First();
              this.data.MainTown = main;
            }
            else
            {
              this.data.MainTown = capital;
            }
          }
          else
          {
            this.data.MainTown = capital;
          }
        }
      }

      // 目標都市を決める（ないこともある）
      if (this.data.TargetTown == null)
      {
        if (mainWars.Any())
        {
          var match = this.towns
            .Where(t => t.CountryId != this.Country.Id)
            .Where(t => mainWars.Contains(t.CountryId) && this.GetStreet(this.towns, this.data.MainTown, (Town)t, tt => tt.CountryId == this.Country.Id || mainWars.Contains(tt.CountryId)) != null)
            .OrderByDescending(t => t.People * (t.Security + t.Technology))
            .FirstOrDefault();
          if (match != null)
          {
            this.data.TargetTown = match;
          }
        }
      }

      // 前線都市を決める（ないこともある）
      if (this.data.BorderTown == null || this.data.NextBattleTown == null)
      {
        if (mainWars.Any())
        {
          async Task<Town> PickNextTownAsync(Town borderTown)
          {
            var arounds = this.towns.GetAroundTowns(borderTown).Where(t => mainWars.Contains(t.CountryId));
            if (arounds.Any())
            {
              // 前線都市が、目標都市への最短でないにしても、敵国と隣接している
              var townData = new List<(TownBase Town, IEnumerable<Character> Defenders, IEnumerable<Character> Stays)>();
              foreach (var town in arounds)
              {
                townData.Add((
                  town,
                  (await repo.Town.GetDefendersAsync(town.Id)).Select(d => d.Character),
                  (await repo.Town.GetCharactersAsync(town.Id))
                ));
              }

              TownBase target;
              switch (targetOrder)
              {
                //case BattleTargetOrder.Defenders:
                default:
                  target = townData.OrderByDescending(d => d.Defenders.Count() * 2 + d.Stays.Count()).First().Town;
                  break;
                case BattleTargetOrder.GetTown:
                  target = townData.GroupBy(d => d.Defenders.Count()).First().OrderBy(d => d.Town.Wall).First().Town;
                  break;
                case BattleTargetOrder.BreakWall:
                  target = townData.GroupBy(d => d.Defenders.Count()).First().OrderByDescending(d => d.Town.Wall).First().Town;
                  break;
              }

              return (Town)target;
            }

            return null;
          }

          if (this.data.BorderTown != null && this.data.NextBattleTown == null)
          {
            var t = await PickNextTownAsync(this.data.BorderTown);
            if (t != null)
            {
              this.data.NextBattleTown = t;
            }
            else
            {
              // 敵と隣接していなければ、前線を進める（下で処理する）
              this.data.BorderTown = null;
            }
          }

          if (this.data.BorderTown == null && this.data.TargetTown != null)
          {
            var street = this.GetStreet(
              this.towns,
              this.data.MainTown,
              this.data.TargetTown,
              t => t.CountryId == this.Country.Id || mainWars.Contains(t.CountryId));
            if (street != null && street.Count() > 1)
            {
              var s = street.ToArray();
              for (var i = 1; i < s.Length; i++)
              {
                if (s[i].CountryId != this.Country.Id)
                {
                  this.data.BorderTown = s[i - 1];
                  this.data.NextBattleTown = s[i];

                  var t = await PickNextTownAsync(this.data.BorderTown);
                  if (t != null)
                  {
                    this.data.NextBattleTown = t;
                  }

                  break;
                }
              }
            }
          }
        }
      }

      // 強制的に全員守備ループするかどうか
      if (this.ForceDefendPolicy != ForceDefendPolicyLevel.NotCare)
      {
        var history = (await repo.AiActionHistory.GetAsync(mainWars, AiBattleTownType.BorderTown, this.Country.Id))
          .Concat(await repo.AiActionHistory.GetAsync(mainWars, AiBattleTownType.MainTown, this.Country.Id));
        var myHistory = (await repo.AiActionHistory.GetAsync(this.Country.Id, AiBattleTownType.BorderTown, mainWars))
          .Concat(await repo.AiActionHistory.GetAsync(this.Country.Id, AiBattleTownType.MainTown, mainWars));

        var n = 48;
        if (this.ForceDefendPolicy == ForceDefendPolicyLevel.Negative)
        {
          n = 192;
        }
        else
        {
          n = 96;
        }

        var targets = history.Where(h => h.IntGameDateTime >= this.GameDateTime.ToInt() - n);
        var myAttacks = myHistory.Where(h => h.IntGameDateTime >= this.GameDateTime.ToInt() - n);
        var targetsUsedMoneyAverage = (history.Count() < 30 && targets.Any()) ? targets.Average(t => t.AttackerSoldiersMoney) : int.MaxValue;
        var money = 0;
        var minMoney = 0;

        if (!history.Any())
        {
          this.data.IsDefendForce = false;
        }
        else if (this.ForceDefendPolicy == ForceDefendPolicyLevel.Aggressive)
        {
          this.data.IsDefendForce = (!targets.Any() &&
            myAttacks.Any() && myAttacks.Min(h => h.RestDefenderCount) >= 1)
            || targetsUsedMoneyAverage < 1_2000;
          money = 26_0000;
          minMoney = 10_0000;
        }
        else if (this.ForceDefendPolicy == ForceDefendPolicyLevel.Medium)
        {
          this.data.IsDefendForce = (!targets.Any(t => t.RestDefenderCount == 0) &&
            !myAttacks.Any(h => h.TargetType == AiBattleTargetType.Wall))
            || targetsUsedMoneyAverage < 8000;
          money = 18_0000;
          minMoney = 8_0000;
        }
        else if (this.ForceDefendPolicy == ForceDefendPolicyLevel.Negative)
        {
          this.data.IsDefendForce = (!targets.Any() &&
            myAttacks.Count(h => h.TargetType == AiBattleTargetType.CharacterLowSoldiers) > myAttacks.Count(h => h.TargetType == AiBattleTargetType.Character))
            || targetsUsedMoneyAverage < 4000;
          money = 10_0000;
          minMoney = 6_0000;
        }

        var charas = await repo.Country.GetCharactersAsync(this.Country.Id);
        if (this.data.IsDefendForce)
        {
          this.data.IsDefendForce = charas
            .Where(c => c.AiType != CharacterAiType.Human && c.AiType.CanBattle())
            .Any(c => c.Money < money);
        }
        else
        {
          this.data.IsDefendForce = charas
            .Where(c => c.AiType != CharacterAiType.Human && c.AiType.CanBattle())
            .Any(c => c.Money < minMoney);
        }
      }

      // DBに保存
      var oldData = old.Data ?? new AiCountryStorategy
      {
        CountryId = this.Country.Id,
      };
      oldData.MainTownId = this.data.MainTown?.Id ?? 0;
      oldData.DevelopTownId = this.data.DevelopTown?.Id ?? 0;
      oldData.BorderTownId = this.data.BorderTown?.Id ?? 0;
      oldData.NextTargetTownId = this.data.NextBattleTown?.Id ?? 0;
      oldData.TargetTownId = this.data.TargetTown?.Id ?? 0;
      oldData.IsDefendForce = this.data.IsDefendForce && this.ForceDefendPolicy != ForceDefendPolicyLevel.NotCare;
      if (!old.HasData)
      {
        await repo.AiCountry.AddAsync(oldData);
      }
      await repo.SaveChangesAsync();

      return true;
    }

    protected override async Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      this.wars = wars;
      this.system = await repo.System.GetAsync();
      this.townWars = await repo.CountryDiplomacies.GetAllTownWarsAsync();
      this.towns = await repo.Town.GetAllAsync();
      this.countries = (await repo.Country.GetAllAsync()).Where(c => !c.HasOverthrown);

      if (this.IsWarAllAvoidLowCharacters)
      {
        var targets = new List<Country>();
        foreach (var c in this.countries.Where(cc => cc.AiType == Data.Entities.CountryAiType.Human))
        {
          var count = await repo.Country.CountCharactersAsync(c.Id);
          if (count >= Config.CountryJoinMaxOnLimited - 1)
          {
            targets.Add(c);
          }
        }
        this.availableCountries = targets;
      }

      this.command = new CharacterCommand
      {
        Id = 0,
        CharacterId = this.Character.Id,
        GameDateTime = this.GameDateTime,
        Type = CharacterCommandType.None,
      };

      await this.BeforeActionAsync(repo);

      if (!await this.InitializeAiDataAsync(repo))
      {
        return this.command;
      }

      await this.ActionAsync(repo);
      await repo.AiActionHistory.AddAsync(new AiActionHistory
      {
        IntGameDateTime = this.GameDateTime.ToInt(),
        CharacterId = this.Character.Id,
        IntRicePrice = this.Town.IntRicePrice,
      });

      return this.command;
    }

    protected override async Task<Optional<CharacterCommand>> GetCommandAsNoCountryAsync(MainRepository repo)
    {
      this.towns = await repo.Town.GetAllAsync();
      this.command = new CharacterCommand
      {
        Id = 0,
        CharacterId = this.Character.Id,
        GameDateTime = this.GameDateTime,
        Type = CharacterCommandType.None,
      };

      if (!await this.ActionAsNoCountryAsync(repo))
      {
        return default;
      }

      return this.command.ToOptional();
    }

    protected virtual async Task BeforeActionAsync(MainRepository repo) { }

    protected abstract Task ActionAsync(MainRepository repo);

    protected virtual async Task<bool> ActionAsNoCountryAsync(MainRepository repo)
    {
      return false;
    }

    private Optional<TownWar> GetAvailableTownWar()
    {
      return this.townWars
        .FirstOrDefault(t => t.RequestedCountryId == this.Country.Id && t.Status == TownWarStatus.Available)
        .ToOptional();
    }

    private IEnumerable<uint> GetWaringCountries()
    {
      if (this.system.IsBattleRoyaleMode)
      {
        if (this.countries.Any(c => c.AiType == CountryAiType.Human))
        {
          return this.countries.Where(c => c.AiType == CountryAiType.Human).Select(c => c.Id).Append(0u);
        }
        else
        {
          return this.countries.Select(c => c.Id).Append(0u);
        }
      }

      if (!this.IsWarAll)
      {
        var availableWars = this.wars
          .Where(w => w.IntStartGameDate <= this.GameDateTime.ToInt() && (w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.StopRequesting));
        if (availableWars.Any())
        {
          var targetCountryIds = availableWars.Select(w => w.InsistedCountryId == this.Country.Id ? w.RequestedCountryId : w.InsistedCountryId);
          return targetCountryIds;
        }
      }
      else
      {
        var r = (this.availableCountries ?? this.countries)
          .Where(c => c.IntEstablished + Config.CountryBattleStopDuring <= this.GameDateTime.ToInt())
          .Select(c => c.Id);
        if (r.Any())
        {
          return r;
        }
      }

      if (this.IsWarEmpty &&
        this.towns.Any(t => t.CountryId == 0 && towns.GetAroundTowns(t).Any(tt => tt.CountryId == this.Country.Id)) &&
        this.GameDateTime.Year >= Config.UpdateStartYear + Config.CountryBattleStopDuring / 12)
      {
        return new List<uint> { 0, };
      }

      return Enumerable.Empty<uint>();
    }

    private IEnumerable<Town> GetAroundTargetTowns()
    {
      var activeWars = this.GetWaringCountries();
      var arounds = this.towns.GetAroundTowns(this.Town);
      var targets = arounds.Where(t => activeWars.Any(w => w == t.CountryId));
      if (!targets.Any())
      {
        targets = arounds.Where(t => t.CountryId == 0);
      }

      return targets.Cast<Town>().ToArray();
    }

    private IEnumerable<uint> GetNearReadyForWarCountries()
    {
      if (!this.IsWarAll)
      {
        var availableWars = this.wars
          .Where(w => w.IntStartGameDate <= this.GameDateTime.ToInt() + 12 && w.IntStartGameDate > this.GameDateTime.ToInt() && (w.Status == CountryWarStatus.InReady || w.Status == CountryWarStatus.StopRequesting));
        if (availableWars.Any())
        {
          var targetCountryIds = availableWars.Select(w => w.InsistedCountryId == this.Country.Id ? w.RequestedCountryId : w.InsistedCountryId);
          return targetCountryIds;
        }
      }
      else
      {
        var r = (this.availableCountries ?? this.countries)
          .Where(c => c.IntEstablished + Config.CountryBattleStopDuring > this.GameDateTime.ToInt())
          .Select(c => c.Id);
        if (r.Any())
        {
          return r;
        }
      }

      if (this.IsWarEmpty &&
        this.towns.Any(t => t.CountryId == 0 && towns.GetAroundTowns(t).Any(tt => tt.CountryId == this.Country.Id)) &&
        this.GameDateTime.Year >= Config.UpdateStartYear + Config.CountryBattleStopDuring / 12 - 1)
      {
        return new List<uint> { 0, };
      }

      return Enumerable.Empty<uint>();
    }

    private IEnumerable<uint> GetReadyForWarCountries()
    {
      if (!this.IsWarAll)
      {
        var availableWars = wars
          .Where(w => w.IntStartGameDate > this.GameDateTime.ToInt() && (w.Status == CountryWarStatus.InReady || w.Status == CountryWarStatus.StopRequesting));
        if (availableWars.Any())
        {
          var targetCountryIds = availableWars.Select(w => w.InsistedCountryId == this.Country.Id ? w.RequestedCountryId : w.InsistedCountryId);
          return targetCountryIds;
        }
      }

      if (this.IsWarEmpty &&
        this.towns.Any(t => t.CountryId == 0 && towns.GetAroundTowns(t).Any(tt => tt.CountryId == this.Country.Id)) &&
        this.GameDateTime.Year < Config.UpdateStartYear + Config.CountryBattleStopDuring / 12)
      {
        return new List<uint> { 0, };
      }

      return Enumerable.Empty<uint>();
    }

    private int GetFirstWarStartMonth()
    {
      if (this.GetWaringCountries().Any())
      {
        return 0;
      }

      var availableWars = wars
        .Where(w => w.IntStartGameDate > this.GameDateTime.ToInt() && (w.Status == CountryWarStatus.InReady || w.Status == CountryWarStatus.StopRequesting));
      if (availableWars.Any())
      {
        return availableWars.Min(w => w.IntStartGameDate);
      }

      return int.MaxValue;
    }

    private async Task<bool> InputMoveToTownAsync(MainRepository repo, uint townId)
    {
      var town = await repo.Town.GetByIdAsync(townId);
      if (!town.HasData)
      {
        return false;
      }

      return await this.MoveToTownOrNextToTownAsync(repo, this.Town, town.Data, this.command);
    }

    private async Task<bool> IsNeedSoldiersAsync(MainRepository repo)
    {
      var will = await this.GetBattleTargetTownAsync(repo);
      return this.Character.SoldierNumber < Math.Min(30, this.GetSoldierNumberMax(await this.FindSoldierTypeAsync(repo, will.Town)));
    }

    private Town GetTownForSoldiers(int num = -1, Func<TownBase, bool> predicate = null)
    {
      if (num <= 0)
      {
        num = this.Character.Leadership;
      }

      if (predicate == null)
      {
        predicate = t => t.People + 1 >= num * Config.SoldierPeopleCost &&
          t.Security + 1 > num / 10;
      }
      var arounds = this.towns.GetAroundTowns(this.Town).ToList();
      arounds.Add(this.Town);

      if (this.CanSoldierForce && this.Town.CountryId == this.Character.CountryId)
      {
        return this.Town;
      }

      if (this.CanSoldierForce && arounds.Any(t => t.CountryId == this.Character.CountryId))
      {
        return (Town)arounds.First(t => t.CountryId == this.Character.CountryId);
      }

      if (this.Town.CountryId == this.Character.CountryId && predicate(this.Town) && this.Town.Technology > 300)
      {
        return this.Town;
      }

      if (this.data.BorderTown != null && this.data.BorderTown.CountryId == this.Character.CountryId && predicate(this.data.BorderTown) && this.data.BorderTown.Technology > 300)
      {
        return this.data.BorderTown;
      }

      var targets = arounds.Where(a => a.CountryId == this.Character.CountryId).Where(predicate);
      if (targets.Any())
      {
        return (Town)targets.OrderByDescending(t => t.Technology + t.People).First();
      }
      
      if (this.data.MainTown.CountryId == this.Character.CountryId && predicate(this.data.MainTown) && this.data.MainTown.Technology > 500)
      {
        return this.data.MainTown;
      }

      var myTowns = this.towns
        .Where(t => t.CountryId == this.Character.CountryId)
        .Where(predicate)
        .OrderByDescending(t => t.Technology + t.People);
      if (myTowns.Any())
      {
        return (Town)myTowns.First();
      }
      else
      {
        return this.towns.Where(t => t.CountryId == this.Character.CountryId).OrderByDescending(t => t.People).FirstOrDefault() ?? this.Town;
      }
    }

    private async Task<bool> InputGetSoldiersAsync(MainRepository repo, int num = -1)
    {
      if (await this.IsNeedSoldiersAsync(repo))
      {
        if (!this.CanSoldierForce || this.Town.CountryId != this.Character.CountryId)
        {
          var town = this.GetTownForSoldiers();
          var isMove = await this.InputMoveToTownAsync(repo, town.Id);
          if (isMove)
          {
            return true;
          }
        }
        else
        {
          this.Town.People = (int)Math.Max(this.Town.People, this.Character.Leadership * Config.SoldierPeopleCost + 500);
          this.Town.Security = Math.Max(this.Town.Security, (short)(this.Character.Leadership / 10 + 1));
          await repo.SaveChangesAsync();
        }

        var will = await this.GetBattleTargetTownAsync(repo);
        var type = await this.FindSoldierTypeAsync(repo, will.Town);
        num = Math.Min(num <= 0 ? this.Character.Leadership : num, this.GetSoldierNumberMax(type));
        if (this.Character.SoldierNumber >= num)
        {
          return false;
        }

        if (this.Town.People + 1 < num * Config.SoldierPeopleCost || this.Town.Security + 1 < num / 10)
        {
          return await this.InputMoveToTownAsync(repo, this.GetTownForSoldiers(num).Id);
        }

        this.command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 1,
          NumberValue = (int)type,
        });
        this.command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 2,
          NumberValue = num,
        });
        this.command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 3,
          NumberValue = 0,
        });
        this.command.Type = CharacterCommandType.Soldier;
        return true;
      }
      return false;
    }

    private async Task<(Town Town, Func<Task<bool>> Input)> GetBattleTargetTownAsync(MainRepository repo)
    {
      Town targetTown = null;

      if (this.BorderTown != null)
      {
        if (this.data.BorderTown.Id != this.Town.Id)
        {
          var arounds = this.towns.GetAroundTowns(this.Town);
          if (this.data.NextBattleTown != null && arounds.Any(t => t.Id == this.data.NextBattleTown.Id))
          {
            targetTown = this.data.NextBattleTown;
          }
          else if (this.data.BorderTown != null && this.Character.TownId != this.data.BorderTown.Id)
          {
            return (this.data.NextBattleTown, async () => await this.InputMoveToBorderTownAsync(repo));
          }
        }
        else
        {
          targetTown = this.data.NextBattleTown;
        }
      }
      if (targetTown != null && this.AttackType != AttackMode.Normal)
      {
        // 城壁優先
        var defenders = await repo.Town.GetDefendersAsync(targetTown.Id);
        if ((this.AttackType == AttackMode.KillDefenders && !defenders.Any()) || defenders.Any())
        {
          var targets = this.GetAroundTargetTowns();

          if (targets.Count() == 1)
          {
            targetTown = targets.First();
          }
          else if (targets.Any())
          {
            // LINQのOrderByが安定ソートであることが前提
            var ds = await repo.Town.GetAllDefendersAsync();
            var targetsData = targets
              .GroupJoin(ds, t => t.Id, dss => dss.TownId, (t, dss) => new { Town = t, Defenders = dss, })
              .OrderBy(t => t.Town.Id == targetTown.Id ? 0 : 1);
            if (this.AttackType == AttackMode.KillDefenders)
            {
              targetTown = targetsData.OrderByDescending(t => t.Defenders.Count()).First().Town;
            }
            else if (this.AttackType == AttackMode.BreakWall)
            {
              targetTown = targetsData.OrderByDescending(t => t.Town.Wall).OrderBy(t => t.Defenders.Count()).First().Town;
            }
            else if (this.AttackType == AttackMode.GetTown)
            {
              targetTown = targetsData.OrderBy(t => t.Town.Wall).OrderBy(t => t.Defenders.Count()).First().Town;
            }
          }
          else if (this.BorderTown != null && this.BorderTown.Id != this.Character.TownId)
          {
            return (this.data.NextBattleTown, async () => await this.InputMoveToBorderTownAsync(repo));
          }
        }
      }
      {
        // 謀略建築物
        var targets = this.GetAroundTargetTowns();
        var subbuildings = await repo.Town.GetSubBuildingsAsync();
        var defenders = await repo.Town.GetAllDefendersAsync();
        var targetsData = targets
          .GroupJoin(subbuildings, t => t.Id, sb => sb.TownId, (t, sbs) => new { Town = t, Defenders = defenders.Where(d => d.TownId == t.Id), SubBuildings = sbs.Where(ssb => ssb.Type == TownSubBuildingType.Agitation), })
          .Where(t => t.SubBuildings.Any())
          .OrderBy(t => t.Defenders.Count())
          .OrderByDescending(t => t.SubBuildings.Count());
        if (targetsData.Any())
        {
          targetTown = targetsData.First().Town;
        }
      }

      return (targetTown, null);
    }

    protected async Task<bool> InputBattleAsync(MainRepository repo)
    {
      // 午前２〜６時
      if (this.GameDateTime.Year % 12 == 3 || this.GameDateTime.Year % 12 == 4)
      {
        return false;
      }

      var wc = this.GetWaringCountries();
      if (!this.GetWaringCountries().Any() && !this.GetNearReadyForWarCountries().Any() && this.data.WillTownWarTown == null)
      {
        return false;
      }

      if (this.data.IsDefendForce)
      {
        return false;
      }

      if (this.data.NextBattleTown == null)
      {
        return false;
      }

      if (await this.InputGetSoldiersAsync(repo))
      {
        return true;
      }

      if (this.Character.SoldierNumber <= 0)
      {
        return false;
      }

      if (this.Town.CountryId != this.Character.CountryId)
      {
        if (await this.InputMoveToQuickestImportantTownAsync(repo))
        {
          return true;
        }

        if (await this.InputMoveToBorderTownAsync(repo))
        {
          return true;
        }
      }

      if (this.data.WillTownWarTown != null && !this.Town.IsNextToTown(this.data.WillTownWarTown))
      {
        var moveTo = this.towns.GetAroundTowns(this.data.WillTownWarTown).FirstOrDefault(t => t.CountryId == this.Country.Id);
        if (moveTo != null)
        {
          return await this.MoveToTownOrNextToTownAsync(repo, this.Town, (Town)moveTo, this.command);
        }
      }

      Town targetTown = null;

      // 井闌判定
      if (this.Town.Wall > 0 && this.NeedDefendSeiranLevel != DefendSeiranLevel.NotCare && this.data.WillTownWarTown == null)
      {
        Town seiranTown = null;
        var enemyNormalAttackers = new List<Character>();
        var enemyWallAttackers = new List<Character>();
        var enemyTowns = this.GetAroundTargetTowns();
        var enemyCharas = new List<Character>();
        foreach (var town in enemyTowns)
        {
          var charas = await repo.Town.GetCharactersAsync(town.Id);
          foreach (var c in charas.Where(c => c.SoldierNumber >= 10 && c.CountryId == town.CountryId))
          {
            if (c.SoldierType.IsForWall())
            {
              enemyWallAttackers.Add(c);
            }
            else
            {
              enemyNormalAttackers.Add(c);
            }
          }
          enemyCharas.AddRange(charas);

          var defenders = await repo.Town.GetDefendersAsync(town.Id);
          if (defenders.Any() && (defenders.First().Character.SoldierType.IsForWall() || defenders.First().Character.SoldierNumber <= 9))
          {
            seiranTown = town;
          }
        }

        // 部隊
        if (this.NeedDefendSeiranLevel == DefendSeiranLevel.SeiransWithUnits)
        {
          var enemyTownCountryIds = enemyTowns.Select(t => t.CountryId).Distinct();
          foreach (var countryId in enemyTownCountryIds)
          {
            var units = await repo.Unit.GetByCountryIdAsync(countryId);
            var townUnits = units.Where(u =>
              u.Members.Any(um =>
                (um.Post == UnitMemberPostType.Leader || um.Post == UnitMemberPostType.Helper) &&
                enemyCharas.Any(c => c.Id == um.CharacterId && c.LastUpdated < this.Character.LastUpdated.AddSeconds(Config.UpdateTime * 1.5f))));
            foreach (var unit in townUnits)
            {
              foreach (var member in unit.Members
                .Join(enemyCharas, m => m.CharacterId, c => c.Id, (m, c) => c)
                .Where(c => !enemyTowns.Any(t => t.Id == c.TownId))
                .Where(c => c.SoldierNumber >= 10))
              {
                if (member.SoldierType.IsForWall())
                {
                  enemyWallAttackers.Add(member);
                }
                else
                {
                  enemyNormalAttackers.Add(member);
                }
              }
            }
          }
        }

        if (enemyWallAttackers.Any())
        {
          var myDefenders = await repo.Town.GetDefendersAsync(this.Town.Id);
          var myAvailableDefenders = myDefenders.Where(d => d.Character.Id != this.Character.Id && d.Character.SoldierNumber >= 20);
          var attackerSize = enemyNormalAttackers.Concat(enemyWallAttackers).Sum(c => c.SoldierNumber > 20 ? 4 : 2);
          var defenderSize = myDefenders
            .Where(d => d.Character.Id != this.Character.Id)
            .Sum(c => c.Character.SoldierNumber == 0 ? 1 : c.Character.SoldierNumber < 20 ? 2 : 4);

          if (seiranTown != null)
          {
            targetTown = seiranTown;
          }
          else if (this.NeedDefendSeiranLevel == DefendSeiranLevel.HalfSeirans ? attackerSize / 2 > defenderSize :
            attackerSize > defenderSize)
          {
            return await this.InputDefendAsync(repo, DefendLevel.Always);
          }
        }
      }

      if (this.data.WillTownWarTown != null && this.GetAvailableTownWar().HasData)
      {
        targetTown = this.data.WillTownWarTown;
      }

      if (targetTown == null)
      {
        var target = await this.GetBattleTargetTownAsync(repo);
        if (target.Input != null)
        {
          return await target.Input();
        }
        else if (target.Town == null)
        {
          return false;
        }
        else
        {
          targetTown = target.Town;
        }
      }

      if (targetTown != null && (this.GetWaringCountries().Any() || this.GetAvailableTownWar().HasData))
      {
        var posts = await repo.Country.GetPostsAsync(this.Country.Id);
        if (posts.Any(p => p.Type == CountryPostType.GrandGeneral && p.CharacterId != this.Character.Id))
        {
          var p = posts.First(pp => pp.Type == CountryPostType.GrandGeneral);
          repo.Country.RemoveCharacterPost(p);
          p.Type = CountryPostType.UnAppointed;
          await StatusStreaming.Default.SendAllAsync(ApiData.From(p));
        }
        var post = new CountryPost
        {
          Type = CountryPostType.GrandGeneral,
          CharacterId = this.Character.Id,
          CountryId = this.Character.CountryId,
        };
        await repo.Country.AddPostAsync(post);
        await repo.SaveChangesAsync();
        await StatusStreaming.Default.SendAllAsync(ApiData.From(post));

        this.command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 1,
          NumberValue = (int)targetTown.Id,
        });
        this.command.Type = CharacterCommandType.Battle;
        return true;
      }
      else
      {
        return false;
      }
    }

    protected async Task<bool> InputDefendAsync(MainRepository repo, DefendLevel level, bool isGetSoldiers = true)
    {
      if (level == DefendLevel.NotCare)
      {
        return false;
      }

      async Task<bool> RunAsync(Town town, DefendLevel lv)
      {
        if (lv == DefendLevel.Always)
        {
          this.command.Type = CharacterCommandType.Defend;
          return true;
        }

        var defends = await repo.Town.GetDefendersAsync(town.Id);
        if (!defends.Any(d => d.Character.Id == this.Character.Id))
        {
          if (lv == DefendLevel.NeedMyDefend)
          {
            this.command.Type = CharacterCommandType.Defend;
            return true;
          }

          var size = 0;
          if (lv == DefendLevel.NeedThreeDefend) size = 3;
          if (lv == DefendLevel.NeedTwoDefend) size = 2;
          if (lv == DefendLevel.NeedAnyDefends) size = 1;
          if (defends.Count() < size)
          {
            this.command.Type = CharacterCommandType.Defend;
            return true;
          }
        }

        return false;
      }

      if (!this.GetWaringCountries().Any() && !this.GetNearReadyForWarCountries().Any())
      {
        // 戦争状態にない場合
        if (this.data.BorderTown == null)
        {
          return false;
        }
        if ((await repo.Town.GetDefendersAsync(this.data.BorderTown.Id)).Any(d => d.Character.Id == this.Character.Id))
        {
          return false;
        }
        if (this.Character.TownId != this.data.BorderTown.Id)
        {
          return await this.InputMoveToBorderTownAsync(repo);
        }
        if (this.Character.SoldierNumber <= 0 && isGetSoldiers && await this.InputGetSoldiersAsync(repo))
        {
          return true;
        }
        level = DefendLevel.NeedMyDefend;
      }

      if (this.Town.CountryId != this.Character.CountryId)
      {
        return false;
      }

      if (isGetSoldiers && await this.InputGetSoldiersAsync(repo))
      {
        return true;
      }

      if (this.Character.SoldierNumber <= 0)
      {
        return false;
      }

      if (this.data.BorderTown == null)
      {
        return false;
      }

      if (this.Town.Id != this.data.BorderTown.Id && this.Town.Id != this.data.MainTown.Id)
      {
        return false;
      }

      return await RunAsync(this.Town, level);
    }

    protected async Task<bool> InputCountryForceDefendLoopAsync(MainRepository repo)
    {
      if (!this.data.IsDefendForce)
      {
        return false;
      }

      if (this.Town.Id != this.data.MainTown.Id && this.Town.Id != this.data.BorderTown?.Id)
      {
        return (await this.InputMoveToBorderTownAsync(repo)) || (await this.InputMoveToMainTownAsync(repo));
      }

      return await this.InputDefendLoopAsync(repo, 0, int.MaxValue);
    }

    protected async Task<bool> InputDefendLoopAsync(MainRepository repo, int minPeople, int minMoney = 0)
    {
      if (this.Town.Id != this.data.MainTown.Id && this.Town.Id != this.data.BorderTown?.Id)
      {
        return false;
      }

      if (!this.GetWaringCountries().Any())
      {
        return false;
      }

      if (this.Town.People > minPeople && this.Character.Money > minMoney)
      {
        return false;
      }

      if (await this.InputDefendAsync(repo, DefendLevel.NeedMyDefend, false))
      {
        return true;
      }

      var num1 = this.Town.People < 2000 ? 1 : this.Town.People < 5000 ? 5 : this.Town.People < 7000 ? 9 : this.Town.People / 700;
      var num2 = this.Character.Money < 30000 ? 1 : this.Character.Money < 80000 ? 9 : this.Character.Leadership;
      var num = Math.Min(num1, num2);
      if (await this.InputGetSoldiersAsync(repo, num))
      {
        return true;
      }

      await this.ActionAfterDefendLoopAsync(repo);
      return true;
    }

    protected virtual async Task ActionAfterDefendLoopAsync(MainRepository repo)
    {
      if (this.InputSoldierTrainingIfNoMoney(1000))
      {
        return;
      }

      if (this.Character.Intellect > this.Character.Strong)
      {
        if (this.Town.Technology < this.Town.TechnologyMax)
        {
          this.command.Type = CharacterCommandType.Technology;
        }

        if (this.Town.Wall < this.Town.WallMax)
        {
          this.command.Type = CharacterCommandType.Wall;
        }

        this.InputTraining(TrainingType.Intellect);
      }
      else
      {
        this.InputTraining(TrainingType.Strong);
      }
    }

    protected async Task<bool> InputDefendAsync(MainRepository repo) => await this.InputDefendAsync(repo, this.NeedDefendLevel);

    protected bool InputSecurity()
    {
      if (this.Town.Security < 100)
      {
        if (this.Character.Rice > 3000 && this.Town.Security < 95)
        {
          this.command.Type = CharacterCommandType.SuperSecurity;
        }
        else
        {
          this.command.Type = CharacterCommandType.Security;
        }
        return true;
      }
      return false;
    }

    protected bool InputSecurityInWar()
    {
      if (!this.GetReadyForWarCountries().Any())
      {
        return false;
      }

      if (this.InputSecurity())
      {
        return true;
      }

      if (this.Town.People < this.Town.PeopleMax - 15000)
      {
        this.command.Type = CharacterCommandType.PeopleIncrease;
        return true;
      }

      return false;
    }

    protected void InputSecurityForce()
    {
      this.command.Type = CharacterCommandType.Security;
    }

    protected async Task<bool> InputFirstSecurityAsync(MainRepository repo)
    {
      if (this.GameDateTime.Year > Config.UpdateStartYear)
      {
        return false;
      }

      if (this.Town.Security >= 60)
      {
        return false;
      }

      var charas = await repo.Country.GetCharactersAsync(this.Character.CountryId);
      if (charas.Any(c => c.AiType == CharacterAiType.ManagedPatroller))
      {
        return false;
      }

      this.InputSecurityForce();
      return true;
    }

    protected async Task<bool> InputMoveToQuickestImportantTownAsync(MainRepository repo)
    {
      if (this.Town.CountryId == this.Character.CountryId)
      {
        return false;
      }

      if (this.data.BorderTown != null && this.data.BorderTown.IsNextToTown(this.Town))
      {
        return await this.InputMoveToTownAsync(repo, this.data.BorderTown.Id);
      }

      if (this.data.MainTown != null && this.data.MainTown.IsNextToTown(this.Town))
      {
        return await this.InputMoveToTownAsync(repo, this.data.MainTown.Id);
      }

      return false;
    }

    protected async Task<bool> InputMoveToDevelopTownAsync(MainRepository repo)
    {
      if (this.GetWaringCountries().Any() || this.GetNearReadyForWarCountries().Any())
      {
        return false;
      }
      return await this.InputMoveToDevelopTownForceAsync(repo);
    }

    protected async Task<bool> InputMoveToDevelopTownForceAsync(MainRepository repo)
    {
      if (this.data.DevelopTown == null)
      {
        return false;
      }
      if (this.data.BorderTown != null && (this.data.BorderTown.Technology < this.data.BorderTown.TechnologyMax ||
                                           this.data.BorderTown.Wall < this.data.BorderTown.WallMax))
      {
        return await this.InputMoveToTownAsync(repo, this.data.BorderTown.Id);
      }
      return await this.InputMoveToTownAsync(repo, this.data.DevelopTown.Id);
    }

    protected async Task<bool> InputMoveToBorderTownInWarAsync(MainRepository repo)
    {
      if (!this.GetWaringCountries().Any() && !this.GetNearReadyForWarCountries().Any())
      {
        return false;
      }
      return await this.InputMoveToBorderTownAsync(repo);
    }

    protected async Task<bool> InputMoveToBorderTownInNotWarAndSecurityNotMaxAsync(MainRepository repo)
    {
      if (this.GetWaringCountries().Any() || this.GetNearReadyForWarCountries().Any())
      {
        return false;
      }
      if (this.data.BorderTown == null)
      {
        return false;
      }
      if (this.data.BorderTown.Security >= 100)
      {
        return false;
      }
      return await this.InputMoveToBorderTownAsync(repo);
    }

    protected async Task<bool> InputMoveToBorderTownAsync(MainRepository repo)
    {
      if (this.data.BorderTown == null)
      {
        return false;
      }
      return await this.InputMoveToTownAsync(repo, this.data.BorderTown.Id);
    }

    protected async Task<bool> InputMoveToMainTownAsync(MainRepository repo)
    {
      return await this.InputMoveToTownAsync(repo, this.data.MainTown.Id);
    }

    protected async Task<bool> InputBuildSubBuildingAsync(MainRepository repo)
    {
      if (this.Town.Id != this.Country.CapitalTownId &&
        this.Town.Id != this.data.MainTown?.Id &&
        this.Town.Id != this.data.BorderTown?.Id)
      {
        return false;
      }

      if (this.Character.Money < 35000)
      {
        return false;
      }

      // 必要な建築物を決める
      var warCountries = this.GetReadyForWarCountries().Concat(this.GetWaringCountries());
      var towns = await repo.Town.GetAllAsync();
      var aroundWarTowns = towns.GetAroundTowns(this.Town).Where(t => warCountries.Contains(t.CountryId));
      var aroundWaringSubbuildings = new List<TownSubBuilding>();
      foreach (var town in aroundWarTowns)
      {
        aroundWaringSubbuildings.AddRange(await repo.Town.GetSubBuildingsAsync(town.Id));
      }
      var needs = new List<TownSubBuildingType>
      {
        TownSubBuildingType.Workshop,
        TownSubBuildingType.Houses,
      };
      if (aroundWaringSubbuildings.Count(ts => ts.Type == TownSubBuildingType.Agitation) >= 2)
      {
        needs.Remove(TownSubBuildingType.Houses);
        needs.Add(TownSubBuildingType.DefenseStation);
      }

      var subBuildings = await repo.Town.GetSubBuildingsAsync(this.Town.Id);

      if (subBuildings.Any(s => (s.Status == TownSubBuildingStatus.UnderConstruction || s.Status == TownSubBuildingStatus.Removing) &&
          s.CharacterId == this.Character.Id))
      {
        return false;
      }

      // 撤去する
      var removeTarget = subBuildings.FirstOrDefault(s => s.Status == TownSubBuildingStatus.Available && !needs.Contains(s.Type));
      if (removeTarget != null)
      {
        this.command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 1,
          NumberValue = (int)removeTarget.Type,
        });
        this.command.Type = CharacterCommandType.RemoveSubBuilding;
        return true;
      }

      // 建設する
      if (needs.Count < 1 || subBuildings.Count() >= needs.Count())
      {
        return false;
      }
      if (!subBuildings.Any(s => s.Type == needs[0]))
      {
        this.command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 1,
          NumberValue = (int)needs[0],
        });
        this.command.Type = CharacterCommandType.BuildSubBuilding;
        return true;
      }

      if (this.Town.Type != TownType.Large || needs.Count < 2)
      {
        return false;
      }

      if (!subBuildings.Any(s => s.Type == needs[1]))
      {
        this.command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 1,
          NumberValue = (int)needs[1],
        });
        this.command.Type = CharacterCommandType.BuildSubBuilding;
        return true;
      }

      return false;
    }

    protected bool InputWallDevelop()
    {
      if (this.data.BorderTown != null)
      {
        return false;
      }

      if (this.Town.Wall >= this.Town.WallMax && this.Town.Technology >= this.Town.TechnologyMax)
      {
        var match = this.GetMatchTown(this.towns, t => t.Wall * -1, t => t.CountryId == this.Country.Id && (t.Wall < t.WallMax || t.Technology < t.TechnologyMax));
        if (match != null)
        {
          var street = this.GetStreet(this.towns, this.Town, match);
          if (street != null && street.Count() > 1)
          {
            this.InputMoveToTown(street.ElementAt(1).Id, this.command);
            return true;
          }
        }

        return false;
      }

      if (this.Town.Technology < this.Town.TechnologyMax)
      {
        command.Type = CharacterCommandType.Technology;
        return true;
      }
      if (this.Town.Wall < this.Town.WallMax)
      {
        command.Type = CharacterCommandType.Wall;
        return true;
      }

      return false;
    }

    protected bool InputDevelopOnBorderOrMain()
    {
      if (this.data.BorderTown != null && this.data.BorderTown.Id == this.Town.Id)
      {
        return this.InputDevelop();
      }

      if (this.data.MainTown.Id == this.Town.Id)
      {
        return this.InputDevelop();
      }

      return false;
    }

    protected bool InputDevelopOnBorderOrMainLow()
    {
      if (this.data.BorderTown != null && this.data.BorderTown.Id == this.Town.Id)
      {
        return this.InputDevelopLow();
      }

      if (this.data.MainTown.Id == this.Town.Id)
      {
        return this.InputDevelopLow();
      }

      return false;
    }

    protected bool InputDevelop()
    {
      var isDevelopIncome = this.Character.AiType != CharacterAiType.FlyingColumn &&
        (this.Country.LastMoneyIncomes < 10000 || this.Country.LastRiceIncomes < 10000);

      var v = this.GameDateTime.Month % 2;
      if (v == 0)
      {
        command.Type = CharacterCommandType.Wall;
        if (isDevelopIncome)
        {
          command.Type = CharacterCommandType.Agriculture;
        }
      }
      else
      {
        command.Type = CharacterCommandType.Technology;
        if (isDevelopIncome)
        {
          command.Type = CharacterCommandType.Commercial;
        }
      }

      // 内政の優先順位
      if (this.Town.Technology < 801)
      {
        command.Type = CharacterCommandType.Technology;
      }

      // 最大値を監視
      if (command.Type == CharacterCommandType.Agriculture && this.Town.Agriculture >= this.Town.AgricultureMax)
      {
        command.Type = CharacterCommandType.Commercial;
      }
      if (command.Type == CharacterCommandType.Commercial && this.Town.Commercial >= this.Town.CommercialMax)
      {
        command.Type = CharacterCommandType.Wall;
      }
      if (command.Type == CharacterCommandType.Wall && this.Town.Wall >= this.Town.WallMax)
      {
        command.Type = CharacterCommandType.Technology;
      }
      if (command.Type == CharacterCommandType.Technology && this.Town.Technology >= this.Town.TechnologyMax)
      {
        command.Type = this.Town.Wall < this.Town.WallMax ? CharacterCommandType.Wall : CharacterCommandType.TownBuilding;
      }
      if (command.Type == CharacterCommandType.TownBuilding &&
        (this.Town.TownBuildingValue >= Config.TownBuildingMax ||
         (this.Town.TownBuilding != TownBuilding.RepairWall &&
          this.Town.TownBuilding != TownBuilding.TerroristHouse &&
          this.Town.TownBuilding != TownBuilding.Houses &&
          this.Town.TownBuilding != TownBuilding.OpenWall &&
          this.Town.TownBuilding != TownBuilding.TrainingBuilding &&
          this.Town.TownBuilding != TownBuilding.School &&
          this.Town.TownBuilding != TownBuilding.Palace)))
      {
        return false;
      }

      if (this.Character.AiType != CharacterAiType.FlyingColumn &&
        ((this.Town.Security < 60 && this.Character.Popularity < 50) || (this.Town.Security < 100 && this.Character.Popularity >= 50)))
      {
        command.Type = CharacterCommandType.Security;
      }
      return true;
    }

    protected bool InputDevelopLow()
    {
      var v = this.GameDateTime.Month % 2;
      if (v == 0)
      {
        command.Type = CharacterCommandType.Wall;
      }
      else
      {
        command.Type = CharacterCommandType.Technology;
      }

      // 内政の優先順位（謀略も考慮）
      if (this.Town.Technology < 401)
      {
        command.Type = CharacterCommandType.Technology;
      }

      // 最大値を監視
      if (command.Type == CharacterCommandType.Wall && this.Town.Wall >= this.Town.WallMax / 2)
      {
        command.Type = CharacterCommandType.Technology;
      }
      if (command.Type == CharacterCommandType.Technology && this.Town.Technology >= this.Town.TechnologyMax / 2)
      {
        return this.InputDevelop();
      }

      return true;
    }

    protected bool InputDevelopInWar()
    {
      if (!this.GetWaringCountries().Any())
      {
        return false;
      }

      if (this.Town.CountryId != this.Character.CountryId)
      {
        return false;
      }

      if (this.Town.Id != this.data.BorderTown?.Id && this.Town.Id != this.data.MainTown?.Id)
      {
        return false;
      }

      if ((this.GameDateTime.Month % 2 == 0 || this.Town.Technology >= this.Town.TechnologyMax) && this.Town.Wall < this.Town.WallMax)
      {
        this.command.Type = CharacterCommandType.Wall;
        return true;
      }

      if (this.Town.Technology < this.Town.TechnologyMax)
      {
        this.command.Type = CharacterCommandType.Technology;
        return true;
      }

      return false;
    }

    protected async Task<bool> InputMissionaryAsync(MainRepository repo)
    {
      if (this.Town.Religion != this.Country.Religion ||
        this.Town.GetReligionPoint(this.Town.Religion) < this.Town.GetReligionPoint(this.Town.SecondReligion) + 1000)
      {
        if (this.Town.CountryId != this.Country.Id)
        {
          var alliance = await repo.CountryDiplomacies.GetCountryAllianceAsync(this.Town.CountryId, this.Country.Id);
          if (alliance.HasData && !alliance.Data.CanMissionary)
          {
            return false;
          }
        }

        this.command.Type = CharacterCommandType.Missionary;
        return true;
      }

      return false;
    }

    protected void InputMissionaryForce()
    {
      this.command.Type = CharacterCommandType.Missionary;
    }

    protected bool InputPolicy()
    {
      if (this.Country.PolicyPoint < 10000)
      {
        this.InputPolicyForce();
        return true;
      }

      return false;
    }

    protected void InputPolicyForce()
    {
      this.command.Type = CharacterCommandType.Policy;
    }

    protected bool InputTownBuilding()
    {
      if (this.Town.TownBuildingValue < Config.TownBuildingMax)
      {
        this.command.Type = CharacterCommandType.TownBuilding;
        return true;
      }

      return false;
    }

    protected bool InputSoldierTraining()
    {
      if (this.Character.Proficiency < 100 && this.Character.SoldierNumber > 0)
      {
        this.command.Type = CharacterCommandType.SoldierTraining;
        return true;
      }
      return false;
    }

    protected void InputSoldierTrainingForce()
    {
      this.command.Type = CharacterCommandType.SoldierTraining;
    }

    protected bool InputSoldierTrainingIfNoMoney(int min)
    {
      if (this.Character.Money < min)
      {
        this.command.Type = CharacterCommandType.SoldierTraining;
        return true;
      }
      return false;
    }

    protected void InputTraining(TrainingType type)
    {
      this.command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 1,
        NumberValue = (int)type,
      });
      this.command.Type = CharacterCommandType.Training;
    }

    protected bool InputMoveToCountryTown(uint countryId)
    {
      if (this.Town.CountryId == countryId)
      {
        return false;
      }
      var nextTown = this.GetMatchTown(this.towns, null, t => t.CountryId == countryId, false);
      var street = this.GetStreet(this.towns, this.Town, nextTown);
      if (street.Count() >= 2)
      {
        this.InputMoveToTown(street.ElementAt(1).Id, this.command);
        return true;
      }
      return false;
    }

    protected void InputJoinCountry()
    {
      this.command.Type = CharacterCommandType.Join;
    }

    protected void InputSafeOut(uint characterId, int money)
    {
      this.command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 1,
        NumberValue = (int)characterId,
      });
      this.command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 2,
        NumberValue = money,
      });
      this.command.Type = CharacterCommandType.SafeOut;
    }

    protected void InputAddSecretary(CharacterAiType type)
    {
      this.command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 1,
        NumberValue = (int)type,
      });
      this.command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 2,
        NumberValue = (int)this.Country.CapitalTownId,
      });
      this.command.Type = CharacterCommandType.AddSecretary;
    }

    protected void InputMoveSecretary(uint id, uint townId)
    {
      this.command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 1,
        NumberValue = (int)id,
      });
      this.command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 2,
        NumberValue = (int)townId,
      });
      this.command.Type = CharacterCommandType.SecretaryToTown;
    }

    protected void InputRemoveSecretary(uint id)
    {
      this.command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 1,
        NumberValue = (int)id,
      });
      this.command.Type = CharacterCommandType.RemoveSecretary;
    }

    protected bool InputMoveToTown(uint townId)
    {
      if (this.Character.TownId != townId)
      {
        this.InputMoveToTown(townId, this.command);
        return true;
      }

      return false;
    }

    protected async Task<bool> InputRiceAsync(MainRepository repo, int maxAssets)
    {
      var isWar = this.GetWaringCountries().Any(w => w != 0) ||
        this.GetNearReadyForWarCountries().Any(w => w != 0) ||
        this.GetReadyForWarCountries().Any(w => w != 0);
      if (isWar)
      {
        return false;
      }

      return await this.InputRiceAlwaysAsync(repo, maxAssets);
    }

    protected async Task<bool> InputRiceAlwaysAsync(MainRepository repo, int maxAssets)
    {
      if (this.Character.Money + this.Character.Rice > maxAssets)
      {
        return false;
      }

      if (await this.InputSellRiceAsync(repo, 1.16f))
      {
        return true;
      }

      if (await this.InputBuyRiceAsync(repo, 0.84f))
      {
        return true;
      }

      return false;
    }

    protected async Task<bool> InputRiceAnywayAsync(MainRepository repo, int maxAssets, int minAssets)
    {
      if (this.Character.Money + this.Character.Rice > maxAssets)
      {
        return false;
      }

      if (this.Character.Rice >= minAssets && await this.InputSellRiceAsync(repo, 1.05f))
      {
        return true;
      }

      if (this.Character.Money >= minAssets && await this.InputBuyRiceAsync(repo, 0.95f))
      {
        return true;
      }

      return false;
    }

    protected async Task<bool> InputExpandRicePriceRangeAsync(MainRepository repo)
    {
      var min = await repo.AiActionHistory.GetMinRicePriceAsync(this.Character.Id, this.GameDateTime.ToInt() - 199);
      var max = await repo.AiActionHistory.GetMaxRicePriceAsync(this.Character.Id, this.GameDateTime.ToInt() - 199);

      var minLower = this.towns.OrderBy(t => t.RicePrice).FirstOrDefault(t => t.RicePrice < min && t.RicePrice < 0.95f);
      var maxHigher = this.towns.OrderByDescending(t => t.RicePrice).FirstOrDefault(t => t.RicePrice > max && t.RicePrice > 1.05f);

      if (maxHigher != null)
      {
        if (await this.InputMoveToTownAsync(repo, maxHigher.Id))
        {
          return true;
        }
      }
      if (minLower != null)
      {
        if (await this.InputMoveToTownAsync(repo, minLower.Id))
        {
          return true;
        }
      }

      return false;
    }

    protected async Task<bool> InputBuyRiceAsync(MainRepository repo, float rateMax)
    {
      var rate = this.GameDateTime.Year > Config.UpdateStartYear ?
        await repo.AiActionHistory.GetMinRicePriceAsync(this.Character.Id, this.GameDateTime.ToInt() - 200) : this.Town.RicePrice;
      var isWar = this.GetWaringCountries().Any() || this.GetNearReadyForWarCountries().Any() || this.GetReadyForWarCountries().Any();
      if (this.Character.Money > (this.Character.GetCharacterType() == CharacterType.Popularity ? 5000 : isWar ? 20000 : 5000) &&
          rate <= rateMax)
      {
        var a = Math.Min(Config.RiceBuyMax, this.Character.Money - 5000);
        this.command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 1,
          NumberValue = 1,
        });
        this.command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 2,
          NumberValue = a,
        });
        this.command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 3,
          NumberValue = (int)((2 - rate) * a),
        });
        this.command.Type = CharacterCommandType.Rice;
        return true;
      }

      return false;
    }

    protected async Task<bool> InputSellRiceAsync(MainRepository repo, float rateMin)
    {
      var rate = this.GameDateTime.Year > Config.UpdateStartYear ?
        await repo.AiActionHistory.GetMaxRicePriceAsync(this.Character.Id, this.GameDateTime.ToInt() - 200) : this.Town.RicePrice;
      var isWar = this.GetWaringCountries().Any() || this.GetNearReadyForWarCountries().Any() || this.GetReadyForWarCountries().Any();
      if (this.Character.Rice > (isWar ? 10000 : 5000) &&
          rate >= rateMin)
      {
        var a = Math.Min(Config.RiceBuyMax, this.Character.Rice - 5000);
        await this.InputSellRiceForceAsync(repo, a);
        return true;
      }

      return false;
    }

    protected async Task<bool> InputSellRiceForReadyWarAsync(MainRepository repo)
    {
      if (this.GetWaringCountries().Any() || this.GetNearReadyForWarCountries().Any())
      {
        return false;
      }

      var first = this.GetFirstWarStartMonth() - this.GameDateTime.ToInt();
      if (first > 500)
      {
        return false;
      }
      if (first < 24)
      {
        return await this.InputSellRiceAsync(repo, 0.0f);
      }
      if (first < 48)
      {
        return await this.InputSellRiceAsync(repo, 0.9f);
      }
      if (first < 80)
      {
        return await this.InputSellRiceAsync(repo, 1.0f);
      }
      if (first < 140)
      {
        return await this.InputSellRiceAsync(repo, 1.1f);
      }

      return await this.InputSellRiceAsync(repo, 1.19f);
    }

    private async Task InputSellRiceForceAsync(MainRepository repo, int val)
    {
      var rate = this.GameDateTime.Year > Config.UpdateStartYear ?
        await repo.AiActionHistory.GetMaxRicePriceAsync(this.Character.Id, this.GameDateTime.ToInt() - 200) : this.Town.RicePrice;
      var a = Math.Min(val, this.Character.Rice - 5000);
      this.command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 1,
        NumberValue = 2,
      });
      this.command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 2,
        NumberValue = a,
      });
      this.command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 3,
        NumberValue = (int)(a * rate),
      });
      this.command.Type = CharacterCommandType.Rice;
    }

    protected async Task<bool> InputSafeInAsync(MainRepository repo, int money, int characterMinMoney)
    {
      var availableMoney = this.Character.Money - characterMinMoney;
      if (availableMoney <= 1000)
      {
        return false;
      }

      var policies = await repo.Country.GetPoliciesAsync(this.Country.Id);
      var max = CountryService.GetCountrySafeMax(policies.Where(p => p.Status == CountryPolicyStatus.Available).Select(p => p.Type));
      if (max <= 0 || this.Country.SafeMoney >= max)
      {
        return false;
      }

      this.command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 1,
        NumberValue = Math.Min(money, availableMoney),
      });
      this.command.Type = CharacterCommandType.SafeIn;
      return true;
    }

    protected async Task<bool> InputUseItemAsync(MainRepository repo)
    {
      var items = await repo.Character.GetItemsAsync(this.Character.Id);
      foreach (var item in items)
      {
        var infoOptional = item.GetInfo();
        if (infoOptional.HasData)
        {
          var info = infoOptional.Data;
          if (info.UsingEffects != null && info.CanUse)
          {
            if (info.UsingEffects.Any(e => e.Type == CharacterItemEffectType.FormationEx ||
              e.Type == CharacterItemEffectType.Money ||
              e.Type == CharacterItemEffectType.IntellectEx ||
              e.Type == CharacterItemEffectType.SkillPoint ||
              e.Type == CharacterItemEffectType.SoldierCorrectionResource ||
              e.Type == CharacterItemEffectType.TerroristEnemy))
            {
              this.command.Parameters.Add(new CharacterCommandParameter
              {
                Type = 1,
                NumberValue = (int)item.Type,
              });
              this.command.Type = CharacterCommandType.UseItem;
              return true;
            }
          }
          if (info.Effects != null && item.Status == CharacterItemStatus.CharacterPending)
          {
            var isGet = false;
            var isHandOver = false;
            var handOverTargetId = (uint)0;
            var countryCharacters = await repo.Country.GetCharactersAsync(this.Character.CountryId);
            if (info.Effects.Any(e => e.Type == CharacterItemEffectType.Strong && e.Value >= 5))
            {
              if (this.Character.GetCharacterType() == CharacterType.Strong)
              {
                isGet = true;
              }
              else
              {
                var target = countryCharacters.FirstOrDefault(c => c.GetCharacterType() == CharacterType.Strong);
                if (target != null)
                {
                  handOverTargetId = target.Id;
                  isHandOver = true;
                }
              }
            }
            if (info.Effects.Any(e => e.Type == CharacterItemEffectType.Intellect && e.Value >= 5))
            {
              if (this.Character.GetCharacterType() == CharacterType.Intellect)
              {
                isGet = true;
              }
              else
              {
                var target = countryCharacters.FirstOrDefault(c => c.GetCharacterType() == CharacterType.Intellect);
                if (target != null)
                {
                  handOverTargetId = target.Id;
                  isHandOver = true;
                }
              }
            }
            if (info.Effects.Any(e => e.Type == CharacterItemEffectType.Popularity && e.Value >= 5))
            {
              if (this.Character.GetCharacterType() == CharacterType.Popularity)
              {
                isGet = true;
              }
              else
              {
                var target = countryCharacters.FirstOrDefault(c => c.GetCharacterType() == CharacterType.Popularity);
                if (target != null)
                {
                  handOverTargetId = target.Id;
                  isHandOver = true;
                }
              }
            }
            if (info.Effects.Any(e => e.Type == CharacterItemEffectType.Leadership ||
              e.Type == CharacterItemEffectType.ProficiencyMinimum ||
              e.Type == CharacterItemEffectType.DiscountCavalrySoldierPercentage ||
              e.Type == CharacterItemEffectType.DiscountCrossbowSoldierPercentage ||
              e.Type == CharacterItemEffectType.DiscountInfantrySoldierPercentage ||
              e.Type == CharacterItemEffectType.DiscountSoldierPercentageWithResource))
            {
              isGet = true;
            }

            if (isHandOver)
            {
              this.command.Parameters.Add(new CharacterCommandParameter
              {
                Type = 1,
                NumberValue = (int)item.Type,
              });
              this.command.Parameters.Add(new CharacterCommandParameter
              {
                Type = 2,
                NumberValue = (int)handOverTargetId,
              });
              this.command.Type = CharacterCommandType.HandOverItem;
              return true;
            }
            else
            {
              if (isGet)
              {
                await ItemService.SetCharacterAsync(repo, item, this.Character);
              }
              else
              {
                await ItemService.ReleaseCharacterAsync(repo, item, this.Character);
              }
            }
          }
        }
      }

      return false;
    }

    protected void MoveToRandomTown()
    {
      var arounds = this.towns.GetAroundTowns(this.Town);
      this.InputMoveToTown(RandomService.Next(arounds).Id);
    }

    protected enum TrainingType
    {
      Strong = 1,
      Intellect = 2,
      Leadership = 3,
      Popularity = 4,
    }

    protected enum DefendLevel
    {
      Always,
      NeedMyDefend,
      NeedThreeDefend,
      NeedTwoDefend,
      NeedAnyDefends,
      NotCare,
    }

    protected enum DefendSeiranLevel
    {
      NotCare,
      HalfSeirans,
      Seirans,
      SeiransWithUnits,
    }

    protected enum ForceDefendPolicyLevel
    {
      NotCare,
      Negative,
      Medium,
      Aggressive,
    }

    protected enum AttackMode
    {
      Normal,
      KillDefenders,
      BreakWall,
      GetTown,
    }

    private class AiCountryData
    {
      /// <summary>
      /// 標的
      /// </summary>
      public Town TargetTown { get; set; }

      /// <summary>
      /// 次に攻めるところ
      /// </summary>
      public Town NextBattleTown { get; set; }

      /// <summary>
      /// 内政
      /// </summary>
      public Town DevelopTown { get; set; }

      /// <summary>
      /// 前線
      /// </summary>
      public Town BorderTown { get; set; }

      /// <summary>
      /// 拠点
      /// </summary>
      public Town MainTown { get; set; }

      /// <summary>
      /// 攻略対象候補
      /// </summary>
      public Town WillTownWarTown { get; set; }

      public bool IsDefendForce { get; set; }
    }
  }
}
