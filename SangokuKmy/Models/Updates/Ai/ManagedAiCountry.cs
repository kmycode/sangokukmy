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
            CountryPolicyType.IntellectCountry,
            CountryPolicyType.StrongCountry,
            CountryPolicyType.AntiGang,
            CountryPolicyType.AttackDefend,
            CountryPolicyType.Earthwork,
            CountryPolicyType.StoneCastle,
            CountryPolicyType.Economy,
            CountryPolicyType.SaveWall,
          };
        }
        else if (this.Management.PolicyTarget == AiCountryPolicyTarget.Money)
        {
          primary = new List<CountryPolicyType>
          {
            CountryPolicyType.HumanDevelopment,
            CountryPolicyType.IntellectCountry,
            CountryPolicyType.StrongCountry,
            CountryPolicyType.Economy,
            CountryPolicyType.AddSalary,
            CountryPolicyType.Storage,
            CountryPolicyType.Collection,
            CountryPolicyType.UndergroundStorage,
            CountryPolicyType.WallEar,
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
            CountryPolicyType.AttackDefend,
            CountryPolicyType.Siege,
            CountryPolicyType.Shosha,
          };
        }

        var normal = new List<CountryPolicyType>
        {
          CountryPolicyType.HumanDevelopment,
          CountryPolicyType.IntellectCountry,
          CountryPolicyType.StrongCountry,
          CountryPolicyType.AntiGang,
          CountryPolicyType.AttackDefend,
          CountryPolicyType.Earthwork,
          CountryPolicyType.StoneCastle,
          CountryPolicyType.PopularityCountry,
          CountryPolicyType.Storage,
          CountryPolicyType.Economy,
          CountryPolicyType.Collection,
          CountryPolicyType.UndergroundStorage,
          CountryPolicyType.WallEar,
          CountryPolicyType.AddSalary,
          CountryPolicyType.Siege,
          CountryPolicyType.Shosha,
          CountryPolicyType.SaveWall,
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
          primary.Add(CountryPolicyType.StoneCastle);
        }
        else if (this.Management.PolicyTarget == AiCountryPolicyTarget.Money)
        {
          primary.Add(CountryPolicyType.Earthwork);
        }
        else if (this.Management.PolicyTarget == AiCountryPolicyTarget.WallAttack)
        {
          primary.Add(CountryPolicyType.Earthwork);
        }

        if (this.Management.WarPolicy == AiCountryWarPolicy.GoodFight)
        {
          primary.Remove(CountryPolicyType.StoneCastle);
          primary.Add(CountryPolicyType.Earthwork);
        }
        else if (this.Management.SeiranPolicy == AgainstSeiranPolicy.NotCare || this.Management.SeiranPolicy == AgainstSeiranPolicy.NotCareMuch)
        {
          primary.Remove(CountryPolicyType.StoneCastle);
          primary.Add(CountryPolicyType.Earthwork);
        }
        else
        {
          primary.Add(CountryPolicyType.StoneCastle);
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
            primary.Add(CountryPolicyType.AttackDefend);
          }
          else if (this.Management.CharacterSize == AiCountryCharacterSize.Medium)
          {
            primary.Add(CountryPolicyType.Earthwork);
          }
          else if (this.Management.CharacterSize == AiCountryCharacterSize.Large)
          {
            primary.Add(CountryPolicyType.StoneCastle);
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
          CountryPolicyType.AntiGang,
          CountryPolicyType.AttackDefend,
          CountryPolicyType.HumanDevelopment,
          CountryPolicyType.Earthwork,
          CountryPolicyType.StrongCountry,
          CountryPolicyType.StoneCastle,
        };
      }
    }

    private IEnumerable<CountryPolicyType> RequestedPolicyTypesUntilWarFirst
    {
      get
      {
        return new List<CountryPolicyType>
        {
          CountryPolicyType.Earthwork,
        };
      }
    }

    public ManagedAiCountry(Country country) : base(country)
    {
    }

    protected override async Task RunInnerAsync(MainRepository repo)
    {
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

      if (!wars.Any())
      {
        this.ResetCharacterAiTypes(characters.Where(c => c.AiType.IsManaged() && !c.AiType.IsMoneyInflator()));
        if (await this.FindVirtualEnemyCountryAsync(repo, this.allTowns, this.allCharacters))
        {
          await this.SetWarAsync(repo, this.allTowns, this.allCharacters);
        }
      }
      else
      {
        await this.ChangeSomeInWarAsync(repo, characters, this.allTowns, wars);
      }

      var requestPolicies = !wars.Any() ? this.PolicyTypes : this.PolicyTypes.Concat(this.RequestedPolicyTypesUntilWar).Distinct();
      await this.GetPolicyAsync(repo, policies, requestPolicies);

      this.Management.IsPolicyFirst = !wars.Any() && !this.IsReadyForFirst(policies);
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
      this.Management.VirtualEnemyCountryId = 0;

      var aroundCountries = allTowns
        .GetAroundCountries(allTowns.Where(t => t.CountryId == this.Country.Id))
        .OrderBy(c => GetCountrySize(c))
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
      var policies = await repo.Country.GetPoliciesAsync(this.Country.Id);
      var towns = allTowns.Where(t => t.CountryId == this.Country.Id);
      var characters = allCharacters.Where(c => c.CountryId == this.Country.Id && !c.AiType.IsSecretary());
      var townsInReady = towns.Where(t => this.IsReadyForWar(t));
      var charactersInReady = characters.Where(c => this.IsReadyForWar(c));
      var characterGroupInReady = charactersInReady.GroupBy(c => c.GetCharacterType());

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
      if (startMonth.ToInt() <= this.Game.IntGameDateTime + 144)
      {
        isCreated = await AiService.CreateWarIfNotWarAsync(repo, this.Country, target, startMonth);
        if (isCreated)
        {
          this.Management.VirtualEnemyCountryId = 0;
        }
      }
      return isCreated;
    }

    private void ResetCharacterAiTypes(IEnumerable<Character> charas)
    {
      foreach (var c in charas)
      {
        c.AiType = c.AiType.ToManagedStandard();
      }
    }

    private async Task ChangeSomeInWarAsync(MainRepository repo, IEnumerable<Character> charas, IEnumerable<Town> towns, IEnumerable<CountryWar> wars)
    {
      if (this.Management.WarStyle == AiCountryWarStyle.NotCare)
      {
        return;
      }

      var storategyOptional = await repo.AiCountry.GetStorategyByCountryIdAsync(this.Country.Id);
      if (!storategyOptional.HasData)
      {
        return;
      }
      var storategy = storategyOptional.Data;
      
      var warTargets = wars
        .Where(w => w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.StopRequesting)
        .Select(w => w.RequestedCountryId == this.Country.Id ? w.InsistedCountryId : w.RequestedCountryId);
      var targetCharas = charas.Where(c => c.AiType.IsManaged());

      var enemyTowns = towns.Where(t => warTargets.Contains(t.CountryId));
      var enemyAroundTowns = enemyTowns.Where(t => towns.GetAroundTowns(t).Any(tt => tt.CountryId == this.Country.Id));
      if (enemyAroundTowns.Count() <= 1)
      {
        this.ResetCharacterAiTypes(targetCharas);
        return;
      }

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
          isSetActions = defenderZeroCount == 0 || moneySum < 60_0000;
        }
        if (this.Management.WarStyle == AiCountryWarStyle.Normal)
        {
          isSetActions = defenderZeroCount < 4 || moneySum < 40_0000;
        }
        if (this.Management.WarStyle == AiCountryWarStyle.Aggressive)
        {
          isSetActions = wallCount == 0 || moneySum < 20_0000;
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
        var changables = targetCharas.Where(c => c.AiType.CanBattle() && c.AiType.ToManagedStandard() == c.AiType);
        for (var i = alreadySet.Count(); i < setSize && changables.Any(); i++)
        {
          var targets = changables.ToArray();
          var target = targets[RandomService.Next(0, targets.Length)];
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

    private async Task GetPolicyAsync(MainRepository repo, IEnumerable<CountryPolicy> policies, IEnumerable<CountryPolicyType> types)
    {
      foreach (var type in types)
      {
        var exists = policies.FirstOrDefault(p => p.Type == type);
        if (exists != null && exists.Status == CountryPolicyStatus.Available)
        {
          continue;
        }

        if (!await CountryService.SetPolicyAndSaveAsync(repo, this.Country, type))
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
        return chara.Money + chara.Rice >= (this.Management.WarPolicy == AiCountryWarPolicy.GoodFight ? 20_0000 : 40_0000);
      }
      if (type == CharacterType.Popularity)
      {
        return chara.Rice >= 10000;
      }
      return true;
    }
  }
}
