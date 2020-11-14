using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;

namespace SangokuKmy.Models.Updates.Ai
{
  public class ManagedAiCountry : AiCountry
  {
    private IReadOnlyList<Character> allCharacters;
    private IReadOnlyList<Town> allTowns;

    private IEnumerable<CountryPolicyType> PolicyTypes
    {
      get
      {
        List<CountryPolicyType> primary = null;
        if (this.Management.PolicyTarget == AiCountryPolicyTarget.WallDefend)
        {
          primary = new List<CountryPolicyType>
          {
            CountryPolicyType.HumanDevelopment,
            CountryPolicyType.AntiGang,
            CountryPolicyType.Economy,
            CountryPolicyType.SaveWall,
          };
        }
        else if (this.Management.PolicyTarget == AiCountryPolicyTarget.Money)
        {
          primary = new List<CountryPolicyType>
          {
            CountryPolicyType.HumanDevelopment,
            CountryPolicyType.Economy,
            CountryPolicyType.Storage,
            CountryPolicyType.Collection,
            CountryPolicyType.AddSalary,
            CountryPolicyType.UndergroundStorage,
            CountryPolicyType.WallEar,
            CountryPolicyType.StomachStorage,
            CountryPolicyType.Shoji,
          };
        }
        else if (this.Management.PolicyTarget == AiCountryPolicyTarget.WallAttack)
        {
          primary = new List<CountryPolicyType>
          {
            CountryPolicyType.HumanDevelopment,
            CountryPolicyType.IntellectCountry,
            CountryPolicyType.StrongCountry,
            CountryPolicyType.AntiGang,
            CountryPolicyType.KillGang,
            CountryPolicyType.Justice,
            CountryPolicyType.Siege,
            CountryPolicyType.JusticeMessage,
            CountryPolicyType.Shosha,
          };
        }

        var normal = new List<CountryPolicyType>
        {
          CountryPolicyType.HumanDevelopment,
          CountryPolicyType.IntellectCountry,
          CountryPolicyType.StrongCountry,
          CountryPolicyType.AntiGang,
          CountryPolicyType.KillGang,
          CountryPolicyType.Justice,
          CountryPolicyType.Siege,
          CountryPolicyType.JusticeMessage,
          CountryPolicyType.Shosha,
          CountryPolicyType.Economy,
          CountryPolicyType.Storage,
          CountryPolicyType.Collection,
          CountryPolicyType.AddSalary,
          CountryPolicyType.UndergroundStorage,
          CountryPolicyType.WallEar,
          CountryPolicyType.StomachStorage,
          CountryPolicyType.Shoji,
          CountryPolicyType.SaveWall,
          CountryPolicyType.HelpRepair,
        };

        if (primary != null)
        {
          return primary
            .Concat(normal)
            .Distinct();
        }
        if (!this.allCharacters.Any(c => c.CountryId == this.Country.Id && c.GetCharacterType() == CharacterType.Popularity))
        {
          normal.Remove(CountryPolicyType.PopularityCountry);
        }

        return normal;
      }
    }

    private IEnumerable<CountryPolicyType> RequestedPolicyTypesForWar
    {
      get
      {
        var primary = new List<CountryPolicyType>();
        if (this.Management.PolicyTarget == AiCountryPolicyTarget.WallDefend)
        {
          primary.Add(CountryPolicyType.SaveWall);
        }
        else if (this.Management.PolicyTarget == AiCountryPolicyTarget.Money)
        {
          primary.Add(CountryPolicyType.Shoji);
        }
        else if (this.Management.PolicyTarget == AiCountryPolicyTarget.WallAttack)
        {
          primary.Add(CountryPolicyType.Shosha);
        }

        return primary;
      }
    }

