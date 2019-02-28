using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Updates
{
  public class FarmerBattlerAiCharacter : AiCharacter
  {
    protected virtual bool CanWall => false;

    public FarmerBattlerAiCharacter(Character character) : base(character)
    {
    }

    protected virtual SoldierType FindSoldierType()
    {
      return SoldierType.Common;
    }

    protected override async Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      var isNeedSoldier = this.Character.SoldierNumber < Math.Max(this.Character.Leadership / 10, 20);
      var towns = await repo.Town.GetAllAsync();
      var command = new CharacterCommand
      {
        Id = 0,
        CharacterId = this.Character.Id,
        GameDateTime = this.GameDateTime,
      };

      if (this.Town.CountryId != this.Character.CountryId || (this.Town.People < 3000 && isNeedSoldier))
      {
        await this.MoveToMyCountryTownAsync(repo, towns, t => t.People > 3000, command);
        return command;
      }

      if (isNeedSoldier)
      {
        command.Type = CharacterCommandType.Soldier;
        command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 1,
          NumberValue = (int)this.FindSoldierType(),
        });
        command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 2,
          NumberValue = this.Character.Leadership,
        });
        return command;
      }

      if (this.Town.Id == this.Country.CapitalTownId)
      {
        var currentDefenders = await repo.Town.GetDefendersAsync(this.Town.Id);
        if (!currentDefenders.Any(d => d.Character.Id == this.Character.Id))
        {
          command.Type = CharacterCommandType.Defend;
          return command;
        }
      }

      var availableWar = wars
        .FirstOrDefault(w => w.IntStartGameDate <= this.GameDateTime.ToInt() && (w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.StopRequesting));
      if (availableWar != null)
      {
        var targetCountryId = availableWar.InsistedCountryId == this.Country.Id ? availableWar.RequestedCountryId : availableWar.InsistedCountryId;
        var targetTowns = towns
          .GetAroundTowns(this.Town)
          .Where(t => t.CountryId == targetCountryId);
        if (!targetTowns.Any())
        {
          // 攻撃先と隣接していない
          await this.MoveToMyCountryTownNextToCountryAsync(repo, towns, t => t.People > 3000, targetCountryId, command);
          return command;
        }
        else
        {
          var targetTownsData = new List<(Town Town, IEnumerable<Character> Defenders)>();
          foreach (var town in targetTowns)
          {
            var defenders = await repo.Town.GetDefendersAsync(town.Id);
            targetTownsData.Add(((Town)town, defenders.Select(td => td.Character)));
          }

          var targetTown = this.CanWall ?
            targetTownsData.OrderBy(td => td.Defenders.Count()).First() :
            targetTownsData.OrderByDescending(td => td.Defenders.Count()).First();

          // 侵攻
          command.Parameters.Add(new CharacterCommandParameter
          {
            Type = 1,
            NumberValue = (int)targetTown.Town.Id,
          });
          command.Type = CharacterCommandType.Battle;
          return command;
        }
      }
      else
      {
        var readyWar = wars
          .OrderBy(w => w.IntStartGameDate)
          .FirstOrDefault(w => w.IntStartGameDate <= this.GameDateTime.ToInt() + 3 && w.Status == CountryWarStatus.InReady || w.Status == CountryWarStatus.StopRequesting);
        if (readyWar != null)
        {
          // 戦争準備中の国に隣接してないなら移動する
          if (!towns.GetAroundTowns(this.Town).Any(t => t.CountryId != readyWar.InsistedCountryId && t.CountryId != readyWar.RequestedCountryId))
          {
            await this.MoveToMyCountryTownNextToCountryAsync(repo, towns, t => t.People > 3000, readyWar.InsistedCountryId == this.Country.Id ? readyWar.RequestedCountryId : readyWar.InsistedCountryId, command);
            return command;
          }
        }

        if (this.Character.Proficiency < 100)
        {
          command.Type = CharacterCommandType.SoldierTraining;
        }
        else
        {
          command.Type = CharacterCommandType.Training;
          command.Parameters.Add(new CharacterCommandParameter
          {
            Type = 1,
            NumberValue = 1,
          });
        }
        return command;
      }
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "農民_武将";
      this.Character.Strong = (short)(10 + current.Year / 3.2f);
      this.Character.Leadership = 100;
      this.Character.Money = 1000000;
      this.Character.Rice = 1000000;
    }
  }

  public class FarmerCivilOfficialAiCharacter : AiCharacter
  {
    public FarmerCivilOfficialAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "農民_文官";
      this.Character.Intellect = (short)(100 + current.Year / 3.2f);
      this.Character.Money = 1000000;
      this.Character.Rice = 1000000;
    }

    protected virtual void SetCommandOnNoWars(CharacterCommand command)
    {
      if (this.GameDateTime.Month % 2 == 0)
      {
        command.Type = CharacterCommandType.Wall;
      }
      else
      {
        command.Type = CharacterCommandType.WallGuard;
      }
    }

    protected override async Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      var towns = await repo.Town.GetAllAsync();
      var command = new CharacterCommand
      {
        Id = 0,
        CharacterId = this.Character.Id,
        GameDateTime = this.GameDateTime,
      };

      if (this.Town.Id != this.Country.CapitalTownId)
      {
        this.MoveToTown(this.Country.CapitalTownId, command);
        return command;
      }
      else
      {
        var availableWar = wars.FirstOrDefault(w => w.IntStartGameDate <= this.GameDateTime.ToInt() && (w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.StopRequesting));
        if (availableWar != null)
        {
          // 首都で守備ループ
          if (this.GameDateTime.Month % 2 == 0)
          {
            command.Parameters.Add(new CharacterCommandParameter
            {
              Type = 1,
              NumberValue = 1,
            });
            command.Parameters.Add(new CharacterCommandParameter
            {
              Type = 2,
              NumberValue = 1,
            });
            command.Type = CharacterCommandType.Soldier;
            return command;
          }
          else
          {
            command.Type = CharacterCommandType.Defend;
            return command;
          }
        }
        else
        {
          this.SetCommandOnNoWars(command);
          return command;
        }
      }
    }
  }

  public class FarmerPatrollerAiCharacter : AiCharacter
  {
    public FarmerPatrollerAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "農民_仁官";
      this.Character.Popularity = (short)(100 + current.Year / 2.8f);
      this.Character.Money = 1000000;
      this.Character.Rice = 1000000;
    }

    protected override async Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      var towns = await repo.Town.GetAllAsync();
      var command = new CharacterCommand
      {
        Id = 0,
        CharacterId = this.Character.Id,
        GameDateTime = this.GameDateTime,
      };

      if (this.Town.Id != this.Country.CapitalTownId)
      {
        this.MoveToTown(this.Country.CapitalTownId, command);
        return command;
      }
      else
      {
        var availableWar = wars.FirstOrDefault(w => w.IntStartGameDate <= this.GameDateTime.ToInt() && (w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.StopRequesting));
        if (availableWar != null)
        {
          command.Type = CharacterCommandType.Security;
          return command;
        }
        else if (this.Town.Security < 100)
        {
          command.Type = CharacterCommandType.Security;
          return command;
        }
        else
        {
          command.Parameters.Add(new CharacterCommandParameter
          {
            Type = 1,
            NumberValue = 4,
          });
          command.Type = CharacterCommandType.Training;
          return command;
        }
      }
    }
  }
}
