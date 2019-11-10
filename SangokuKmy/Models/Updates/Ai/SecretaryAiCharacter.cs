using System;
using System.Threading.Tasks;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Common;
using System.Collections.Generic;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Updates.Ai
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

  public class SecretaryLeaderAiCharacter : SecretaryGatherAiCharacter
  {
    public SecretaryLeaderAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      base.Initialize(current);
      this.Character.Name = "政務官_部隊長";
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

  public class SecretaryScouterAiCharacter : SecretaryAiCharacter
  {
    public SecretaryScouterAiCharacter(Character character) : base(character)
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
      command.Type = CharacterCommandType.Spy;
      command.Parameters.Add(new CharacterCommandParameter
      {
        Type = 1,
        NumberValue = (int)this.Character.TownId,
      });
      return command;
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "政務官_斥候";
    }
  }
}
