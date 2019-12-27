using System;
using System.Collections.Generic;
using SangokuKmy.Models.Data.Entities;
using System.Linq;
using SangokuKmy.Common;
using SangokuKmy.Models.Data;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Commands
{
  public static class Commands
  {
    public static Command EmptyCommand { get; } = new EmptyCommand();

    private static readonly IReadOnlyCollection<Command> commands = new Command[]
    {
      new EmptyCommand(),
      new AgricultureCommand(),
      new CommercialCommand(),
      new TechnologyCommand(),
      new WallCommand(),
      new SecurityCommand(),
      new SuperSecurityCommand(),
      // new AgricultureMaxCommand(),
      // new CommercialMaxCommand(),
      // new WallMaxCommand(),
      new TownBuildingCommand(),
      new SoldierCommand(),
      new SoldierTrainingCommand(),
      new DefendCommand(),
      new BattleCommand(),
      new GatherCommand(),
      new PromotionCommand(),
      new MoveCommand(),
      new RiceCommand(),
      new TrainingCommand(),
      new JoinCommand(),
      new SafeInCommand(),
      new SafeOutCommand(),
      new AddSecretaryCommand(),
      new EditSecretaryCommand(),
      new EditSecretaryToTownCommand(),
      new RemoveSecretaryCommand(),
      // new SoldierResearchCommand(),
      new PolicyCommand(),
      new GetFormationCommand(),
      new ChangeFormationCommand(),
      // new ResearchFormationCommand(),
      new BuyItemCommand(),
      new SellItemCommand(),
      new HandOverItemCommand(),
      new TownPatrolCommand(),
      new TownInvestCommand(),
      new UseItemCommand(),
      new GenerateItemCommand(),
      new PeopleIncreaseCommand(),
      new PeopleDecreaseCommand(),
      new SoldierTrainingAllCommand(),
      new SpyCommand(),
      new ExamineCommand(),
      new BuildSubBuildingCommand(),
      new RemoveSubBuildingCommand(),
    };

    public static Optional<Command> Get(CharacterCommandType type)
    {
      return commands.SingleOrDefault(c => c.Type == type).ToOptional();
    }

    public static async Task<bool> ExecuteAsync(CharacterCommandType type, MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> parameters, CommandSystemData game)
    {
      var cmd = Get(type);
      if (cmd.HasData)
      {
        await cmd.Data.ExecuteAsync(repo, character, parameters, game);
        return true;
      }
      return false;
    }
  }
}
