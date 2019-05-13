using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Migrations;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;
using SoldierType = SangokuKmy.Models.Data.Entities.SoldierType;
using SangokuKmy.Models.Services;
using SangokuKmy.Common;

namespace SangokuKmy.Models.Updates.Ai
{
  public abstract class WorkerAiCharacter : AiCharacter
  {
    private AiCountryData data;
    private CharacterCommand command;
    private IEnumerable<CountryWar> wars;
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

    protected virtual ForceDefendPolicyLevel ForceDefendPolicy => ForceDefendPolicyLevel.NotCare;

    protected virtual DefendLevel NeedDefendLevel => DefendLevel.NeedMyDefend;

    protected virtual DefendSeiranLevel NeedDefendSeiranLevel => DefendSeiranLevel.Seirans;

    protected virtual UnitPolicyLevel UnitLevel => UnitPolicyLevel.NotCare;

    protected virtual UnitGatherPolicyLevel UnitGatherLevel => UnitGatherPolicyLevel.Always;

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

      if (!mainWars.Any() && managementOptional.HasData)
      {
        var country = await repo.Country.GetByIdAsync(managementOptional.Data.VirtualEnemyCountryId);
        if (country.HasData)
        {
          mainWars = mainWars.Append(country.Data.Id);
        }
      }

      var targetOrder = BattleTargetOrder.Defenders;