    private IEnumerable<CountryPolicyType> RequestedPolicyTypesFirst
    {
      get
      {
        var primary = new List<CountryPolicyType>();
        if (this.Management.PolicyTarget == AiCountryPolicyTarget.WallDefend)
        {
          if (this.Management.CharacterSize == AiCountryCharacterSize.Small)
          {
            primary.Add(CountryPolicyType.SaveWall);
          }
          else if (this.Management.CharacterSize == AiCountryCharacterSize.Medium)
          {
            primary.Add(CountryPolicyType.SaveWall);
          }
          else if (this.Management.CharacterSize == AiCountryCharacterSize.Large)
          {
            primary.Add(CountryPolicyType.SaveWall);
          }
        }
        else if (this.Management.PolicyTarget == AiCountryPolicyTarget.Money)
        {
          if (this.Management.CharacterSize == AiCountryCharacterSize.Small)
          {
            primary.Add(CountryPolicyType.AddSalary);
          }
          else if (this.Management.CharacterSize == AiCountryCharacterSize.Medium)
          {
            primary.Add(CountryPolicyType.Collection);
          }
          else if (this.Management.CharacterSize == AiCountryCharacterSize.Large)
          {
            primary.Add(CountryPolicyType.WallEar);
          }
        }
        else if (this.Management.PolicyTarget == AiCountryPolicyTarget.WallAttack)
        {
          if (this.Management.CharacterSize == AiCountryCharacterSize.Small)
          {
            primary.Add(CountryPolicyType.AttackDefend);
          }
          else if (this.Management.CharacterSize == AiCountryCharacterSize.Medium)
          {
            primary.Add(CountryPolicyType.Siege);
          }
          else if (this.Management.CharacterSize == AiCountryCharacterSize.Large)
          {
            primary.Add(CountryPolicyType.Shosha);
          }
        }

        return primary;
      }
    }

    private IEnumerable<CountryPolicyType> RequestedPolicyTypesUntilWar
    {
      get
      {
        return new List<CountryPolicyType>
        {
          CountryPolicyType.HumanDevelopment,
        };
      }
    }

    private IEnumerable<CountryPolicyType> RequestedPolicyTypesUntilWarFirst
    {
      get
      {
        return new List<CountryPolicyType>
        {
          CountryPolicyType.Shosha,
        };
      }
    }

    public ManagedAiCountry(Country country) : base(country)
    {
    }

    protected override async Task RunInnerAsync(MainRepository repo)
    {
      await AiService.CheckManagedReinforcementsAsync(repo, this.Country.Id);
      await repo.SaveChangesAsync();

      var system = await repo.System.GetAsync();

      this.allTowns = await repo.Town.GetAllAsync();
      var towns = this.allTowns.Where(t => t.CountryId == this.Country.Id);
      this.allCharacters = await repo.Character.GetAllAliveAsync();
      var characters = this.allCharacters.Where(c => c.CountryId == this.Country.Id);
      var policies = await repo.Country.GetPoliciesAsync(this.Country.Id);
      var allWars = await repo.CountryDiplomacies.GetAllWarsAsync();
      var wars = allWars
        .Where(w => w.InsistedCountryId == this.Country.Id || w.RequestedCountryId == this.Country.Id)
        .Where(w => w.Status == CountryWarStatus.InReady || w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.StopRequesting);

      var charaCount = characters.Count(c => c.AiType.IsManaged());
      this.Management.CharacterSize = charaCount <= Config.CountryJoinMaxOnLimited / 2 + 1 ? AiCountryCharacterSize.Small :
        charaCount <= Config.CountryJoinMaxOnLimited ? AiCountryCharacterSize.Medium : AiCountryCharacterSize.Large;

      if (!wars.Any() && !system.IsBattleRoyaleMode)
      {
        this.ResetCharacterAiTypes(characters.Where(c => c.AiType.IsManaged() && !c.AiType.IsMoneyInflator()));

        var isWar = false;
        if (await this.FindVirtualEnemyCountryAsync(repo, this.allTowns, this.allCharacters))
        {
          if (this.Country.AiType != CountryAiType.Puppet)
          {
            isWar = await this.SetWarAsync(repo, this.allTowns, this.allCharacters);
          }
        }

        if (!isWar)
        {
          await this.ChangeSomeInNotWarAsync(repo, characters, this.allTowns, wars);
        }
        await this.ChangeSomeInNotWarOrInReadyAsync(repo, characters, this.allTowns, wars);
      }
      else
      {
        if (!wars.Any(w => w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.StopRequesting) &&
          wars.Any(w => w.Status == CountryWarStatus.InReady))
        {
          if (wars.Where(w => w.Status == CountryWarStatus.InReady).All(w => w.IntStartGameDate > this.Game.IntGameDateTime + 24))
          {
            await this.ChangeSomeInNotWarOrInReadyAsync(repo, characters, this.allTowns, wars);
          }
          else
          {
            this.Management.TownWarTargetTownId = 0;
          }
        }
        await this.ChangeSomeInWarAsync(repo, characters, this.allTowns, wars);
      }

      var requestPolicies = !wars.Any() ? this.PolicyTypes : this.PolicyTypes.Concat(this.RequestedPolicyTypesUntilWar).Distinct();
      await this.GetPolicyAsync(repo, policies, requestPolicies);

      this.Management.IsPolicyFirst = !wars.Any() && this.Management.TownWarTargetTownId != 0 && allTowns.GetAroundCountries(allTowns.Where(t => t.CountryId == this.Country.Id)).Any(c => c == 1) && !this.IsReadyForFirst(policies);
      this.Management.IsPolicySecond = !wars.Any() ? !this.IsReadyForWar(policies) : this.HasPolicies(policies, this.RequestedPolicyTypesUntilWarFirst);
    }

