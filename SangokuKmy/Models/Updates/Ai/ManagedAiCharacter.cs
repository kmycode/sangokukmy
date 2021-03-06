﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;

namespace SangokuKmy.Models.Updates.Ai
{
  public abstract class ManagedAiCharacter : WorkerAiCharacter
  {
    protected override AttackMode AttackType => this.CurrentAttackType;
    protected AttackMode CurrentAttackType { get; set; } = AttackMode.KillDefenders;

    protected override DefendSeiranLevel NeedDefendSeiranLevel => this._seiranLevel;
    private DefendSeiranLevel _seiranLevel = DefendSeiranLevel.NotCare;

    protected override ForceDefendPolicyLevel ForceDefendPolicy => this._forceDefendLevel;
    private ForceDefendPolicyLevel _forceDefendLevel = ForceDefendPolicyLevel.NotCare;

    protected override bool IsWarEmpty => true;

    protected virtual bool CanPolicyFirst => true;

    protected virtual bool CanJoinHumanCountry => false;

    protected bool IsPolicySecond { get; private set; }

    public ManagedAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      throw new NotImplementedException();
    }

    protected override async Task<bool> ActionAsNoCountryAsync(MainRepository repo)
    {
      // 仕官先の国候補
      var allCountries = (await repo.Country.GetAllAsync()).Where(c => !c.HasOverthrown);
      var countries = allCountries.Where(c => c.AiType == CountryAiType.Managed);
      if (!countries.Any())
      {
        if (this.CanJoinHumanCountry)
        {
          var allTowns = await repo.Town.GetAllAsync();
          var religions = allTowns.GroupBy(t => t.Religion).Select(t => new { Regilion = t.Key, Count = t.Count(), });
          if (religions.Any())
          {
            var religion = religions.OrderBy(r => r.Count).First().Regilion;
            countries = allCountries.Where(c => c.AiType == CountryAiType.Human && c.Religion == religion);

            if (!countries.Any())
            {
              religion = religions.OrderBy(r => r.Count).ElementAtOrDefault(1)?.Regilion ?? religion;
              countries = allCountries.Where(c => c.AiType == CountryAiType.Human && c.Religion == religion);
            }
          }
          else
          {
            countries = allCountries.Where(c => c.AiType == CountryAiType.Human);
          }
        }
        else
        {
          return false;
        }
      }

      // 一番武将の少ない国を選んで仕官する
      var characters = (await repo.Character.GetAllAliveAsync()).Where(c => countries.Any(cc => cc.Id == c.CountryId));
      Country targetCountry;
      if (!characters.Any())
      {
        targetCountry = countries.First();
      }
      else
      {
        var targetCountryId = characters.GroupBy(c => c.CountryId).OrderBy(c => c.Count()).First().Key;
        targetCountry = countries.FirstOrDefault(c => c.Id == targetCountryId);
        if (targetCountry == null)
        {
          return false;
        }
      }

      var towns = await repo.Town.GetAllAsync();
      var targetTowns = towns.Where(t => t.CountryId == targetCountry.Id);
      var currentTown = towns.FirstOrDefault(t => t.Id == this.Character.TownId);
      
      if (currentTown != null && currentTown.CountryId == targetCountry.Id)
      {
        this.InputJoinCountry();
      }
      else
      {
        this.InputMoveToCountryTown(targetCountry.Id);
      }

      return true;
    }

    protected override async Task BeforeActionAsync(MainRepository repo)
    {
      var management = await repo.AiCountry.GetManagementByCountryIdAsync(this.Country.Id);

      // 井闌使われたときの対応
      if (management.HasData)
      {
        var m = management.Data;
        this._seiranLevel = m.SeiranPolicy == AgainstSeiranPolicy.NotCare ? DefendSeiranLevel.NotCare :
                            m.SeiranPolicy == AgainstSeiranPolicy.NotCareMuch ? DefendSeiranLevel.HalfSeirans :
                            m.SeiranPolicy == AgainstSeiranPolicy.Gonorrhea ? DefendSeiranLevel.SeiransWithUnits :
                            DefendSeiranLevel.Seirans;
      }

      // 強制守備ループに入るときの兵糧攻め判定基準
      if (management.HasData)
      {
        var m = management.Data;
        if (m.ForceDefendPolicy == AiCountryForceDefendPolicy.NotCare)
        {
          this._forceDefendLevel = ForceDefendPolicyLevel.NotCare;
        }
        if (m.ForceDefendPolicy == AiCountryForceDefendPolicy.Negative)
        {
          this._forceDefendLevel = ForceDefendPolicyLevel.Negative;
        }
        if (m.ForceDefendPolicy == AiCountryForceDefendPolicy.Medium)
        {
          this._forceDefendLevel = ForceDefendPolicyLevel.Medium;
        }
        if (m.ForceDefendPolicy == AiCountryForceDefendPolicy.Aggressive)
        {
          this._forceDefendLevel = ForceDefendPolicyLevel.Aggressive;
        }
      }
    }