      var old = await repo.AiCountry.GetStorategyByCountryIdAsync(this.Character.CountryId);
      if (old.HasData)
      {
        // 作戦見直し
        if (old.Data.IntNextResetGameDate <= this.GameDateTime.ToInt())
        {
          if (!availableWars.Any())
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

        var units = await repo.Unit.GetByCountryIdAsync(this.Country.Id);
        var main = this.towns.FirstOrDefault(t => t.Id == old.Data.MainTownId);
        var target = this.towns.FirstOrDefault(t => t.Id == old.Data.TargetTownId);
        var border = this.towns.FirstOrDefault(t => t.Id == old.Data.BorderTownId);
        var nextTarget = this.towns.FirstOrDefault(t => t.Id == old.Data.NextTargetTownId);
        var mainUnit = units.FirstOrDefault(u => u.Id == old.Data.MainUnitId);
        var borderUnit = units.FirstOrDefault(u => u.Id == old.Data.BorderUnitId);
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

      if (this.data.BorderTown != null && this.data.MainTown != null && this.data.BorderTown.Id != this.data.MainTown.Id)
      {
        if (this.towns.GetAroundTowns(this.data.MainTown).Any(t => mainWars.Contains(t.CountryId)))
        {
          // 重要都市が敵国に隣接したら、ひとまず重要都市に退却
          this.data.BorderTown = this.data.MainTown;
        }
      }

      if (this.data.BorderTown != null &&
        (!this.towns.GetAroundTowns(this.data.BorderTown).Any(t => mainWars.Contains(t.CountryId)) ||
         (availableWars.Any() && !this.towns.GetAroundTowns(this.data.BorderTown).Any(t => availableWars.Contains(t.CountryId)))))
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

      // 部隊作成、解散
      if (this.data.BorderTown != null && (this.UnitLevel == UnitPolicyLevel.BorderOnly || this.UnitLevel == UnitPolicyLevel.BorderOnlyAndBeforePeopleChange))
      {
        if (this.data.BorderUnit != null)
        {
          var leader = this.data.BorderUnit.Members.FirstOrDefault(m => m.Post == UnitMemberPostType.Leader);
          if (leader == null || leader.Character.TownId != this.data.BorderTown.Id)
          {
            await UnitService.RemoveAsync(repo, this.data.BorderUnit.Id);
            this.data.BorderUnit = null;
          }
        }
        if (this.data.BorderUnit == null)
        {
          var charas = await repo.Country.GetCharactersAsync(this.Country.Id);
          var leader = charas.Where(c => c.TownId == this.data.BorderTown.Id).FirstOrDefault();
          if (leader != null)
          {
            var unit = new Unit
            {
              CountryId = this.Country.Id,
              Name = $"前線 {this.data.BorderTown.Name} 集合部隊",
            };
            await UnitService.CreateAndSaveAsync(repo, unit, leader.Id);
            this.data.BorderUnit = unit;
          }
        }
      }

      // 強制的に全員守備ループするかどうか
      if (this.ForceDefendPolicy != ForceDefendPolicyLevel.NotCare)
      {
        var history = await repo.AiActionHistory.GetAsync(mainWars, AiBattleTownType.MainAndBorderTown, this.Country.Id);
        var myHistory = await repo.AiActionHistory.GetAsync(this.Country.Id, AiBattleTownType.MainAndBorderTown, mainWars);

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
        var money = 0;
        if (!history.Any())
        {
          this.data.IsDefendForce = false;
        }
        else if (this.ForceDefendPolicy == ForceDefendPolicyLevel.Aggressive)
        {
          this.data.IsDefendForce = !targets.Any() &&
            myAttacks.Min(h => h.RestDefenderCount) >= 1;
          money = 26_0000;
        }
        else if (this.ForceDefendPolicy == ForceDefendPolicyLevel.Medium)
        {
          this.data.IsDefendForce = !targets.Any(t => t.RestDefenderCount == 0) &&
            !myAttacks.Any(h => h.TargetType == AiBattleTargetType.Wall);
          money = 18_0000;
        }
        else if (this.ForceDefendPolicy == ForceDefendPolicyLevel.Negative)
        {
          this.data.IsDefendForce = !targets.Any() &&
            myAttacks.Count(h => h.TargetType == AiBattleTargetType.CharacterLowSoldiers) > myAttacks.Count(h => h.TargetType == AiBattleTargetType.Character);
          money = 10_0000;
        }

        if (this.data.IsDefendForce)
        {
          var charas = await repo.Country.GetCharactersAsync(this.Country.Id);
          this.data.IsDefendForce = charas
            .Where(c => c.AiType != CharacterAiType.Human && c.AiType.CanBattle())
            .Any(c => c.Money < money);
        }
      }

      // DBに保存
      var oldData = old.Data ?? new AiCountryStorategy
      {
        CountryId = this.Country.Id,
      };
      oldData.MainTownId = this.data.MainTown?.Id ?? 0;
      oldData.BorderTownId = this.data.BorderTown?.Id ?? 0;
      oldData.NextTargetTownId = this.data.NextBattleTown?.Id ?? 0;
      oldData.TargetTownId = this.data.TargetTown?.Id ?? 0;
      oldData.MainUnitId = this.data.MainUnit?.Id ?? 0;
      oldData.BorderUnitId = this.data.BorderUnit?.Id ?? 0;
      oldData.IsDefendForce = this.data.IsDefendForce && this.ForceDefendPolicy != ForceDefendPolicyLevel.NotCare;
      if (!old.HasData)
      {
        await repo.AiCountry.AddAsync(oldData);
      }

      return true;
    }

    protected override async Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      this.wars = wars;
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

      if (!await this.InitializeAiDataAsync(repo))
      {
        return this.command;
      }

      await this.ActionAsync(repo);

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

    protected abstract Task ActionAsync(MainRepository repo);

    protected virtual async Task<bool> ActionAsNoCountryAsync(MainRepository repo)
    {
      return false;
    }

    private IEnumerable<uint> GetWaringCountries()
    {
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
        .Where(w => w.IntStartGameDate > this.GameDateTime.ToInt() && (w.Status == CountryWarStatus.InReady));
      if (availableWars.Any())
      {
        return availableWars.Min(w => w.IntStartGameDate);
      }

      return int.MaxValue;
    }

    private async Task<bool> IsGatherUnitNextTurnAsync(MainRepository repo, UnitPolicyLevel level, Unit unit)
    {
      if (level == UnitPolicyLevel.NotCare)
      {
        return false;
      }

      var leader = unit.Members.FirstOrDefault(m => m.Post == UnitMemberPostType.Leader);
      if (leader == null)
      {
        return false;
      }

      var town = await repo.Town.GetByIdAsync(leader.Character.TownId);
      if (!town.HasData)
      {
        return false;
      }

      if (level == UnitPolicyLevel.BorderOnlyAndBeforePeopleChange)
      {
        if (town.Data.Security >= 50)
        {
          var month = leader.Character.LastUpdatedGameDate.NextMonth().Month;
          if (month != 11 && month != 12 && month != 5 && month != 6)
          {
            return false;
          }
        }
      }

      var defenders = await repo.Town.GetDefendersAsync(town.Data.Id);
      if (!defenders.Any())
      {
        return false;
      }

      var memberCount = unit.Members.Count(m => m.CharacterId != this.Character.Id) + 1;
      var needMemberCount = unit.Members.Count(m => m.Character.TownId != leader.Character.TownId && m.CharacterId != this.Character.Id);
      if (this.Character.TownId != leader.Character.TownId)
      {
        needMemberCount++;
      }
      if (this.UnitGatherLevel == UnitGatherPolicyLevel.Always)
      {
        return needMemberCount > 0;
      }
      if (this.UnitGatherLevel == UnitGatherPolicyLevel.Need1_2)
      {
        return needMemberCount >= memberCount / 2;
      }
      if (this.UnitGatherLevel == UnitGatherPolicyLevel.Need1_3)
      {
        return needMemberCount >= memberCount / 3;
      }

      return false;
    }

    private async Task<bool> InputMoveOrJoinUnitAsync(MainRepository repo, uint townId)
    {
      async Task<bool> RunAsync(Unit unit, Town t)
      {
        if (unit != null &&
          t != null &&
          t.Id == townId)
        {
          if (!unit.Members.Any(u => u.Character.Id == this.Character.Id))
          {
            await UnitService.EntryAsync(repo, unit.Id, this.Character.Id);
          }
          return await this.IsGatherUnitNextTurnAsync(repo, this.UnitLevel, unit);
        }

        if (unit != null && unit.Members.Any(u => u.CharacterId == this.Character.Id))
        {
          UnitService.Leave(repo, this.Character.Id);
        }
        return false;
      }

      if (await RunAsync(this.data.BorderUnit, this.data.BorderTown))
      {
        return false;
      }

      if (await RunAsync(this.data.MainUnit, this.data.MainTown))
      {
        return false;
      }

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
      return this.Character.SoldierNumber < Math.Min(30, this.GetSoldierNumberMax(await this.FindSoldierTypeAsync(repo, will.Town))) &&
          (this.GetWaringCountries().Any() ||
           this.GetNearReadyForWarCountries().Any());
    }

    private Town GetTownForSoldiers(int num = -1)
    {
      if (num <= 0)
      {
        num = this.Character.Leadership;
      }

      bool predicate(TownBase t) => t.CountryId == this.Character.CountryId &&
        t.People + 1 >= num * Config.SoldierPeopleCost &&
        t.Security + 1 > num / 10;
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

      if (predicate(this.Town) && this.Town.Technology > 300)
      {
        return this.Town;
      }

      if (this.data.BorderTown != null && predicate(this.data.BorderTown) && this.data.BorderTown.Technology > 300)
      {
        return this.data.BorderTown;
      }

      var targets = arounds.Where(predicate);
      if (targets.Any())
      {
        return (Town)targets.OrderByDescending(t => t.Technology + t.People).First();
      }

      if (predicate(this.data.MainTown) && this.data.MainTown.Technology > 500)
      {
        return this.data.MainTown;
      }

      var myTowns = this.towns
        .Where(t => t.CountryId == this.Character.CountryId)
        .Where(predicate)
        .OrderByDescending(t => t.Technology);
      if (myTowns.Any())
      {
        return myTowns.First();
      }
      else
      {
        return this.towns.Where(t => t.CountryId == this.Character.CountryId).OrderByDescending(t => t.People).FirstOrDefault() ?? this.Town;
      }
    }

    private async Task<bool> GetSoldiersAsync(MainRepository repo, int num = -1)
    {
      if (await this.IsNeedSoldiersAsync(repo))
      {
        if (!this.CanSoldierForce || this.Town.CountryId != this.Character.CountryId)
        {
          var town = this.GetTownForSoldiers();
          var isMove = await this.InputMoveOrJoinUnitAsync(repo, town.Id);
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

        if (this.Town.People + 1 < num * Config.SoldierPeopleCost || this.Town.Security + 1 < num / 10)
        {
          return await this.InputMoveOrJoinUnitAsync(repo, this.GetTownForSoldiers(num).Id);
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

      return (targetTown, null);
    }

    protected async Task<bool> InputBattleAsync(MainRepository repo)
    {
      if (!this.GetWaringCountries().Any() && !this.GetNearReadyForWarCountries().Any())
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

      if (await this.GetSoldiersAsync(repo))
      {
        return true;
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

      Town targetTown = null;

      // 井闌判定
      if (this.Town.Wall > 0 && this.NeedDefendSeiranLevel != DefendSeiranLevel.NotCare)
      {
        Town seiranTown = null;
        var enemyNormalAttackers = new List<Character>();
        var enemyWallAttackers = new List<Character>();
        var enemyTowns = this.GetAroundTargetTowns();
        var enemyCharas = new List<Character>();
        foreach (var town in enemyTowns)
        {
          var charas = await repo.Town.GetCharactersAsync(town.Id);
          foreach (var c in charas.Where(c => c.SoldierNumber >= 10))
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

      if (targetTown != null && this.GetWaringCountries().Any())
      {
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

    protected async Task<bool> InputDefendAsync(MainRepository repo, DefendLevel level)
    {
      if (level == DefendLevel.NotCare)
      {
        return false;
      }

      async Task<bool> RunAsync(Town town)
      {
        if (level == DefendLevel.Always)
        {
          this.command.Type = CharacterCommandType.Defend;
          return true;
        }

        var defends = await repo.Town.GetDefendersAsync(town.Id);
        if (!defends.Any(d => d.Character.Id == this.Character.Id))
        {
          if (level == DefendLevel.NeedMyDefend)
          {
            this.command.Type = CharacterCommandType.Defend;
            return true;
          }

          var size = 0;
          if (level == DefendLevel.NeedThreeDefend) size = 3;
          if (level == DefendLevel.NeedTwoDefend) size = 2;
          if (level == DefendLevel.NeedAnyDefends) size = 1;
          if (defends.Count() < size)
          {
            this.command.Type = CharacterCommandType.Defend;
            return true;
          }
        }

        return false;
      }

      if (this.Town.CountryId != this.Character.CountryId)
      {
        return false;
      }

      if (!this.GetWaringCountries().Any() && !this.GetNearReadyForWarCountries().Any())
      {
        return false;
      }

      if (await this.GetSoldiersAsync(repo))
      {
        return true;
      }

      if (this.data.BorderTown == null)
      {
        return false;
      }

      if (this.Town.Id != this.data.BorderTown.Id && this.Town.Id != this.data.MainTown.Id)
      {
        return false;
      }

      return await RunAsync(this.Town);
    }

    protected async Task<bool> InputCountryDefendLoopAsync(MainRepository repo)
    {
      if (this.ForceDefendPolicy == ForceDefendPolicyLevel.NotCare)
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

      if (await this.InputDefendAsync(repo, DefendLevel.NeedMyDefend))
      {
        return true;
      }

      var num1 = this.Town.People < 2000 ? 1 : this.Town.People < 5000 ? 5 : this.Town.People < 7000 ? 9 : this.Town.People / 700;
      var num2 = this.Character.Money < 30000 ? 1 : this.Character.Money < 80000 ? 9 : this.Character.Leadership;
      var num = Math.Min(num1, num2);
      if (await this.GetSoldiersAsync(repo, num))
      {
        return true;
      }

      if (this.Character.Intellect > this.Character.Strong)
      {
        if (this.Town.Technology < this.Town.TechnologyMax)
        {
          this.command.Type = CharacterCommandType.Technology;
          return true;
        }

        if (this.Town.Wall < this.Town.WallMax)
        {
          this.command.Type = CharacterCommandType.Wall;
          return true;
        }

        this.InputTraining(TrainingType.Intellect);
        return true;
      }
      else
      {
        this.InputTraining(TrainingType.Strong);
        return true;
      }
    }

    protected async Task<bool> InputDefendAsync(MainRepository repo) => await this.InputDefendAsync(repo, this.NeedDefendLevel);

    protected bool InputSecurity()
    {
      if (this.Town.Security < 100)
      {
        this.command.Type = CharacterCommandType.SuperSecurity;
        return true;
      }
      return false;
    }

    protected void InputSecurityForce()
    {
      this.command.Type = CharacterCommandType.Security;
    }

    protected async Task<bool> InputMoveToQuickestImportantTownAsync(MainRepository repo)
    {
      if (this.Town.CountryId == this.Character.CountryId)
      {
        return false;
      }

      if (this.data.BorderTown != null && this.data.BorderTown.IsNextToTown(this.Town))
      {
        return await this.InputMoveOrJoinUnitAsync(repo, this.data.BorderTown.Id);
      }

      if (this.data.MainTown != null && this.data.MainTown.IsNextToTown(this.Town))
      {
        return await this.InputMoveOrJoinUnitAsync(repo, this.data.MainTown.Id);
      }

      return false;
    }

    protected async Task<bool> InputMoveToBorderTownAsync(MainRepository repo)
    {
      if (this.data.BorderTown == null)
      {
        return false;
      }
      return await this.InputMoveOrJoinUnitAsync(repo, this.data.BorderTown.Id);
    }

    protected async Task<bool> InputMoveToMainTownAsync(MainRepository repo)
    {
      return await this.InputMoveOrJoinUnitAsync(repo, this.data.MainTown.Id);
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
      if (this.Town.Technology < 801)
      {
        command.Type = CharacterCommandType.Technology;
      }

      // 最大値を監視
      if (command.Type == CharacterCommandType.Wall && this.Town.Wall >= this.Town.WallMax)
      {
        command.Type = CharacterCommandType.Technology;
      }
      if (command.Type == CharacterCommandType.Technology && this.Town.Technology >= this.Town.TechnologyMax)
      {
        command.Type = CharacterCommandType.TownBuilding;
      }
      if (command.Type == CharacterCommandType.TownBuilding &&
        (this.Town.TownBuildingValue >= Config.TownBuildingMax ||
         this.Town.TownBuilding != TownBuilding.RepairWall ||
         this.Town.TownBuilding != TownBuilding.TerroristHouse))
      {
        return false;
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

    protected void InputPolicy()
    {
      this.command.Type = CharacterCommandType.Policy;
    }

    protected void InputTownBuilding()
    {
      this.command.Type = CharacterCommandType.TownBuilding;
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

    protected void InputSetUnitSecretary(uint id, uint unitId)
    {
      this.command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 1,
        NumberValue = (int)id,
      });
      this.command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 2,
        NumberValue = (int)unitId,
      });
      this.command.Type = CharacterCommandType.Secretary;
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

    protected bool InputRice()
    {
      var isWar = this.GetWaringCountries().Any() || this.GetNearReadyForWarCountries().Any() || this.GetReadyForWarCountries().Any();
      if (isWar)
      {
        return false;
      }

      return this.InputRiceAlways();
    }

    protected bool InputRiceAlways()
    {
      if (this.InputBuyRice(0.9f))
      {
        return true;
      }

      if (this.InputSellRice(1.1f))
      {
        return true;
      }

      return false;
    }

    protected bool InputBuyRice(float rateMax)
    {
      var isWar = this.GetWaringCountries().Any() || this.GetNearReadyForWarCountries().Any() || this.GetReadyForWarCountries().Any();
      if (this.Character.Money > (this.Character.GetCharacterType() == CharacterType.Popularity ? 5000 : isWar ? 30000 : 5000) &&
          this.Town.RicePrice < rateMax)
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
          NumberValue = (int)((2 - this.Town.RicePrice) * a),
        });
        this.command.Type = CharacterCommandType.Rice;
        return true;
      }

      return false;
    }

    protected bool InputSellRice(float rateMin)
    {
      var isWar = this.GetWaringCountries().Any() || this.GetNearReadyForWarCountries().Any() || this.GetReadyForWarCountries().Any();
      if (this.Character.Rice > (this.Character.GetCharacterType() == CharacterType.Popularity ? 20000 : isWar ? 10000 : 5000) &&
          this.Town.RicePrice > rateMin)
      {
        var a = Math.Min(Config.RiceBuyMax, this.Character.Rice - 5000);
        this.InputSellRiceForce(a);
        return true;
      }

      return false;
    }

    protected bool InputSellRiceForReadyWar()
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
        return this.InputSellRice(0.0f);
      }
      if (first < 36)
      {
        return this.InputSellRice(0.85f);
      }
      if (first < 48)
      {
        return this.InputSellRice(0.9f);
      }
      if (first < 80)
      {
        return this.InputSellRice(1.0f);
      }
      if (first < 140)
      {
        return this.InputSellRice(1.1f);
      }

      return this.InputSellRice(1.19f);
    }

    private void InputSellRiceForce(int val)
    {
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
        NumberValue = (int)(a * this.Town.RicePrice),
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

    protected async Task<bool> InputGatherUnitAsync(MainRepository repo)
    {
      async Task<bool> RunAsync(Unit unit)
      {
        if (unit == null)
        {
          return false;
        }

        if (!unit.Members.Any(m => m.Post == UnitMemberPostType.Leader && m.CharacterId == this.Character.Id))
        {
          return false;
        }

        if (!(await this.IsGatherUnitNextTurnAsync(repo, this.UnitLevel, unit)))
        {
          return false;
        }

        this.command.Type = CharacterCommandType.Gather;
        return true;
      }

      return await RunAsync(this.data.BorderUnit) || await RunAsync(this.data.MainUnit);
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

    protected enum UnitPolicyLevel
    {
      NotCare,
      BorderOnlyAndBeforePeopleChange,
      BorderOnly,
    }

    protected enum UnitGatherPolicyLevel
    {
      Always,
      Need1_3,
      Need1_2,
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
      /// 前線
      /// </summary>
      public Town BorderTown { get; set; }

      /// <summary>
      /// 拠点
      /// </summary>
      public Town MainTown { get; set; }

      /// <summary>
      /// 前線の部隊
      /// </summary>
      public Unit BorderUnit { get; set; }

      /// <summary>
      /// メインの部隊
      /// </summary>
      public Unit MainUnit { get; set; }

      public bool IsDefendForce { get; set; }
    }
  }
}