    private async Task<bool> FindVirtualEnemyCountryAsync(MainRepository repo, IEnumerable<Town> allTowns, IEnumerable<Character> allCharacters)
    {
      float GetCountrySize(uint countryId)
      {
        return allCharacters.Count(c => c.CountryId == countryId) +
          allTowns.Count(t => t.CountryId == countryId) * 0.3f;
      }

      var old = this.Management.VirtualEnemyCountryId;
      if (old != 0 &&
        (await repo.Country.GetAliveByIdAsync(old)).HasData &&
        (this.Management.WarTargetPolicy == AiCountryWarTargetPolicy.Random || (this.Game.GameDateTime.Year % 2 != 0 || this.Game.GameDateTime.Month != 1)))
      {
        return true;
      }

      this.Management.VirtualEnemyCountryId = 0;
      var allCountries = (await repo.Country.GetAllAsync()).Where(c => !c.HasOverthrown);

      var aroundCountries = allTowns
        .GetAroundCountries(allTowns.Where(t => t.CountryId == this.Country.Id))
        .OrderBy(c => GetCountrySize(c))
        .Where(c => allCountries.Any(cc => cc.Id == c && cc.AiType != CountryAiType.Terrorists))
        .ToArray();
      if (!aroundCountries.Any(c => c != this.Country.Id))
      {
        return false;
      }

      var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
      if (wars.Any(w => w.IsJoinAvailable(this.Country.Id)))
      {
        return false;
      }

      var aroundWars = wars.Where(w => !w.IsJoinAvailable(this.Country.Id) && aroundCountries.Any(c => w.IsJoinAvailable(c)));
      var aroundCountriesWithWars = aroundCountries.Concat(
        aroundWars.SelectMany(w => new uint[] { w.RequestedCountryId, w.InsistedCountryId, }))
        .Where(c => c != this.Country.Id)
        .Distinct()
        .OrderBy(c => GetCountrySize(c))
        .ToArray();

      if (this.Management.WarTargetPolicy == AiCountryWarTargetPolicy.Random)
      {
        this.Management.VirtualEnemyCountryId = RandomService.Next(aroundCountries);
        return true;
      }
      if (this.Management.WarTargetPolicy == AiCountryWarTargetPolicy.Weakest)
      {
        this.Management.VirtualEnemyCountryId = aroundCountries.First();
        return true;
      }
      if (this.Management.WarTargetPolicy == AiCountryWarTargetPolicy.EqualityWeaker ||
          this.Management.WarTargetPolicy == AiCountryWarTargetPolicy.EqualityStronger)
      {
        var mySize = GetCountrySize(this.Country.Id);
        if (aroundCountriesWithWars.Count() == 1)
        {
          this.Management.VirtualEnemyCountryId = aroundCountriesWithWars.First();
        }
        else
        {
          if (this.Management.WarTargetPolicy == AiCountryWarTargetPolicy.EqualityWeaker)
          {
            if (GetCountrySize(aroundCountriesWithWars.First()) < mySize)
            {
              this.Management.VirtualEnemyCountryId = aroundCountriesWithWars.Last(c => GetCountrySize(c) < mySize);
            }
            else
            {
              this.Management.VirtualEnemyCountryId = aroundCountriesWithWars.First();
            }
            return true;
          }
          if (this.Management.WarTargetPolicy == AiCountryWarTargetPolicy.EqualityStronger)
          {
            if (GetCountrySize(aroundCountriesWithWars.Last()) > mySize)
            {
              this.Management.VirtualEnemyCountryId = aroundCountriesWithWars.First(c => GetCountrySize(c) > mySize);
            }
            else
            {
              this.Management.VirtualEnemyCountryId = aroundCountriesWithWars.Last();
            }
            return true;
          }
        }
      }

      return false;
    }

