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

namespace SangokuKmy.Models.Updates
{
  public abstract class WorkerAiCharacter : AiCharacter
  {
    private AiCountryData data;
    private CharacterCommand command;
    private IEnumerable<CountryWar> wars;
    private IEnumerable<Town> towns;

    protected Town BorderTown => this.data.BorderTown;

    protected virtual bool CanSoldierForce => false;

    protected virtual DefendLevel NeedDefendLevel => DefendLevel.NeedMyDefend;

    protected virtual SoldierType FindSoldierType()
    {
      return SoldierType.Common;
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

      var availableWars = this.GetWaringCountries();
      var readyWars = this.GetReadyForWarCountries();
      var nearReadyWars = this.GetNearReadyForWarCountries();
      var mainWars = availableWars.Any() ? availableWars :
                     nearReadyWars.Any() ? nearReadyWars : readyWars;

      var targetOrder = BattleTargetOrder.Defenders;

      var old = await repo.AiCountry.GetByCountryIdAsync(this.Character.CountryId);
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

        var main = this.towns.FirstOrDefault(t => t.Id == old.Data.MainTownId);
        var target = this.towns.FirstOrDefault(t => t.Id == old.Data.TargetTownId);
        var border = this.towns.FirstOrDefault(t => t.Id == old.Data.BorderTownId);
        var nextTarget = this.towns.FirstOrDefault(t => t.Id == old.Data.NextTargetTownId);

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
            if (capital.People < 4000)
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
        if (availableWars.Any() || readyWars.Any())
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
        if (availableWars.Any() || readyWars.Any())
        {
          if (this.data.BorderTown != null && this.data.NextBattleTown == null)
          {
            var arounds = this.towns.GetAroundTowns(this.data.BorderTown).Where(t => mainWars.Contains(t.CountryId));
            if (arounds.Any())
            {
              // 前線都市が、目標都市への最短でないにしても、敵国と隣接している
              var townData = new List<(TownBase Town, IEnumerable<Character> Defenders)>();
              foreach (var town in arounds)
              {
                townData.Add((
                  town,
                  (await repo.Town.GetDefendersAsync(town.Id)).Select(d => d.Character)
                ));
              }

              TownBase target;
              switch (targetOrder)
              {
                //case BattleTargetOrder.Defenders:
                default:
                  target = townData.OrderByDescending(d => d.Defenders.Count()).First().Town;
                  break;
                case BattleTargetOrder.GetTown:
                  target = townData.GroupBy(d => d.Defenders.Count()).First().OrderBy(d => d.Town.Wall).First().Town;
                  break;
                case BattleTargetOrder.BreakWall:
                  target = townData.GroupBy(d => d.Defenders.Count()).First().OrderByDescending(d => d.Town.Wall).First().Town;
                  break;
              }

              this.data.NextBattleTown = (Town)target;
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
              for (var i = 0; i < s.Length; i++)
              {
                if (s[i].CountryId != this.Country.Id && i > 0)
                {
                  this.data.BorderTown = s[i - 1];
                  this.data.NextBattleTown = s[i];
                  break;
                }
              }
            }
          }
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

    protected abstract Task ActionAsync(MainRepository repo);

    private IEnumerable<uint> GetWaringCountries()
    {
      var availableWars = wars
        .Where(w => w.IntStartGameDate <= this.GameDateTime.ToInt() && (w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.StopRequesting));
      if (availableWars.Any())
      {
        var targetCountryIds = availableWars.Select(w => w.InsistedCountryId == this.Country.Id ? w.RequestedCountryId : w.InsistedCountryId);
        return targetCountryIds;
      }
      return Enumerable.Empty<uint>();
    }

    private IEnumerable<uint> GetNearReadyForWarCountries()
    {
      var availableWars = wars
        .Where(w => w.IntStartGameDate <= this.GameDateTime.ToInt() + 12 && w.IntStartGameDate > this.GameDateTime.ToInt() && (w.Status == CountryWarStatus.InReady || w.Status == CountryWarStatus.StopRequesting));
      if (availableWars.Any())
      {
        var targetCountryIds = availableWars.Select(w => w.InsistedCountryId == this.Country.Id ? w.RequestedCountryId : w.InsistedCountryId);
        return targetCountryIds;
      }
      return Enumerable.Empty<uint>();
    }

    private IEnumerable<uint> GetReadyForWarCountries()
    {
      var availableWars = wars
        .Where(w => w.IntStartGameDate > this.GameDateTime.ToInt() && (w.Status == CountryWarStatus.InReady || w.Status == CountryWarStatus.StopRequesting));
      if (availableWars.Any())
      {
        var targetCountryIds = availableWars.Select(w => w.InsistedCountryId == this.Country.Id ? w.RequestedCountryId : w.InsistedCountryId);
        return targetCountryIds;
      }
      return Enumerable.Empty<uint>();
    }

    private bool IsNeedSoldiers()
    {
      return this.Character.SoldierNumber < 30 &&
          (this.GetWaringCountries().Any() ||
           this.GetNearReadyForWarCountries().Any());
    }

    private Town GetTownForSoldiers()
    {
      Func<TownBase, bool> predicate = t => t.CountryId == this.Character.CountryId && t.People >= this.Character.Leadership * 6 && t.Security > this.Character.Leadership / 9;
      var arounds = towns.GetAroundTowns(this.Town).ToList();
      arounds.Add(this.Town);

      if (this.CanSoldierForce && this.Town.CountryId == this.Character.CountryId)
      {
        return this.Town;
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
        return (Town)myTowns.First();
      }
      else
      {
        return this.towns.Where(t => t.CountryId == this.Character.CountryId).OrderByDescending(t => t.People).FirstOrDefault() ?? this.Town;
      }
    }

    private async Task<bool> GetSoldiersAsync(MainRepository repo, int num = -1)
    {
      if (this.IsNeedSoldiers())
      {
        var town = this.GetTownForSoldiers();

        var isMove = await this.MoveToTownOrNextToTownAsync(repo, this.Town, town, this.command);
        if (isMove)
        {
          return true;
        }

        this.command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 1,
          NumberValue = (int)this.FindSoldierType(),
        });
        this.command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 2,
          NumberValue = num <= 0 ? this.Character.Leadership : num,
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

    protected async Task<bool> InputBattleAsync(MainRepository repo)
    {
      Town targetTown = null;

      if (!this.GetWaringCountries().Any() && !this.GetNearReadyForWarCountries().Any())
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
        if (this.InputMoveToQuickestImportantTown())
        {
          return true;
        }

        if (await this.InputMoveToBorderTownAsync(repo))
        {
          return true;
        }
      }

      if (this.BorderTown != null)
      {
        if (this.data.BorderTown.Id != this.Town.Id)
        {
          var arounds = this.towns.GetAroundTowns(this.Town);
          if (this.data.NextBattleTown != null && arounds.Any(t => t.Id == this.data.NextBattleTown.Id))
          {
            targetTown = this.data.NextBattleTown;
          }
          else
          {
            if (await this.InputMoveToBorderTownAsync(repo))
            {
              return true;
            }
          }
        }
        else
        {
          targetTown = this.data.NextBattleTown;
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

      if (!this.GetWaringCountries().Any() && !this.GetNearReadyForWarCountries().Any())
      {
        return false;
      }

      if (await this.GetSoldiersAsync(repo))
      {
        return true;
      }

      if (this.Town.CountryId != this.Character.CountryId)
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

      return await RunAsync(this.Town);
    }

    protected async Task<bool> InputDefendLoopAsync(MainRepository repo, int minPeople)
    {
      if (this.Town.Id != this.data.MainTown.Id)
      {
        return false;
      }

      if (this.Town.People > minPeople)
      {
        return false;
      }

      if (await this.InputDefendAsync(repo, DefendLevel.NeedMyDefend))
      {
        return true;
      }

      var num = this.Town.People < 2000 ? 1 : this.Town.People < 5000 ? 5 : this.Town.People < 7000 ? 10 : this.Town.People / 700;
      if (await this.GetSoldiersAsync(repo, num))
      {
        return true;
      }

      return true;
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

    protected bool InputMoveToQuickestImportantTown()
    {
      if (this.Town.CountryId == this.Character.CountryId)
      {
        return false;
      }

      if (this.data.BorderTown != null && this.data.BorderTown.IsNextToTown(this.Town))
      {
        this.InputMoveToTown(this.data.BorderTown.Id, this.command);
        return true;
      }

      if (this.data.MainTown != null && this.data.MainTown.IsNextToTown(this.Town))
      {
        this.InputMoveToTown(this.data.MainTown.Id, this.command);
        return true;
      }

      return false;
    }

    protected async Task<bool> InputMoveToBorderTownAsync(MainRepository repo)
    {
      if (this.data.BorderTown == null)
      {
        return false;
      }
      return await this.MoveToTownOrNextToTownAsync(repo, this.Town, this.data.BorderTown, this.command);
    }

    protected async Task<bool> InputMoveToMainTownAsync(MainRepository repo)
    {
      return await this.MoveToTownOrNextToTownAsync(repo, this.Town, this.data.MainTown, this.command);
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
      if (command.Type == CharacterCommandType.TownBuilding && this.Town.TownBuildingValue >= Config.TownBuildingMax)
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

    protected bool InputSoldierTraining()
    {
      if (this.Character.Proficiency < 100 && this.Character.SoldierNumber > 0)
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
    }
  }
}
