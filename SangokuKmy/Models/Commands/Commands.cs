using System;
using System.Collections.Generic;
using SangokuKmy.Models.Data.Entities;
using System.Linq;
using SangokuKmy.Common;
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
      new WallGuardCommand(),
      new SecurityCommand(),
      new SuperSecurityCommand(),
      new AgricultureMaxCommand(),
      new CommercialMaxCommand(),
      new WallMaxCommand(),
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
    };

    public static Optional<Command> Get(CharacterCommandType type)
    {
      return commands.SingleOrDefault(c => c.Type == type).ToOptional();
    }
  }
}