    private async Task<bool> SetWarAsync(MainRepository repo, IEnumerable<Town> allTowns, IEnumerable<Character> allCharacters)
    {
      var system = await repo.System.GetAsync();
      var policies = await repo.Country.GetPoliciesAsync(this.Country.Id);
      var towns = allTowns.Where(t => t.CountryId == this.Country.Id);
      var characters = allCharacters.Where(c => c.CountryId == this.Country.Id && !c.AiType.IsSecretary() && c.AiType != CharacterAiType.ManagedEvangelist);
      var townsInReady = towns.Where(t => this.IsReadyForWar(t));
      var charactersInReady = characters.Where(c => this.IsReadyForWar(c));
      var characterGroupInReady = charactersInReady.GroupBy(c => c.GetCharacterType());
      var storageOptional = await repo.AiCountry.GetStorategyByCountryIdAsync(this.Country.Id);

      if (system.RuleSet == GameRuleSet.BattleRoyale)
      {
        return false;
      }

      if (!characterGroupInReady.Any(g => g.Key == CharacterType.Strong || g.Key == CharacterType.Intellect))
      {
        return false;
      }

      if (!this.IsReadyForWar(policies))
      {
        return false;
      }
      
      if (this.Management.WarPolicy == AiCountryWarPolicy.GoodFight)
      {
        if (!charactersInReady.Any() || !townsInReady.Any())
        {
          return false;
        }
      }
      if (this.Management.WarPolicy == AiCountryWarPolicy.Balance)
      {
        if ((float)charactersInReady.Count() / characters.Count() < 0.4f || (float)townsInReady.Count() / towns.Count() < 0.4f)
        {
          return false;
        }
      }
      if (this.Management.WarPolicy == AiCountryWarPolicy.Carefully)
      {
        if ((float)charactersInReady.Count() / characters.Count() < 1.0f || (float)townsInReady.Count() / towns.Count() < 0.8f)
        {
          return false;
        }
      }

      if (storageOptional.HasData &&
        ((storageOptional.Data.BorderTownId != 0 && !townsInReady.Any(t => t.Id == storageOptional.Data.BorderTownId)) ||
         (storageOptional.Data.MainTownId != 0 && !townsInReady.Any(t => t.Id == storageOptional.Data.MainTownId))))
      {
        return false;
      }

      Country target = null;
      if (this.Management.VirtualEnemyCountryId != 0)
      {
        var t = await repo.Country.GetAliveByIdAsync(this.Management.VirtualEnemyCountryId);
        target = t.Data;
      }
      else
      {
        return false;
      }

      var isCreated = false;
      var startMonth = AiService.GetWarStartDateTime(this.Game.GameDateTime, this.Management.WarStartDatePolicy);
      var nextStartMonth = AiService.GetWarStartDateTime(this.Game.GameDateTime.NextMonth(), this.Management.WarStartDatePolicy);
      if (startMonth.ToInt() != nextStartMonth.ToInt())
      {
        // isCreated = await AiService.CreateWarIfNotWarAsync(repo, this.Country, target, startMonth);
        isCreated = await AiService.CreateWarAsync(repo, this.Country, target, startMonth);
        if (isCreated)
        {
          this.Management.VirtualEnemyCountryId = 0;
        }
      }
      return isCreated;
    }
    
