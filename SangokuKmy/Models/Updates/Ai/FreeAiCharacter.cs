using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;

namespace SangokuKmy.Models.Updates.Ai
{
  public abstract class FreeAiCharacter : AiCharacter
  {
    protected FreeAiCharacter(Character character) : base(character)
    {
    }

    protected override async Task<CharacterCommand> GetCommandInnerAsync(MainRepository repo, IEnumerable<CountryWar> wars)
    {
      return (await this.ActionAsync(repo)) ?? new CharacterCommand
      {
        Type = CharacterCommandType.None,
      };
    }

    protected override async Task<Optional<CharacterCommand>> GetCommandAsNoCountryAsync(MainRepository repo)
    {
      return await this.ActionAsync(repo).ToOptionalAsync();
    }

    protected abstract Task<CharacterCommand> ActionAsync(MainRepository repo);
  }

  public class FreeEvangelistAiCharacter : FreeAiCharacter
  {
    public FreeEvangelistAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Religion = RandomService.Next(new ReligionType[] { ReligionType.Buddhism, ReligionType.Confucianism, ReligionType.Taoism, });
      this.Character.Name = "伝道師";
      this.Character.Intellect = (short)(100 + current.Year / 2.9f);
      this.Character.Money = 1000000;
      this.Character.Rice = 1000000;
    }

    protected override async Task<CharacterCommand> ActionAsync(MainRepository repo)
    {
      var countries = await repo.Country.GetAllAsync();
      if (!countries.Any(c => !c.HasOverthrown && c.Religion == this.Character.Religion))
      {
        return default;
      }

      var command = new CharacterCommand
      {
        CharacterId = this.Character.Id,
        GameDateTime = this.GameDateTime,
      };

      if (this.GameDateTime.Year % 8 == 0 && this.Town.Religion == this.Character.Religion)
      {
        var towns = await repo.Town.GetAllAsync();
        var aroundTowns = towns.GetAroundTowns(this.Town);
        var targetTown = RandomService.Next(aroundTowns);

        command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 1,
          NumberValue = (int)targetTown.Id,
        });
        command.Type = CharacterCommandType.Move;
      }
      else
      {
        command.Type = CharacterCommandType.MissionarySelf;
      }

      return command;
    }
  }
}