    protected override async Task ActionAsync(MainRepository repo)
    {
      var management = await repo.AiCountry.GetManagementByCountryIdAsync(this.Country.Id);

      var post = await repo.Country.GetPostsAsync(this.Country.Id);
      if (post.Any(p => p.CharacterId == this.Character.Id && (p.Type == CountryPostType.Monarch || p.Type == CountryPostType.Warrior)))
      {
        if (await this.ActionAsMonarchAsync(repo))
        {
          return;
        }
      }

      await this.ActionPersonalAsync(repo);
    }

    protected abstract Task ActionPersonalAsync(MainRepository repo);

    protected override async Task ActionAfterDefendLoopAsync(MainRepository repo)
    {
      if (this.GameDateTime.Month % 6 == 0 && this.Character.Money >= 50 && this.InputPolicy())
      {
        return;
      }

      if (await this.InputRiceAnywayAsync(repo, 50_0000, Config.RiceBuyMax / 2))
      {
        return;
      }

      await base.ActionAfterDefendLoopAsync(repo);
    }

    /*
    protected async Task<IEnumerable<uint>> GetWarTargetsAsync(MainRepository repo)
    {
      var wars = (await repo.CountryDiplomacies.GetAllWarsAsync())
        .Where(w => w.RequestedCountryId == this.Country.Id || w.InsistedCountryId == this.Country.Id)
        .Where(w => w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.InReady || w.Status == CountryWarStatus.StopRequesting)
        .Select(w => w.RequestedCountryId == this.Country.Id ? w.InsistedCountryId : w.RequestedCountryId);

      return wars.Distinct();
    }
    */

    protected async Task<bool> IsWarAsync(MainRepository repo)
    {
      var system = await repo.System.GetAsync();
      if (system.IsBattleRoyaleMode)
      {
        return true;
      }

      var wars = (await repo.CountryDiplomacies.GetAllWarsAsync())
        .Where(w => w.RequestedCountryId == this.Country.Id || w.InsistedCountryId == this.Country.Id)
        .Where(w => w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.InReady || w.Status == CountryWarStatus.StopRequesting)
        .Where(w => w.IntStartGameDate < this.GameDateTime.ToInt() + 12);

      return wars.Any();
    }

