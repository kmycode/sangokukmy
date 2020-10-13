using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Services;

namespace SangokuKmy.Models.Updates.Ai
{
  public abstract class FarmerAiCharacter : AiCharacter
  {
    protected List<CountryWar> Wars { get; } = new List<CountryWar>();

    public FarmerAiCharacter(Character character) : base(character)
    {
    }

    protected async Task<IEnumerable<uint>> GetWarTargetCountriesAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      var system = await repo.System.GetAsync();
      if (system.IsBattleRoyaleMode)
      {
        var countries = (await repo.Country.GetAllAsync()).Where(c => !c.HasOverthrown);
        if (countries.Any(c => c.AiType == CountryAiType.Human))
        {
          return countries.Where(c => c.AiType == CountryAiType.Human).Select(c => c.Id);
        }
        else
        {
          return countries.Select(c => c.Id).Append(0u);
        }
      }

      this.UpdateAvailableWars(wars);
      return this.Wars.Select(w => w.InsistedCountryId == this.Country.Id ? w.RequestedCountryId : w.InsistedCountryId);
    }

    protected void UpdateAvailableWars(IEnumerable<CountryWar> wars)
    {
      var availableWars = wars
        .Where(w => w.IntStartGameDate <= this.GameDateTime.ToInt() && (w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.StopRequesting));
      this.Wars.AddRange(availableWars);
    }
  }

  public class FarmerBattlerAiCharacter : FarmerAiCharacter
  {
    protected virtual bool CanWall => false;

    protected virtual bool CanSoldierForce => true;

    public FarmerBattlerAiCharacter(Character character) : base(character)
    {
    }

    protected virtual SoldierType FindSoldierType()
    {
      if (this.Wars.Any(w => w.Mode == CountryWarMode.Religion))
      {
        return SoldierType.LightApostle;
      }
      return SoldierType.Common;
    }

    protected override async Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      this.UpdateAvailableWars(wars);

      var isNeedSoldier = this.Character.SoldierNumber < Math.Max(this.Character.Leadership / 10, 20);
      var towns = await repo.Town.GetAllAsync();
      var command = new CharacterCommand
      {
        Id = 0,
        CharacterId = this.Character.Id,
        GameDateTime = this.GameDateTime,
        Type = CharacterCommandType.None,
      };

      if (this.Town.CountryId != this.Character.CountryId || ((this.Town.People < 3000 || this.Town.Security < 20) && isNeedSoldier && !this.CanSoldierForce))
      {
        // 徴兵のための都市に移動する
        await this.MoveToMyCountryTownAsync(repo, towns, t => t.People > 3000 && t.Security >= 20, t => t.People * t.Security, command);
        return command;
      }

      if (isNeedSoldier)
      {
        if (this.CanSoldierForce)
        {
          this.Town.People = (int)Math.Max(this.Town.People, this.Character.Leadership * Config.SoldierPeopleCost + 500);
          this.Town.Security = Math.Max(this.Town.Security, (short)(this.Character.Leadership / 10 + 1));
          await repo.SaveChangesAsync();
        }
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
        command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 3,
          NumberValue = 0,
        });
        return command;
      }

      if (this.Town.Id == this.Country.CapitalTownId || (this.Town.People > 3000 && this.Town.Security >= 40))
      {
        var currentDefenders = await repo.Town.GetDefendersAsync(this.Town.Id);
        if (!currentDefenders.Any())
        {
          command.Type = CharacterCommandType.Defend;
          return command;
        }
      }

      var targetCountryIds = await this.GetWarTargetCountriesAsync(repo, wars);
      if (targetCountryIds.Any())
      {
        var targetTowns = towns
          .GetAroundTowns(this.Town)
          .Where(t => targetCountryIds.Contains(t.CountryId));
        if (!targetTowns.Any())
        {
          // 攻撃先と隣接していない
          var canAttack = await this.MoveToMyCountryTownNextToCountryAsync(repo, towns, t => true, t => t.People * t.Security, targetCountryIds, command);
          if (!canAttack)
          {
            // 攻撃先と飛び地
            command.Type = CharacterCommandType.Training;
            command.Parameters.Add(new CharacterCommandParameter
            {
              Type = 1,
              NumberValue = 1,
            });
          }
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

          var targetTown = targetTownsData.OrderBy(td => td.Defenders.Count()).First();

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
            var canAttack = await this.MoveToMyCountryTownNextToCountryAsync(repo, towns, t => t.People > 3000 && t.Security >= 40, t => t.People * t.Security, readyWar.InsistedCountryId == this.Country.Id ? readyWar.RequestedCountryId : readyWar.InsistedCountryId, command);
            if (!canAttack)
            {
              // 攻撃先と飛び地
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
      this.Character.Strong = (short)Math.Max(current.ToInt() * 0.9f / 12 + 100, 100);
      this.Character.Leadership = 100;
      this.Character.Money = 1000000;
      this.Character.Rice = 1000000;
    }
  }

  public class FarmerCivilOfficialAiCharacter : FarmerAiCharacter
  {
    protected virtual bool CanSoldierForce => true;

    protected virtual SoldierType SoldierType => SoldierType.LightIntellect;

    public FarmerCivilOfficialAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "農民_文官";
      this.Character.Intellect = (short)Math.Max(current.ToInt() * 0.9f / 12 + 100, 100);
      this.Character.Leadership = 100;
      this.Character.Money = 1000000;
      this.Character.Rice = 1000000;
    }

    protected virtual void SetCommandOnNoWars(CharacterCommand command)
    {
      command.Type = CharacterCommandType.Wall;
    }

    protected override async Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      this.UpdateAvailableWars(wars);

      var towns = await repo.Town.GetAllAsync();
      var command = new CharacterCommand
      {
        Id = 0,
        CharacterId = this.Character.Id,
        GameDateTime = this.GameDateTime,
      };

      if (this.Town.Id != this.Country.CapitalTownId)
      {
        this.InputMoveToTown(this.Country.CapitalTownId, command);
        return command;
      }
      else
      {
        var targetCountryIds = await this.GetWarTargetCountriesAsync(repo, wars);
        if (targetCountryIds.Any())
        {
          var targetTowns = towns
            .GetAroundTowns(this.Town)
            .Where(t => targetCountryIds.Contains(t.CountryId));

          var currentTownDefenders = await repo.Town.GetDefendersAsync(this.Town.Id);
          var isDefending = currentTownDefenders.Any(d => d.Character.Id == this.Character.Id);
          if (isDefending && targetTowns.Any() && this.Character.SoldierNumber > 25)
          {
            // 攻撃
            var targetTownsData = new List<(Town Town, IEnumerable<Character> Defenders)>();
            foreach (var town in targetTowns)
            {
              var defenders = await repo.Town.GetDefendersAsync(town.Id);
              targetTownsData.Add(((Town)town, defenders.Select(td => td.Character)));
            }

            var targetTown = targetTownsData.OrderBy(td => td.Defenders.Count()).First();

            command.Parameters.Add(new CharacterCommandParameter
            {
              Type = 1,
              NumberValue = (int)targetTown.Town.Id,
            });
            command.Type = CharacterCommandType.Battle;
            return command;
          }
          else if ((await repo.System.GetAsync()).IsBattleRoyaleMode && !targetTowns.Any())
          {
            // 攻撃先と隣接していない、かつ全国戦争中
            var canAttack = await this.MoveToMyCountryTownNextToCountryAsync(repo, towns, t => true, t => t.People * t.Security, targetCountryIds, command);
            if (!canAttack)
            {
              // 攻撃先と飛び地
              command.Type = CharacterCommandType.Training;
              command.Parameters.Add(new CharacterCommandParameter
              {
                Type = 1,
                NumberValue = 2,
              });
            }
            return command;
          }

          else
          {
            if (this.Character.Money > 30000 &&
              ((isDefending && this.Character.SoldierNumber < 30) || (!isDefending && this.Character.SoldierNumber <= 0)))
            {
              // 兵を補充
              if (this.CanSoldierForce)
              {
                this.Town.People = (int)Math.Max(this.Town.People, this.Character.Leadership * Config.SoldierPeopleCost + 500);
                this.Town.Security = Math.Max(this.Town.Security, (short)(this.Character.Leadership / 10 + 1));
                await repo.SaveChangesAsync();
              }
              command.Parameters.Add(new CharacterCommandParameter
              {
                Type = 1,
                NumberValue = (int)this.SoldierType,
              });
              command.Parameters.Add(new CharacterCommandParameter
              {
                Type = 2,
                NumberValue = this.Character.Leadership,
              });
              command.Parameters.Add(new CharacterCommandParameter
              {
                Type = 3,
                NumberValue = 0,
              });
              command.Type = CharacterCommandType.Soldier;
              return command;
            }
            else if (!isDefending && this.Character.Money >= 30000)
            {
              // 守備してなければ守備
              command.Type = CharacterCommandType.Defend;
              return command;
            }
            else
            {
              // 壁塗り
              command.Type = CharacterCommandType.Wall;
              return command;
            }
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

  public class FarmerPatrollerAiCharacter : FarmerAiCharacter
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

    protected virtual void SetCommandOnNoWars(CharacterCommand command)
    {
      command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 1,
        NumberValue = 4,
      });
      command.Type = CharacterCommandType.Training;
    }

    protected override async Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      this.UpdateAvailableWars(wars);

      var towns = await repo.Town.GetAllAsync();
      var command = new CharacterCommand
      {
        Id = 0,
        CharacterId = this.Character.Id,
        GameDateTime = this.GameDateTime,
      };

      if (this.Town.Id != this.Country.CapitalTownId)
      {
        this.InputMoveToTown(this.Country.CapitalTownId, command);
        return command;
      }
      else
      {
        var availableWar = wars.FirstOrDefault(w => w.IntStartGameDate <= this.GameDateTime.ToInt() && (w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.StopRequesting));
        if (availableWar != null)
        {
          command.Type = CharacterCommandType.SuperSecurity;
          return command;
        }
        else if (this.Town.Security < 100)
        {
          command.Type = CharacterCommandType.Security;
          return command;
        }
        else
        {
          this.SetCommandOnNoWars(command);
          return command;
        }
      }
    }
  }

  public class FarmerEvangelistAiCharacter : FarmerAiCharacter
  {
    public FarmerEvangelistAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "農民_伝道師";
      this.Character.Popularity = (short)(100 + current.Year / 2.8f);
      this.Character.Money = 1000000;
      this.Character.Rice = 1000000;
    }

    protected override async Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      this.UpdateAvailableWars(wars);

      this.Character.Religion = this.Country.Religion;
      this.Character.From = this.Country.Religion == ReligionType.Buddhism ? CharacterFrom.Buddhism :
        this.Country.Religion == ReligionType.Confucianism ? CharacterFrom.Confucianism :
        this.Country.Religion == ReligionType.Taoism ? CharacterFrom.Taoism : CharacterFrom.Unknown;

      var towns = await repo.Town.GetAllAsync();
      var command = new CharacterCommand
      {
        Id = 0,
        CharacterId = this.Character.Id,
        GameDateTime = this.GameDateTime,
      };

      if (this.Town.Religion == this.Character.Religion)
      {
        command.Type = CharacterCommandType.Missionary;
      }
      else
      {
        var aroundTowns = towns.GetAroundTowns(this.Town);
        command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 1,
          NumberValue = (int)RandomService.Next(aroundTowns).Id,
        });
        command.Type = CharacterCommandType.Move;
      }

      return command;
    }
  }
}