    private async Task<bool> SetTownWarAsync(MainRepository repo, IEnumerable<Town> allTowns, IEnumerable<Character> allCharacters)
    {
      if (this.Management.TownWarPolicy == AiCountryTownWarPolicy.NotCare)
      {
        return false;
      }

      var townWars = await repo.CountryDiplomacies.GetAllTownWarsAsync();
      var lastTownWar = townWars.OrderByDescending(w => w.IntGameDate).FirstOrDefault(w => w.RequestedCountryId == this.Country.Id);
      if (lastTownWar != null && lastTownWar.Status == TownWarStatus.Terminated && lastTownWar.IntGameDate > this.Game.IntGameDateTime - 114)
      {
        this.Management.TownWarTargetTownId = 0;
        return false;
      }
      if (lastTownWar != null && lastTownWar.Status == TownWarStatus.Available)
      {
        return false;
      }

      var charas = allCharacters.Where(c =>
        c.CountryId == this.Country.Id &&
        c.AiType.IsManaged() &&
        !c.AiType.IsMoneyInflator() &&
        c.AiType.CanBattle());
      var readyCharas = charas.Where(c => this.IsReadyForWar(c) && c.AiType.ToManagedStandard() == CharacterAiType.ManagedBattler);
      if (readyCharas.Count() < charas.Count() / 2)
      {
        this.Management.TownWarTargetTownId = 0;
        return false;
      }

      var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
      if (wars.Any(w =>
        w.IsJoinAvailable(this.Country.Id) &&
        (w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.StopRequesting || (lastTownWar != null && w.IntStartGameDate - 6 < lastTownWar.IntGameDate + 120))))
      {
        this.Management.TownWarTargetTownId = 0;
        return false;
      }

      var countries = await repo.Country.GetAllAsync();
      var targetTowns = allTowns
        .GroupBy(t => t.CountryId)
        .Where(g => g.Key != this.Country.Id && g.Count() >= 2)
        .Join(
          countries.Where(c => !wars.Any(w => !w.IsJoinAvailable(this.Country.Id) && w.IsJoinAvailable(c.Id))),
          g => g.Key,
          c => c.Id,
          (g, c) => new { Country = c, Towns = g.Where(t => t.Id != c.CapitalTownId && allTowns.GetAroundTowns(t).Any(tt => tt.CountryId == this.Country.Id)), })
        .SelectMany(c => c.Towns);

      if (this.Management.TownWarTargetTownId != 0)
      {
        var town = allTowns.FirstOrDefault(t => t.Id == this.Management.TownWarTargetTownId);
        if (town != null && targetTowns.Any(t => t.Id == town.Id))
        {
          if (town.CountryId == this.Country.Id || !allTowns.GetAroundTowns(town).Any(t => t.CountryId == this.Country.Id))
          {
            this.Management.TownWarTargetTownId = 0;
          }
        }
        else
        {
          this.Management.TownWarTargetTownId = 0;
        }
      }

      if (this.Management.TownWarTargetTownId == 0 && targetTowns.Any())
      {
        if (this.Management.TownWarPolicy == AiCountryTownWarPolicy.Negative)
        {
          var getBack = townWars
            .OrderByDescending(w => w.IntGameDate)
            .Where(w => targetTowns.Any(t => t.Id == w.TownId) && targetTowns.FirstOrDefault(t => t.Id == w.TownId)?.CountryId != this.Country.Id)
            .FirstOrDefault(w => w.InsistedCountryId == this.Country.Id && w.Status == TownWarStatus.Terminated);
          if (getBack != null)
          {
            this.Management.TownWarTargetTownId = getBack.Id;
          }
        }
        else
        {
          var storategy = await repo.AiCountry.GetStorategyByCountryIdAsync(this.Country.Id);
          if (storategy.HasData)
          {
            var s = storategy.Data;
            if (targetTowns.Any(t => t.Id == s.NextTargetTownId))
            {
              // TownWarPolicy = Medium or Aggressive
              this.Management.TownWarTargetTownId = s.NextTargetTownId;
            }
            else if ((this.Management.TownWarPolicy == AiCountryTownWarPolicy.Aggressive || this.Management.TownWarPolicy == AiCountryTownWarPolicy.ExtraAggressive) && s.BorderTownId != 0)
            {
              var borderTown = allTowns.FirstOrDefault(t => t.Id == s.BorderTownId);
              if (borderTown != null)
              {
                var targets = allTowns
                  .GetAroundTowns(borderTown)
                  .OrderBy(t => t.Wall)
                  .Where(t => targetTowns.Any(tt => tt.Id == t.Id));
                if (this.Management.TownWarPolicy == AiCountryTownWarPolicy.Aggressive)
                {
                  var myWars = wars.Where(w => w.IsJoinAvailable(this.Country.Id)).Select(w => w.GetEnemy(this.Country.Id)).Distinct();
                  if (!myWars.Any() && this.Management.VirtualEnemyCountryId != 0)
                  {
                    myWars = myWars.Append(this.Management.VirtualEnemyCountryId);
                  }
                  targets = targets.Where(t => myWars.Contains(t.CountryId));
                }
                if (targets.Any())
                {
                  this.Management.TownWarTargetTownId = targets.First().Id;
                }
              }
            }
          }
        }
      }

      if (this.Management.TownWarTargetTownId != 0)
      {
        if (lastTownWar == null || lastTownWar.IntGameDate < this.Game.IntGameDateTime - 120)
        {
          var town = allTowns.FirstOrDefault(t => t.Id == this.Management.TownWarTargetTownId);
          if (town == null)
          {
            return false;
          }
          var arounds = allTowns.GetAroundTowns(town).Where(t => t.CountryId == this.Country.Id);

          var inReadyCharacters = readyCharas.Count(c => c.SoldierNumber >= Math.Min((short)10, c.Leadership) && arounds.Any(t => t.Id == c.TownId));
          var waitingCharacters = readyCharas.Count() - inReadyCharacters;

          if (waitingCharacters <= inReadyCharacters / 2)
          {
            await CountryService.SendTownWarAndSaveAsync(repo, new TownWar
            {
              RequestedCountryId = this.Country.Id,
              InsistedCountryId = town.CountryId,
              TownId = town.Id,
              IntGameDate = this.Game.IntGameDateTime + 1,
              Status = TownWarStatus.InReady,
            });
            return true;
          }
        }
      }

      return false;
    }

