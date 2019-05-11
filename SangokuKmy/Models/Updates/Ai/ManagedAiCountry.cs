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
    private IEnumerable<CountryPolicyType> PolicyTypes
    {
      get
      {
        List<CountryPolicyType> primary = null;
        if (this.Management.PolicyTarget == AiCountryPolicyTarget.Wall)
        {
          primary = new List<CountryPolicyType>
          {
            CountryPolicyType.AntiGang,
            CountryPolicyType.AttackDefend,
            CountryPolicyType.HumanDevelopment,
            CountryPolicyType.Earthwork,
            CountryPolicyType.StrongCountry,
            CountryPolicyType.StoneCastle,
          };
        }
        else if (this.Management.PolicyTarget == AiCountryPolicyTarget.Money)
        {
          primary = new List<CountryPolicyType>
          {
            CountryPolicyType.HumanDevelopment,
            CountryPolicyType.Storage,
            CountryPolicyType.UndergroundStorage,
            CountryPolicyType.Economy,
          };
        }
        else if (this.Management.PolicyTarget == AiCountryPolicyTarget.People)
        {
          primary = new List<CountryPolicyType>
          {
            CountryPolicyType.AntiGang,
            CountryPolicyType.Economy,
            CountryPolicyType.SaveWall,
            CountryPolicyType.HumanDevelopment,
          };
        }

        var normal = new List<CountryPolicyType>
        {
          CountryPolicyType.AntiGang,
          CountryPolicyType.AttackDefend,
          CountryPolicyType.HumanDevelopment,
          CountryPolicyType.IntellectCountry,
          CountryPolicyType.StrongCountry,
          CountryPolicyType.Storage,
          CountryPolicyType.Earthwork,
          CountryPolicyType.SaveWall,
          CountryPolicyType.StoneCastle,
          CountryPolicyType.UndergroundStorage,
          CountryPolicyType.Economy,
        };

        if (primary != null)
        {
          return primary
            .Concat(normal)
            .Distinct();
        }

        return normal;
      }
    }

    private IEnumerable<CountryPolicyType> RequestedPolicyTypesForWar
    {
      get
      {
        var primary = new List<CountryPolicyType>();
        if (this.Management.PolicyTarget == AiCountryPolicyTarget.Wall)
        {
          primary.Add(CountryPolicyType.Earthwork);
        }
        else if (this.Management.PolicyTarget == AiCountryPolicyTarget.Money)
        {
          primary.Add(CountryPolicyType.AntiGang);
        }
        else if (this.Management.PolicyTarget == AiCountryPolicyTarget.People)
        {
          primary.Add(CountryPolicyType.AntiGang);
          primary.Add(CountryPolicyType.SaveWall);
        }

        if (this.Management.WarPolicy == AiCountryWarPolicy.GoodFight)
        {
          primary.Add(CountryPolicyType.Earthwork);
        }
        else if (this.Management.SeiranPolicy == AgainstSeiranPolicy.NotCare || this.Management.SeiranPolicy == AgainstSeiranPolicy.NotCareMuch)
        {
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
        primary.Add(CountryPolicyType.AttackDefend);
        if (this.Management.PolicyTarget == AiCountryPolicyTarget.Wall)
        {
          if (this.Management.CharacterSize == AiCountryCharacterSize.Medium ||
            this.Management.CharacterSize == AiCountryCharacterSize.Large)
          {
            primary.Add(CountryPolicyType.Earthwork);
          }
        }
        else if (this.Management.PolicyTarget == AiCountryPolicyTarget.Money)
        {
          if (this.Management.CharacterSize == AiCountryCharacterSize.Large)
          {
          }
        }
        else if (this.Management.PolicyTarget == AiCountryPolicyTarget.People)
        {
          if (this.Management.CharacterSize == AiCountryCharacterSize.Large)
          {
          }
        }

        return primary;
      }
    }

    public ManagedAiCountry(Country country) : base(country)
    {
    }

    protected override async Task RunInnerAsync(MainRepository repo)
    {
      var allTowns = await repo.Town.GetAllAsync();
      var towns = allTowns.Where(t => t.CountryId == this.Country.Id);
      var allCharacters = await repo.Character.GetAllAliveAsync();
      var characters = allCharacters.Where(c => c.CountryId == this.Country.Id);
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
        this.ResetCharacterAiTypes(characters.Where(c => c.AiType.IsManaged()));
        if (await this.FindVirtualEnemyCountryAsync(repo, allTowns, allCharacters))
        {
          await this.SetWarAsync(repo, allTowns, allCharacters);
        }
      }
      else
      {
        await this.ChangeSomeInWarAsync(repo, characters, towns, wars);
      }

      await this.GetPolicyAsync(repo, policies, this.PolicyTypes);

      this.Management.IsPolicyFirst = !wars.Any() && !this.IsReadyForFirst(policies);
      this.Management.IsPolicySecond = !wars.Any() && !this.IsReadyForWar(policies);
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

      var aroundCountries = allTowns.GetAroundCountries(allTowns.Where(t => t.CountryId == this.Country.Id)).ToArray();
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
      aroundCountries = aroundCountries.Concat(
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
        if (aroundCountries.Count() == 1)
        {
          this.Management.VirtualEnemyCountryId = aroundCountries.First();
        }
        else
        {
          if (this.Management.WarTargetPolicy == AiCountryWarTargetPolicy.EqualityWeaker)
          {
            if (GetCountrySize(aroundCountries.First()) < mySize)
            {
              this.Management.VirtualEnemyCountryId = aroundCountries.Last(c => GetCountrySize(c) < mySize);
            }
            else
            {
              this.Management.VirtualEnemyCountryId = aroundCountries.First();
            }
            return true;
          }
          if (this.Management.WarTargetPolicy == AiCountryWarTargetPolicy.EqualityStronger)
          {
            if (GetCountrySize(aroundCountries.Last()) > mySize)
            {
              this.Management.VirtualEnemyCountryId = aroundCountries.First(c => GetCountrySize(c) > mySize);
            }
            else
            {
              this.Management.VirtualEnemyCountryId = aroundCountries.Last();
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
        isCreated = await AiService.CreateWarAsync(repo, this.Country, target, startMonth);
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

      if (history.Count() >= 50)
      {
        var historyTarget = history.OrderByDescending(h => h.IntGameDateTime).Take(60).ToArray();
        var wallCount = historyTarget.Count(h => h.TargetType == AiBattleTargetType.Wall);
        var defenderZeroCount = historyTarget.Count(h => h.RestDefenderCount == 0 || h.TargetType == AiBattleTargetType.Wall);

        var isSet = false;
        var setSize = 1;

        if (this.Management.WarStyle == AiCountryWarStyle.Negative)
        {
          isSet = defenderZeroCount == 0;
        }
        if (this.Management.WarStyle == AiCountryWarStyle.Normal)
        {
          isSet = defenderZeroCount < 4;
        }
        if (this.Management.WarStyle == AiCountryWarStyle.Aggressive)
        {
          isSet = wallCount == 0;
        }

        if (!isSet)
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
      return this.RequestedPolicyTypesFirst.All(p => policies.Any(pp => pp.Type == p));
    }

    private bool IsReadyForWar(IEnumerable<CountryPolicy> policies)
    {
      return this.RequestedPolicyTypesForWar.All(p => policies.Any(pp => pp.Type == p));
    }

    private bool IsReadyForWar(Town town)
    {
      return ((float)town.Wall / town.WallMax) > 0.85f && town.Technology >= 900;
    }

    private bool IsReadyForWar(Character chara)
    {
      var type = chara.GetCharacterType();
      if (type == CharacterType.Strong)
      {
        return chara.Money >= (this.Management.WarPolicy == AiCountryWarPolicy.GoodFight ? 30000 : 60000);
      }
      if (type == CharacterType.Intellect)
      {
        return chara.Money >= (this.Management.WarPolicy == AiCountryWarPolicy.GoodFight ? 70000 : 100000);
      }
      if (type == CharacterType.Popularity)
      {
        return chara.Rice >= 10000;
      }
      return true;
    }
  }
}
