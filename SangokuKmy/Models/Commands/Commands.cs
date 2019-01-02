using System;
using System.Collections.Generic;
using SangokuKmy.Models.Data.Entities;
using System.Linq;
using SangokuKmy.Common;
namespace SangokuKmy.Models.Commands
{
  public static class Commands
  {
    private static readonly IReadOnlyCollection<Command> commands = new Command[]
    {
      new AgricultureCommand(),
      new CommercialCommand(),
      new TechnologyCommand(),
      new WallCommand(),
      new WallGuardCommand(),
      new SecurityCommand(),
      new AgricultureMaxCommand(),
      new CommercialMaxCommand(),
      new WallMaxCommand(),
      new SoldierCommand(),
      new DefendCommand(),
      new TrainingCommand(),
    };

    public static Optional<Command> Get(CharacterCommandType type)
    {
      return commands.SingleOrDefault(c => c.Type == type).ToOptional();
    }
  }
}