    private void ResetCharacterAiTypes(IEnumerable<Character> charas)
    {
      foreach (var c in charas)
      {
        c.AiType = c.AiType.ToManagedStandard();
      }
    }

    private async Task ChangeSomeInNotWarAsync(MainRepository repo, IEnumerable<Character> charas, IEnumerable<Town> towns, IEnumerable<CountryWar> wars)
    {
      var policies = await repo.Country.GetPoliciesAsync(this.Country.Id);
      var safeInMax = CountryService.GetCountrySafeMax(policies.GetAvailableTypes());
      if (this.Game.GameDateTime.Year >= Config.UpdateStartYear + 8)
      {
        var allTargets = charas.Where(c => c.AiType.ToManagedStandard() == CharacterAiType.ManagedBattler ||
          c.AiType.ToManagedStandard() == CharacterAiType.ManagedCivilOfficial ||
          c.AiType.ToManagedStandard() == CharacterAiType.ManagedPatroller);
        var pattern = this.Game.GameDateTime.Year / 2 % 4;
        var targets = allTargets
          .Where(c => pattern == 1 ? c.Id % 2 == 0 : pattern == 0 ? c.Id % 2 == 1 : false)
          .Where(c => c.Money > Config.RiceBuyMax + 7000 || c.Rice > Config.RiceBuyMax + 7000)
          .Where(c => c.Money + c.Rice < 100_0000)
          .Where(c => c.AiType.ToManagedStandard() != CharacterAiType.ManagedPatroller || safeInMax > 0);
        if (this.Management.TownWarTargetTownId != 0)
        {
          targets = Enumerable.Empty<Character>();
        }
        foreach (var c in targets)
        {
          if (c.AiType == CharacterAiType.ManagedBattler)
          {
            c.AiType = CharacterAiType.ManagedMoneyInflatingBattler;
          }
          else if (c.AiType == CharacterAiType.ManagedCivilOfficial)
          {
            c.AiType = CharacterAiType.ManagedMoneyInflatingCivilOfficial;
          }
          else if (safeInMax > 0 && c.AiType == CharacterAiType.ManagedPatroller)
          {
            c.AiType = CharacterAiType.ManagedMoneyInflatingPatroller;
          }
        }
        foreach (var c in allTargets.Except(targets))
        {
          c.AiType = c.AiType.ToManagedStandard();
        }
      }
    }

    private async Task ChangeSomeInNotWarOrInReadyAsync(MainRepository repo, IEnumerable<Character> charas, IEnumerable<Town> towns, IEnumerable<CountryWar> wars)
    {
      var storategyOptional = await repo.AiCountry.GetStorategyByCountryIdAsync(this.Country.Id);
      if (!storategyOptional.HasData)
      {
        return;
      }
      var storategy = storategyOptional.Data;

      if (this.Management.DevelopStyle != AiCountryDevelopStyle.NotCare)
      {
        bool Predicate(Town t) => t.Wall >= t.WallMax && t.Technology >= t.TechnologyMax;
        int DevelopableSize(Town t) => (t.WallMax - t.Wall) + (t.TechnologyMax - t.Technology);

        var isChangeTown = true;
        var oldTarget = towns.FirstOrDefault(t => t.Id == storategy.DevelopTownId);
        if (oldTarget != null)
        {
          isChangeTown = Predicate(oldTarget);
        }

        if (isChangeTown)
        {
          Town target = null;
          var myRestTowns = towns.Where(t => t.CountryId == this.Country.Id && !Predicate(t));
          if (myRestTowns.Any())
          {
            var style = this.Management.DevelopStyle;
            if (style == AiCountryDevelopStyle.BorderTownFirst)
            {
              if (this.Management.VirtualEnemyCountryId != 0)
              {
                var borderTowns = myRestTowns.Where(t => towns.GetAroundTowns(t).Any(tt => tt.CountryId == this.Management.VirtualEnemyCountryId));
                if (borderTowns.Any())
                {
                  target = borderTowns.OrderByDescending(t => DevelopableSize(t)).First();
                }
                else
                {
                  style = AiCountryDevelopStyle.LowerTownFirst;
                }
              }
              else
              {
                style = AiCountryDevelopStyle.LowerTownFirst;
              }
            }
            if (style == AiCountryDevelopStyle.LowerTownFirst)
            {
              target = myRestTowns.OrderByDescending(t => DevelopableSize(t)).First();
            }
            if (style == AiCountryDevelopStyle.HigherTownFirst)
            {
              target = myRestTowns.OrderBy(t => DevelopableSize(t)).First();
            }
          }

          if (target != null)
          {
            storategy.DevelopTownId = target.Id;
          }
          else
          {
            storategy.DevelopTownId = 0;
          }
        }
      }

      await this.SetTownWarAsync(repo, towns, charas);
    }

