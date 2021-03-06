﻿using SangokuKmy.Common;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Updates.Ai
{
  public static class AiCharacterFactory
  {
    public static AiCharacter Create(CharacterAiType type)
    {
      var chara = new Character
      {
        AiType = type,
      };
      return Create(chara);
    }

    public static AiCharacter Create(Character chara)
    {
      AiCharacter ai = null;
      if (chara.AiType == CharacterAiType.FarmerBattler)
      {
        ai = new FarmerBattlerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.FarmerCivilOfficial)
      {
        ai = new FarmerCivilOfficialAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.FarmerPatroller)
      {
        ai = new FarmerPatrollerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.FarmerEvangelist)
      {
        ai = new FarmerEvangelistAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.TerroristBattler)
      {
        ai = new TerroristBattlerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.TerroristWallBattler)
      {
        ai = new TerroristWallBattlerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.TerroristRyofu)
      {
        ai = new TerroristRyofuAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.TerroristCivilOfficial)
      {
        ai = new TerroristCivilOfficialAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.TerroristPatroller)
      {
        ai = new TerroristPatrollerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.TerroristMainPatroller)
      {
        ai = new TerroristMainPatrollerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.SecretaryPatroller)
      {
        ai = new SecretaryPatrollerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.SecretaryUnitGather)
      {
        ai = new SecretaryGatherAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.SecretaryUnitLeader)
      {
        ai = new SecretaryLeaderAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.SecretaryPioneer)
      {
        ai = new SecretaryPioneerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.SecretaryScouter)
      {
        ai = new SecretaryScouterAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.SecretaryEvangelist)
      {
        ai = new SecretaryEvangelistAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.ThiefBattler)
      {
        ai = new ThiefBattlerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.ThiefWallBattler)
      {
        ai = new ThiefWallBattlerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.ThiefPatroller)
      {
        ai = new ThiefPatrollerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.ManagedBattler)
      {
        ai = new ManagedBattlerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.ManagedWallBattler)
      {
        ai = new ManagedWallBattlerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.ManagedWallBreaker)
      {
        ai = new ManagedWallBreakerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.ManagedShortstopBattler)
      {
        ai = new ManagedShortstopBattlerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.ManagedCivilOfficial)
      {
        ai = new ManagedCivilOfficialAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.ManagedShortstopCivilOfficial)
      {
        ai = new ManagedShortstopCivilOfficialAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.ManagedPatroller)
      {
        ai = new ManagedPatrollerAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.ManagedMoneyInflatingBattler ||
        chara.AiType == CharacterAiType.ManagedMoneyInflatingCivilOfficial ||
        chara.AiType == CharacterAiType.ManagedMoneyInflatingPatroller)
      {
        ai = new ManagedMoneyInflaterAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.ManagedEvangelist)
      {
        ai = new ManagedEvangelistAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.FlyingColumn)
      {
        ai = new FlyingColumnAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.FreeEvangelist)
      {
        ai = new FreeEvangelistAiCharacter(chara);
      }
      else if (chara.AiType == CharacterAiType.SecretaryTrader)
      {
        ai = new SecretaryTraderAiCharacter(chara);
      }
      else
      {
        ai = new HumanCharacter(chara);
      }

      return ai;
    }
  }

  public abstract class AiCharacter
  {
    public Character Character { get; }

    protected GameDateTime GameDateTime { get; set; }

    protected Country Country { get; set; }

    protected Town Town { get; set; }

    public AiCharacter(Character character)
    {
      this.Character = character;
    }

    protected virtual async Task<Optional<CharacterCommand>> GetCommandAsNoCountryAsync(MainRepository repo)
    {
      return default;
    }

    public virtual async Task<Optional<CharacterCommand>> GetCommandAsync(MainRepository repo, GameDateTime current)
    {
      var country = await repo.Country.GetAliveByIdAsync(this.Character.CountryId);

      var town = await repo.Town.GetByIdAsync(this.Character.TownId);
      if (!town.HasData)
      {
        return default;
      }

      this.GameDateTime = current;
      this.Country = country.Data;
      this.Town = town.Data;

      if (!country.HasData)
      {
        var cmd = await this.GetCommandAsNoCountryAsync(repo);
        if (!cmd.HasData)
        {
          this.Character.DeleteTurn = (short)Config.DeleteTurns;
          return default;
        }
        else
        {
          return cmd;
        }
      }

      var wars = (await repo.CountryDiplomacies.GetAllWarsAsync()).Where(w => w.InsistedCountryId == this.Country.Id || w.RequestedCountryId == this.Country.Id);

      return await this.GetCommandInnerAsync(repo, wars).ToOptionalAsync();
    }

    protected abstract Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars);

    protected IEnumerable<Town> GetStreet(IEnumerable<Town> towns, Town from, Town target, Func<TownBase, bool> predicate = null)
    {
      // 経路探索
      bool checkStreet(IList<Town> street)
      {
        var current = street.Last();
        var arounds = towns
          .GetAroundTowns(current)
          .Where(a => !street.Contains(a) && !street.Reverse().Skip(1).Any(s => s.IsNextToTown(a)));
        if (predicate != null)
        {
          arounds = arounds.Where(predicate);
        }
        if (arounds.Any(a => a.Id == target.Id))
        {
          // 周りにあれば返す
          street.Add(towns.FirstOrDefault(s => s.Id == target.Id));
          return true;
        }
        else
        {
          // 周りになければ経路探索
          var results = new List<List<Town>>();
          foreach (var a in arounds)
          {
            var list = new List<Town>(street)
            {
              (Town)a,
            };
            var r = checkStreet(list);
            if (r)
            {
              results.Add(list);
            }
          }
          if (results.Any())
          {
            var shorter = results.OrderBy(r => r.Count).First().Skip(1);
            foreach (var s in shorter)
            {
              street.Add(s);
            }
            return true;
          }
          else
          {
            return false;
          }
        }
      }

      var st = new List<Town>
      {
        from,
      };
      var nextTo = checkStreet(st);
      return nextTo ? st : null;
    }

    protected async Task<Town> GetNextTownForMoveToTownAsync(MainRepository repo, Town from, Town target)
    {
      if (from.IsNextToTown(target))
      {
        return target;
      }

      if (from.Id == target.Id)
      {
        return target;
      }

      return (this.GetStreet(await repo.Town.GetAllAsync(), from, target)).ElementAt(1);
    }

    protected Town GetMatchTown(IEnumerable<Town> towns, Func<TownBase, object> order, Func<TownBase, bool> subject, bool isSearchMyCountryTown = true)
    {
      var current = this.Town;

      var arounds = towns
        .GetAroundTowns(current)
        .Append(current);
      if (order != null)
      {
        arounds = arounds.OrderByDescending(order);
      }

      var aroundsRank = arounds
        .Where(subject);
      if (order != null)
      {
        aroundsRank = aroundsRank.OrderByDescending(order);
      }
      if (aroundsRank.Any())
      {
        // 条件に合う都市が隣にある
        return (Town)aroundsRank.First();
      }
      else
      {
        // 探索範囲を自国の全都市に広げる
        var myCountryTowns = isSearchMyCountryTown ? towns.Where(t => t.CountryId == this.Country.Id) : towns;
        var allRank = myCountryTowns
          .OrderByDescending(t => t.People + t.Wall);
        var match = allRank.FirstOrDefault(subject);
        Town target;
        if (match != null)
        {
          target = (Town)match;
        }
        else
        {
          target = allRank.FirstOrDefault();
        }

        if (target != null && current.Id != target.Id)
        {
          return target;
        }
        else
        {
          return null;
        }
      }
    }

    protected async Task<Town> Old_GetMatchTownAsync(MainRepository repo, IEnumerable<Town> towns, Func<TownBase, object> order, Func<TownBase, bool> subject)
    {
      var target = this.GetMatchTown(towns, order, t => subject(t) && t.CountryId == this.Country.Id);
      if (target != null)
      {
        return await this.GetNextTownForMoveToTownAsync(repo, this.Town, target);
      }
      else
      {
        return null;
      }
    }

    protected async Task<bool> MoveToMyCountryTownAsync(MainRepository repo, IEnumerable<Town> towns, Func<TownBase, bool> subject, Func<TownBase, object> order, CharacterCommand command)
    {
      var target = await this.Old_GetMatchTownAsync(repo, towns, order, subject);
      if (target != null && target.Id != this.Town.Id)
      {
        this.InputMoveToTown(target.Id, command);
        return true;
      }
      return false;
    }

    protected async Task MoveToMyCountryTownAsync(MainRepository repo, IEnumerable<Town> towns, Func<TownBase, bool> subject, CharacterCommand command)
    {
      var target = await this.Old_GetMatchTownAsync(repo, towns, null, subject);
      if (target != null && target.Id != this.Town.Id)
      {
        this.InputMoveToTown(target.Id, command);
      }
    }

    protected async Task<bool> MoveToTownOrNextToTownAsync(MainRepository repo, Town from, Town target, CharacterCommand command)
    {
      if (from.Id == target.Id)
      {
        return false;
      }
      else
      {
        var town = await this.GetNextTownForMoveToTownAsync(repo, from, target);
        this.InputMoveToTown(town.Id, command);
        return true;
      }
    }

    protected void InputMoveToTown(uint townId, CharacterCommand command)
    {
      command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 1,
        NumberValue = (int)townId,
      });
      command.Type = CharacterCommandType.Move;
    }

    protected async Task<bool> MoveToMyCountryTownNextToCountryAsync(MainRepository repo, IEnumerable<Town> towns, Func<TownBase, bool> subject, Func<TownBase, object> order, uint countryId, CharacterCommand command)
    {
      return await this.MoveToMyCountryTownAsync(repo, towns, t => subject(t) && towns.GetAroundTowns(t).Any(tt => tt.CountryId == countryId), order, command);
    }

    protected async Task<bool> MoveToMyCountryTownNextToCountryAsync(MainRepository repo, IEnumerable<Town> towns, Func<TownBase, bool> subject, Func<TownBase, object> order, IEnumerable<uint> countryIds, CharacterCommand command)
    {
      return await this.MoveToMyCountryTownAsync(repo, towns, t => subject(t) && towns.GetAroundTowns(t).Any(tt => countryIds.Contains(tt.CountryId)), order, command);
    }

    public abstract void Initialize(GameDateTime current);
  }

  public class HumanCharacter : AiCharacter
  {
    public HumanCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      throw new NotImplementedException();
    }

    public override async Task<Optional<CharacterCommand>> GetCommandAsync(MainRepository repo, GameDateTime current)
    {
      var cmd = (await repo.CharacterCommand.GetAsync(this.Character.Id, current)).Data ?? new CharacterCommand
      {
        Type = CharacterCommandType.None,
      };
      return cmd.ToOptional();
    }

    protected override Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      throw new NotImplementedException();
    }
  }
}