    private async Task<bool> ActionAsMonarchAsync(MainRepository repo)
    {
      var allTowns = await repo.Town.GetAllAsync();
      var towns = allTowns.Where(t => t.CountryId == this.Country.Id);
      var charas = (await repo.Character.GetAllAliveAsync()).Where(c => c.CountryId == this.Country.Id);
      var policies = (await repo.Country.GetPoliciesAsync(this.Country.Id)).Where(p => p.Status == CountryPolicyStatus.Available);
      var secretaryMax = CountryService.GetSecretaryMax(policies.Select(p => p.Type));
      var storategy = await repo.AiCountry.GetStorategyByCountryIdAsync(this.Country.Id);

      // 政務官の管理
      if (secretaryMax > 0 && this.Character.Money >= Config.SecretaryCost)
      {
        var secretaries = charas.Where(c => c.AiType.IsSecretary());
        var isWar = await this.IsWarAsync(repo);

        CharacterAiType requestedType;
        if (isWar)
        {
          requestedType = CharacterAiType.SecretaryPatroller;
        }
        else
        {
          if (storategy.HasData)
          {
            var mainTown = towns.FirstOrDefault(t => t.Id == storategy.Data.MainTownId);
            var borderTown = towns.FirstOrDefault(t => t.Id == storategy.Data.BorderTownId);
            if (mainTown != null &&
              (mainTown.Security < 75 || (mainTown.Security < 100 && mainTown.People < 30000)) &&
              !charas.Any(c => c.AiType == CharacterAiType.ManagedPatroller && c.TownId == mainTown.Id))
            {
              requestedType = CharacterAiType.SecretaryPatroller;
            }
            else if (borderTown != null &&
              (borderTown.Security < 75 || (borderTown.Security < 100 && borderTown.People < 20000)) &&
              !charas.Any(c => c.AiType == CharacterAiType.ManagedPatroller && c.TownId == borderTown.Id))
            {
              requestedType = CharacterAiType.SecretaryPatroller;
            }
            else
            {
              requestedType = CharacterAiType.SecretaryPioneer;
            }
          }
          else
          {
            var capital = towns.FirstOrDefault(t => t.Id == this.Country.CapitalTownId && t.CountryId == this.Country.Id);
            if (capital != null &&
              (capital.Security < 75 || (capital.Security < 100 && capital.People < 40000)) &&
              !charas.Any(c => c.AiType == CharacterAiType.ManagedPatroller && c.TownId == capital.Id))
            {
              requestedType = CharacterAiType.SecretaryPatroller;
            }
            else
            {
              requestedType = CharacterAiType.SecretaryPioneer;
            }
          }
        }

        if (!secretaries.All(s => s.AiType == requestedType))
        {
          this.InputRemoveSecretary(secretaries.First(s => s.AiType != requestedType).Id);
          return true;
        }
        if (CountryService.GetCurrentSecretaryPoint(secretaries.Select(s => s.AiType).Append(requestedType)) <= secretaryMax &&
          this.Country.LastMoneyIncomes > Config.SecretaryCost &&
          this.Country.LastRiceIncomes > Config.SecretaryCost)
        {
          this.InputAddSecretary(requestedType);
          return true;
        }

        foreach (var chara in secretaries)
        {
          var secretaryTown = await repo.Town.GetByIdAsync(chara.TownId);
          if (secretaryTown.HasData)
          {
            var town = secretaryTown.Data;

            if (chara.AiType == CharacterAiType.SecretaryPioneer)
            {
              if (town.CountryId != this.Country.Id || (town.Agriculture == town.AgricultureMax && town.Commercial == town.CommercialMax))
              {
                var nextTown = towns
                  .Where(t => t.Agriculture < t.AgricultureMax || t.Commercial < t.CommercialMax)
                  .OrderByDescending(t => t.AgricultureMax + t.CommercialMax - t.Agriculture - t.Commercial).FirstOrDefault();
                if (nextTown != null)
                {
                  this.InputMoveSecretary(chara.Id, nextTown.Id);
                  return true;
                }
              }
            }
            else if (chara.AiType == CharacterAiType.SecretaryPatroller)
            {
              if (isWar && charas.Any(c => c.AiType == CharacterAiType.ManagedPatroller))
              {
                if (storategy.HasData && chara.TownId != storategy.Data.MainTownId && towns.Any(t => t.Id == storategy.Data.MainTownId))
                {
                  this.InputMoveSecretary(chara.Id, storategy.Data.MainTownId);
                  return true;
                }
              }
              else if (town.Security == 100)
              {
                if (storategy.HasData)
                {
                  Town nextTown = null;
                  if (town.Id == storategy.Data.MainTownId)
                  {
                    nextTown = allTowns.FirstOrDefault(t => t.Id == storategy.Data.BorderTownId);
                  }
                  else if (town.Id == storategy.Data.BorderTownId)
                  {
                    nextTown = allTowns.FirstOrDefault(t => t.Id == storategy.Data.MainTownId);
                  }
                  else
                  {
                    nextTown = allTowns.FirstOrDefault(t => t.Id == storategy.Data.BorderTownId) ??
                               allTowns.FirstOrDefault(t => t.Id == storategy.Data.MainTownId);
                  }
                  if (nextTown != null && nextTown.Security < 100)
                  {
                    this.InputMoveSecretary(chara.Id, nextTown.Id);
                    return true;
                  }
                }
              }
            }
          }
        }
      }

      // 金庫
      if (this.Country.SafeMoney >= Config.PaySafeMax)
      {
        var chara = charas
          .Where(c => c.AiType.IsManaged() && !c.AiType.IsMoneyInflator())
          .Where(c => c.AiType.ToManagedStandard() != CharacterAiType.ManagedPatroller && c.AiType != CharacterAiType.ManagedEvangelist)
          .OrderBy(c => c.Money)
          .FirstOrDefault();
        if (chara != null)
        {
          this.InputSafeOut(chara.Id, Config.PaySafeMax);
          return true;
        }
      }

      return false;
    }
  }

  public class ManagedBattlerAiCharacter : ManagedAiCharacter
  {
    private SoldierType soldierTypeCache = SoldierType.Unknown;

    protected override AttackMode AttackType
    {
      get
      {
        if (base.AttackType == AttackMode.BreakWall)
        {
          if (!this.Character.SoldierType.IsForWall())
          {
            return AttackMode.GetTown;
          }
        }

        return base.AttackType;
      }
    }

    public ManagedBattlerAiCharacter(Character character) : base(character)
    {
    }

    protected override async Task<SoldierType> FindSoldierTypeAsync(MainRepository repo, Town nextBattleTown)
    {
      if (this.soldierTypeCache == SoldierType.Unknown)
      {
        var soldierTypeNumber = this.Character.Id + this.GameDateTime.Year / 14;

        if (this.Town.Technology >= 500 &&
          this.Character.Money > this.Character.Leadership * 300 + 5_0000 &&
          (this.AttackType == AttackMode.BreakWall ||
           this.AttackType == AttackMode.GetTown ||
           (nextBattleTown != null &&
            nextBattleTown.Wall > 200 &&
            MlService.Predict(await repo.AiActionHistory.GetAsync(this.Country.Id, this.Town.Id, this.Town.CountryId), new AiBattleHistory
            {
              CharacterId = this.Character.Id,
              CountryId = this.Character.CountryId,
              IntGameDateTime = this.GameDateTime.ToInt() + 1,
              TownId = this.NextBattleTown.Id,
              TownCountryId = this.NextBattleTown.CountryId,
            }) == AiBattleTargetType.Wall)))
        {
          this.soldierTypeCache = SoldierType.StoneSlingshot;
        }
        else if (this.Town.Technology >= 1000 && this.Character.Money > this.Character.Leadership * 240 + 45_0000 && this.GameDateTime.Year >= 108)
        {
          if (soldierTypeNumber % 3 == 0)
          {
            this.soldierTypeCache = SoldierType.RepeatingCrossbow;
          }
          else if (soldierTypeNumber % 3 == 1)
          {
            this.soldierTypeCache = SoldierType.Chariot;
          }
          else
          {
            this.soldierTypeCache = SoldierType.Seishu;
          }
        }
        else if (this.Town.Technology >= 800 && this.Character.Money > this.Character.Leadership * 200 + 22_0000)
        {
          if (soldierTypeNumber % 3 == 0)
          {
            this.soldierTypeCache = SoldierType.StrongCrossbow;
          }
          else if (soldierTypeNumber % 3 == 1)
          {
            this.soldierTypeCache = SoldierType.HeavyCavalry;
          }
          else
          {
            this.soldierTypeCache = SoldierType.HeavyInfantry;
          }
        }
        else if (this.Town.Technology >= 500 && this.Character.Money > this.Character.Leadership * 160 + 12_0000)
        {
          if (soldierTypeNumber % 3 == 0)
          {
            this.soldierTypeCache = SoldierType.LongArcher;
          }
          else if (soldierTypeNumber % 3 == 1)
          {
            this.soldierTypeCache = SoldierType.LightCavalry;
          }
          else
          {
            this.soldierTypeCache = SoldierType.Infantry;
          }
        }
        else if (this.Town.Technology >= 200 && this.Character.Money > this.Character.Leadership * 160 + 12_0000)
        {
          if (soldierTypeNumber == 0)
          {
            this.soldierTypeCache = SoldierType.Archer;
          }
          else if (soldierTypeNumber == 1)
          {
            this.soldierTypeCache = SoldierType.Cow;
          }
          else
          {
            this.soldierTypeCache = SoldierType.LightInfantry;
          }
        }
        else
        {
          this.soldierTypeCache = SoldierType.Common;
        }
      }

      return this.soldierTypeCache;
    }

    protected override DefendLevel NeedDefendLevel
    {
      get
      {
        if ((this.Town.People > 20000 && this.Town.Security > 50) || this.Town.Wall < 600)
        {
          return DefendLevel.NeedMyDefend;
        }
        if ((this.Town.People > 10000 && this.Town.Security > 50) || this.Town.Wall < 1000)
        {
          return DefendLevel.NeedThreeDefend;
        }
        if ((this.BorderTown != null && this.Town.Id == this.BorderTown.Id) || (this.Town.People > 5000 && this.Town.Security > 30) || this.Town.Wall < 1400)
        {
          return DefendLevel.NeedTwoDefend;
        }
        return DefendLevel.NeedAnyDefends;
      }
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "武将";
      this.Character.Strong = 100;
      this.Character.Leadership = 100;
      this.Character.Money = 12_0000;
      this.Character.Rice = 6_0000;
    }

    protected override async Task ActionPersonalAsync(MainRepository repo)
    {
      if (await this.InputUseItemAsync(repo))
      {
        return;
      }

      if (await this.InputFirstSecurityAsync(repo))
      {
        return;
      }

      if (await this.InputCountryForceDefendLoopAsync(repo))
      {
        return;
      }

      if (await this.InputDefendLoopAsync(repo, 5000, 5_0000))
      {
        return;
      }

      if (await this.InputDefendAsync(repo))
      {
        return;
      }

      if (await this.InputBattleAsync(repo))
      {
        return;
      }

      if (await this.InputMoveToBorderTownAsync(repo))
      {
        return;
      }

      if (this.InputMoveToCountryTown(this.Country.Id))
      {
        return;
      }

      if (this.InputSoldierTraining())
      {
        return;
      }

      if (await this.InputSellRiceForReadyWarAsync(repo))
      {
        return;
      }

      if (await this.InputRiceAsync(repo, 10_0000))
      {
        return;
      }

      if (this.InputSoldierTrainingIfNoMoney(100))
      {
        return;
      }

      if ((this.GameDateTime.Month % 6 == 1 || this.Character.Class < Config.NextLank * 8) && this.InputPolicy())
      {
        return;
      }
      else if (this.GameDateTime.Year % 6 == 0 && this.InputPolicy())
      {
        return;
      }
      else
      {
        if (this.IsPolicySecond && this.InputPolicy())
        {
          return;
        }
        if (this.GameDateTime.Year % 3 == 1 && await this.InputRiceAsync(repo, 40_0000))
        {
          return;
        }
        if (this.GameDateTime.Month % 6 == 0)
        {
          if (this.InputPolicy() || this.InputTownBuilding())
          {
            return;
          }
          this.InputSoldierTrainingForce();
        }
        if (this.Character.Leadership < this.Character.Strong * 0.666f)
        {
          this.InputTraining(TrainingType.Leadership);
        }
        else
        {
          this.InputTraining(TrainingType.Strong);
        }
      }
    }
  }

  public class ManagedWallBattlerAiCharacter : ManagedBattlerAiCharacter
  {
    protected override async Task<SoldierType> FindSoldierTypeAsync(MainRepository repo, Town willTown)
    {
      if (this.Town.Technology >= 500 &&
        this.Character.Money > this.Character.Leadership * 300 + 1_5000)
      {
        var prediction = MlService.Predict(await repo.AiActionHistory.GetAsync(this.Country.Id, willTown.Id, willTown.CountryId), new AiBattleHistory
        {
          IntGameDateTime = this.GameDateTime.ToInt() + 1,
          CharacterId = this.Character.Id,
          CountryId = this.Character.CountryId,
          TownId = willTown.Id,
          TownCountryId = willTown.CountryId,
        });
        if (prediction == AiBattleTargetType.Wall)
        {
          return SoldierType.Seiran;
        }
        if (prediction != AiBattleTargetType.Unknown)
        {
          return await base.FindSoldierTypeAsync(repo, willTown);
        }

        var defenders = await repo.Town.GetDefendersAsync(willTown.Id);
        if (defenders.Any(d => d.Character.SoldierNumber > 9))
        {
          return await base.FindSoldierTypeAsync(repo, willTown);
        }

        return SoldierType.Seiran;
      }

      return await base.FindSoldierTypeAsync(repo, willTown);
    }

    public ManagedWallBattlerAiCharacter(Character character) : base(character)
    {
    }
  }

  public class ManagedWallBreakerAiCharacter : ManagedWallBattlerAiCharacter
  {
    protected override DefendLevel NeedDefendLevel => DefendLevel.NotCare;

    protected override AttackMode AttackType => this.Character.SoldierType.IsForWall() ? AttackMode.BreakWall : base.AttackType;

    public ManagedWallBreakerAiCharacter(Character character) : base(character)
    {
    }
  }

  public class ManagedShortstopBattlerAiCharacter : ManagedWallBattlerAiCharacter
  {
    protected override DefendLevel NeedDefendLevel => DefendLevel.NotCare;

    protected override AttackMode AttackType => AttackMode.GetTown;

    public ManagedShortstopBattlerAiCharacter(Character character) : base(character)
    {
    }
  }

  public class ManagedCivilOfficialAiCharacter : ManagedAiCharacter
  {
    protected override DefendSeiranLevel NeedDefendSeiranLevel => DefendSeiranLevel.Seirans;

    protected override int RequestedTechnologyForSoldier => 500;

    protected override bool CanPolicyFirst =>
      (this.Country.LastMoneyIncomes >= 10000 && this.Country.LastRiceIncomes >= 10000) ? base.CanPolicyFirst : false;

    protected override AttackMode AttackType
    {
      get
      {
        if (base.AttackType == AttackMode.BreakWall)
        {
          return AttackMode.GetTown;
        }

        return base.AttackType;
      }
    }

    public ManagedCivilOfficialAiCharacter(Character character) : base(character)
    {
    }

    protected override SoldierType FindSoldierType()
    {
      if (this.Town.Technology >= 500 && this.Character.Money > this.Character.Leadership * 100 + 12_0000)
      {
        if (this.Character.Id % 3 == 0)
        {
          return SoldierType.IntellectArcher;
        }
        else if (this.Character.Id % 3 == 1)
        {
          return SoldierType.IntellectHeavyCavalry;
        }
        else
        {
          return SoldierType.LightIntellect;
        }
      }

      return SoldierType.IntellectCommon;
    }

    protected override int GetSoldierNumberMax(SoldierType type)
    {
      if (type == SoldierType.Common)
      {
        return 1;
      }
      else
      {
        return base.GetSoldierNumberMax(type);
      }
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "文官";
      this.Character.Intellect = 100;
      this.Character.Leadership = 100;
      this.Character.Money = 12_0000;
      this.Character.Rice = 6_0000;
    }

    protected override async Task ActionPersonalAsync(MainRepository repo)
    {
      if (await this.InputUseItemAsync(repo))
      {
        return;
      }

      if (await this.InputFirstSecurityAsync(repo))
      {
        return;
      }

      if (await this.InputCountryForceDefendLoopAsync(repo))
      {
        return;
      }

      if (await this.InputDefendLoopAsync(repo, 8000, 6_0000))
      {
        return;
      }

      if (await this.InputDefendAsync(repo))
      {
        return;
      }

      if (this.InputDevelopInWar())
      {
        return;
      }

      if (await this.InputBattleAsync(repo))
      {
        return;
      }

      if (await this.InputMoveToBorderTownInWarAsync(repo))
      {
        return;
      }

      if (await this.InputMoveToDevelopTownAsync(repo))
      {
        return;
      }

      if (this.InputMoveToCountryTown(this.Country.Id))
      {
        return;
      }

      if (this.InputSoldierTraining())
      {
        return;
      }

      if (await this.InputSellRiceForReadyWarAsync(repo))
      {
        return;
      }

      if (await this.InputRiceAsync(repo, 20_0000))
      {
        return;
      }

      if (this.InputSoldierTrainingIfNoMoney(100))
      {
        return;
      }

      if (await this.InputBuildSubBuildingAsync(repo))
      {
        return;
      }

      if (this.InputDevelop())
      {
        return;
      }

      if ((this.GameDateTime.Month % 6 == 1 || this.Character.Class < Config.NextLank * 8) && this.InputPolicy())
      {
        return;
      }
      else
      {
        if (this.IsPolicySecond && this.InputPolicy())
        {
          return;
        }
        if (this.GameDateTime.Year % 3 != 1 && await this.InputRiceAsync(repo, 60_0000))
        {
          return;
        }
        if (this.GameDateTime.Month % 6 == 0)
        {
          if (this.InputPolicy() || this.InputTownBuilding())
          {
            return;
          }
          this.InputSoldierTrainingForce();
        }
        if (this.Character.Leadership < this.Character.Intellect * 0.666f)
        {
          this.InputTraining(TrainingType.Leadership);
        }
        else
        {
          this.InputTraining(TrainingType.Intellect);
        }
      }
    }
  }

  public class ManagedShortstopCivilOfficialAiCharacter : ManagedCivilOfficialAiCharacter
  {
    protected override DefendLevel NeedDefendLevel => DefendLevel.NotCare;

    protected override AttackMode AttackType => AttackMode.GetTown;

    public ManagedShortstopCivilOfficialAiCharacter(Character character) : base(character)
    {
    }
  }

  public class ManagedPatrollerAiCharacter : ManagedAiCharacter
  {
    protected override AttackMode AttackType
    {
      get
      {
        if (base.AttackType == AttackMode.BreakWall)
        {
          return AttackMode.GetTown;
        }

        return base.AttackType;
      }
    }

    protected override int GetSoldierNumberMax(SoldierType type)
    {
      return 1;
    }

    public ManagedPatrollerAiCharacter(Character character) : base(character)
    {
    }

    protected override SoldierType FindSoldierType()
    {
      return SoldierType.PopularityHalberd;
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "仁官";
      this.Character.Intellect = 90;
      this.Character.Popularity = 100;
      this.Character.Leadership = 10;
      this.Character.Money = 12_0000;
      this.Character.Rice = 6_0000;
    }

    protected override async Task ActionPersonalAsync(MainRepository repo)
    {
      if (await this.InputUseItemAsync(repo))
      {
        return;
      }

      if (await this.InputCountryForceDefendLoopAsync(repo))
      {
        return;
      }

      if (await this.InputMoveToBorderTownInWarAsync(repo))
      {
        return;
      }

      if (await this.InputMoveToBorderTownInNotWarAndSecurityNotMaxAsync(repo))
      {
        return;
      }

      if (this.InputMoveToCountryTown(this.Country.Id))
      {
        return;
      }

      if (this.InputSecurity())
      {
        return;
      }

      if (this.InputSecurityInWar())
      {
        return;
      }

      if (await this.InputDefendLoopAsync(repo, 20000))
      {
        return;
      }

      if (await this.InputDefendAsync(repo, DefendLevel.NeedMyDefend))
      {
        return;
      }

      if (await this.InputBuyRiceAsync(repo, 0.85f))
      {
        return;
      }

      if (await this.InputSafeInAsync(repo, Config.PaySafeMax, 10_0000))
      {
        return;
      }

      if (this.InputSoldierTraining())
      {
        return;
      }

      if (this.InputSoldierTrainingIfNoMoney(100))
      {
        return;
      }

      if (await this.InputMoveToMainTownAsync(repo))
      {
        return;
      }

      if (this.GameDateTime.Month % 6 == 0)
      {
        this.InputSecurityForce();
        return;
      }

      this.InputTraining(TrainingType.Popularity);
    }
  }

  public class ManagedMoneyInflaterAiCharacter : ManagedAiCharacter
  {
    public ManagedMoneyInflaterAiCharacter(Character character) : base(character)
    {
    }

    protected override async Task ActionPersonalAsync(MainRepository repo)
    {
      if (await this.InputUseItemAsync(repo))
      {
        return;
      }

      if (await this.InputExpandRicePriceRangeAsync(repo))
      {
        return;
      }

      if (await this.InputRiceAnywayAsync(repo, int.MaxValue, Config.RiceBuyMax))
      {
        return;
      }

      this.MoveToRandomTown();
    }
  }

  public class ManagedEvangelistAiCharacter : ManagedAiCharacter
  {
    protected override bool CanJoinHumanCountry => true;

    public ManagedEvangelistAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "伝道師";
      this.Character.Intellect = 100;
      this.Character.Leadership = 100;
      this.Character.Money = 120_0000;
      this.Character.Rice = 60_0000;
    }

    protected override async Task ActionPersonalAsync(MainRepository repo)
    {
      if (await this.InputUseItemAsync(repo))
      {
        return;
      }

      if (await this.InputMissionaryAsync(repo))
      {
        return;
      }

      this.MoveToRandomTown();
    }
  }
}