    private async Task ChangeSomeInWarAsync(MainRepository repo, IEnumerable<Character> charas, IEnumerable<Town> towns, IEnumerable<CountryWar> wars)
    {
      var storategyOptional = await repo.AiCountry.GetStorategyByCountryIdAsync(this.Country.Id);
      if (!storategyOptional.HasData)
      {
        return;
      }
      var storategy = storategyOptional.Data;

      if (this.Management.WarStyle != AiCountryWarStyle.NotCare)
      {
        var battleRoyaleCountries = (await repo.Country.GetAllAsync()).Where(c => !c.HasOverthrown);
        if (battleRoyaleCountries.Any(c => c.AiType == CountryAiType.Human))
        {
          battleRoyaleCountries = battleRoyaleCountries.Where(c => c.AiType == CountryAiType.Human);
        }
        var warTargets = (await repo.System.GetAsync()).IsBattleRoyaleMode ?
          battleRoyaleCountries.Select(c => c.Id) :
          wars
            .Where(w => w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.StopRequesting)
            .Select(w => w.RequestedCountryId == this.Country.Id ? w.InsistedCountryId : w.RequestedCountryId);
        var targetCharas = charas.Where(c => c.AiType.IsManaged());

        var enemyTowns = towns.Where(t => warTargets.Contains(t.CountryId));
        var enemyAroundTowns = enemyTowns.Where(t => towns.GetAroundTowns(t).Any(tt => tt.CountryId == this.Country.Id));
        if (enemyAroundTowns.Count() <= 1)
        {
          this.ResetCharacterAiTypes(targetCharas);
        }
        else
        {
          var history = await repo.AiActionHistory.GetAsync(warTargets, AiBattleTownType.BorderTown, this.Country.Id);

          var isSetActions = false;
          var setSize = 1;

          if (history.Count() >= 50)
          {
            var historyTarget = history.OrderByDescending(h => h.IntGameDateTime).Take(60).ToArray();
            var wallCount = historyTarget.Count(h => h.TargetType == AiBattleTargetType.Wall);
            var defenderZeroCount = historyTarget.Count(h => h.RestDefenderCount == 0 || h.TargetType == AiBattleTargetType.Wall);
            var moneySum = historyTarget.Sum(h => h.AttackerSoldiersMoney);

            if (this.Management.WarStyle == AiCountryWarStyle.Negative)
            {
              isSetActions = defenderZeroCount == 0 || moneySum < 40_0000;
            }
            if (this.Management.WarStyle == AiCountryWarStyle.Normal)
            {
              isSetActions = defenderZeroCount < 4 || moneySum < 60_0000;
            }
            if (this.Management.WarStyle == AiCountryWarStyle.Aggressive)
            {
              isSetActions = wallCount == 0 || moneySum < 80_0000;
            }
          }
          else if (history.Any() && history.Max(h => h.IntGameDateTime) < this.Game.IntGameDateTime - 48)
          {
            isSetActions = this.allCharacters.Where(c => c.AiType.IsManaged()).All(c => this.IsReadyForWar(c));
          }

          if (!isSetActions)
          {
            this.ResetCharacterAiTypes(targetCharas);
          }
          else
          {
            var alreadySet = targetCharas.Where(c => c.AiType.ToManagedStandard() != c.AiType);
            var changables = targetCharas
              .Where(c => c.AiType.CanBattle() && c.AiType.ToManagedStandard() == c.AiType);
            if (changables.Any(c => c.AiType == CharacterAiType.ManagedBattler))
            {
              changables = changables.Where(c => c.AiType == CharacterAiType.ManagedBattler);
            }
            for (var i = alreadySet.Count(); i < setSize && changables.Any(); i++)
            {
              var targets = changables.ToArray();
              var target = RandomService.Next(targets);
              if (target.AiType == CharacterAiType.ManagedCivilOfficial)
              {
                target.AiType = CharacterAiType.ManagedShortstopCivilOfficial;
              }
              if (target.AiType == CharacterAiType.ManagedBattler)
              {
                var r = RandomService.Next(0, 3);
                if (r == 0)
                {
                  target.AiType = CharacterAiType.ManagedShortstopBattler;
                }
                if (r == 1)
                {
                  target.AiType = CharacterAiType.ManagedWallBattler;
                }
                if (r == 2)
                {
                  target.AiType = CharacterAiType.ManagedWallBreaker;
                }
              }
            }
          }
        }
      }

      foreach (var c in charas.Where(c => c.CountryId == this.Country.Id && c.AiType.IsMoneyInflator()))
      {
        c.AiType = c.AiType.ToManagedStandard();
      }
      if (!(await repo.System.GetAsync()).IsBattleRoyaleMode && wars.Min(w => w.IntStartGameDate) > this.Game.IntGameDateTime + 24)
      {
        var developTownOptional = await repo.Town.GetByIdAsync(storategy.DevelopTownId);
        foreach (var c in charas.Where(c => c.CountryId == this.Country.Id && (c.Money >= Config.RiceBuyMax || c.Rice >= Config.RiceBuyMax)))
        {
          if (c.AiType == CharacterAiType.ManagedBattler && c.Money + c.Rice < 18_0000)
          {
            c.AiType = CharacterAiType.ManagedMoneyInflatingBattler;
          }
          else if (c.AiType == CharacterAiType.ManagedPatroller || c.AiType == CharacterAiType.ManagedMoneyInflatingPatroller)
          {
            // 米転がし＜民忠
            if (!developTownOptional.HasData || developTownOptional.Data.Security >= 100)
            {
              c.AiType = CharacterAiType.ManagedMoneyInflatingPatroller;
            }
            else
            {
              c.AiType = CharacterAiType.ManagedPatroller;
            }
          }
        }
      }
      else
      {
        foreach (var c in charas.Where(c => c.CountryId == this.Country.Id))
        {
          if (c.AiType.IsMoneyInflator())
          {
            c.AiType = c.AiType.ToManagedStandard();
          }
        }
      }
    }

