using System;
using System.Threading.Tasks;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Common;
using System.Collections.Generic;
using SangokuKmy.Models.Data.ApiEntities;
using System.Linq;

namespace SangokuKmy.Models.Updates
{
  public abstract class SecretaryAiCharacter : AiCharacter
  {
    public SecretaryAiCharacter(Character character) : base(character)
    {
    }
  }

  public class SecretaryPatrollerAiCharacter : SecretaryAiCharacter
  {
    public SecretaryPatrollerAiCharacter(Character character) : base(character)
    {
    }

    protected override async Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      var command = new CharacterCommand
      {
        Id = 0,
        CharacterId = this.Character.Id,
        GameDateTime = this.GameDateTime,
      };
      command.Type = CharacterCommandType.Security;
      return command;
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "政務官_仁官";
      this.Character.Popularity = (short)(100 + current.Year / 3.2f);
      this.Character.Money = 1000000;
      this.Character.Rice = 1000000;
    }
  }

  public class SecretaryGatherAiCharacter : SecretaryAiCharacter
  {
    public SecretaryGatherAiCharacter(Character character) : base(character)
    {
    }

    protected override async Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      var command = new CharacterCommand
      {
        Id = 0,
        CharacterId = this.Character.Id,
        GameDateTime = this.GameDateTime,
      };
      command.Type = CharacterCommandType.Gather;
      return command;
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "政務官_集合官";
      this.Character.Popularity = (short)(100 + current.Year / 3.2f);
      this.Character.Money = 1000000;
      this.Character.Rice = 1000000;
    }
  }

  public class SecretaryPioneerAiCharacter : SecretaryAiCharacter
  {
    public SecretaryPioneerAiCharacter(Character character) : base(character)
    {
    }

    protected override async Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      var command = new CharacterCommand
      {
        Id = 0,
        CharacterId = this.Character.Id,
        GameDateTime = this.GameDateTime,
      };
      if (this.GameDateTime.Month % 2 == 0 && this.Town.Commercial < this.Town.CommercialMax)
      {
        command.Type = CharacterCommandType.Commercial;
      }
      else
      {
        command.Type = CharacterCommandType.Agriculture;
      }
      return command;
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "政務官_農商官";
      this.Character.Intellect = (short)(100 + current.Year / 3.3f);
      this.Character.Money = 1000000;
      this.Character.Rice = 1000000;
    }
  }

  public class SecretaryDefenderAiCharacter : SecretaryAiCharacter
  {
    public SecretaryDefenderAiCharacter(Character character) : base(character)
    {
    }

    protected virtual SoldierType FindSoldierType()
    {
      if (this.Town.Technology >= 700)
      {
        return SoldierType.HeavyCavalry;
      }
      if (this.Town.Technology >= 600)
      {
        return SoldierType.HeavyInfantry;
      }
      if (this.Town.Technology >= 300)
      {
        return SoldierType.LightCavalry;
      }
      if (this.Town.Technology >= 200)
      {
        return SoldierType.LightInfantry;
      }
      return SoldierType.Common;
    }

    protected virtual int GetSoldierNumber()
    {
      if (this.Character.AliasId == DefenderSecretaryType.SoldierAll)
      {
        return this.Character.Leadership;
      }
      if (this.Character.AliasId == DefenderSecretaryType.SoldierLast9)
      {
        return Math.Max(1, (this.Character.Leadership + 1) / 10 * 10 - 1);
      }
      if (this.Character.AliasId == DefenderSecretaryType.Soldier9)
      {
        return 9;
      }
      if (this.Character.AliasId == DefenderSecretaryType.Soldier1)
      {
        return 1;
      }
      return 1;
    }

    protected override async Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      var command = new CharacterCommand
      {
        Id = 0,
        CharacterId = this.Character.Id,
        GameDateTime = this.GameDateTime,
      };

      if (this.Town.CountryId == this.Character.CountryId)
      {
        var defenders = await repo.Town.GetDefendersAsync(this.Town.Id);
        if (this.Character.SoldierNumber > 0 && !defenders.Any(d => d.Character.Id == this.Character.Id))
        {
          command.Type = CharacterCommandType.Defend;
          return command;
        }

        if (this.Character.SoldierNumber < this.GetSoldierNumber())
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
            NumberValue = this.GetSoldierNumber(),
          });
          command.Parameters.Add(new CharacterCommandParameter
          {
            Type = 3,
            NumberValue = 0,
          });
          return command;
        }

        if (!defenders.Any() || defenders.First().Character.Id != this.Character.Id)
        {
          command.Type = CharacterCommandType.Defend;
          return command;
        }

        if (this.Character.Proficiency < 100)
        {
          command.Type = CharacterCommandType.SoldierTraining;
          return command;
        }
      }

      command.Type = CharacterCommandType.Training;
      command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 1,
        NumberValue = 1,
      });
      return command;
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "政務官_農商官";
      this.Character.Intellect = (short)(100 + current.Year / 3.3f);
      this.Character.Money = 1000000;
      this.Character.Rice = 1000000;
    }
  }
}