    private async Task GetPolicyAsync(MainRepository repo, IEnumerable<CountryPolicy> policies, IEnumerable<CountryPolicyType> types)
    {
      foreach (var type in types)
      {
        var exists = policies.FirstOrDefault(p => p.Type == type);
        if (exists != null && exists.Status == CountryPolicyStatus.Available)
        {
          continue;
        }

        if (!await CountryService.SetPolicyAndSaveAsync(repo, this.Country, type, isCheckSubjects: false))
        {
          break;
        }
      }
    }

    private bool IsReadyForFirst(IEnumerable<CountryPolicy> policies)
    {
      return this.HasPolicies(policies, this.RequestedPolicyTypesFirst);
    }

    private bool IsReadyForWar(IEnumerable<CountryPolicy> policies)
    {
      return this.HasPolicies(policies, this.RequestedPolicyTypesForWar);
    }

    private bool HasPolicies(IEnumerable<CountryPolicy> policies, IEnumerable<CountryPolicyType> types)
    {
      return types.All(p => policies.GetAvailableTypes().Contains(p));
    }

    private bool IsReadyForWar(Town town)
    {
      return ((float)town.Wall / town.WallMax) > 0.85f && town.Technology >= 900;
    }

    private bool IsReadyForWar(Character chara)
    {
      var type = chara.GetCharacterType();
      if (type == CharacterType.Strong || type == CharacterType.Intellect)
      {
        return chara.Money + chara.Rice >= (this.Management.WarPolicy == AiCountryWarPolicy.GoodFight ? 100_0000 : 90_0000);
      }
      if (type == CharacterType.Popularity)
      {
        return chara.Rice >= 10000;
      }
      return true;
    }
  }
}
