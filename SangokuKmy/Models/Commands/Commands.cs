using System;
using System.Collections.Generic;
using SangokuKmy.Models.Data.Entities;
using System.Linq;
namespace SangokuKmy.Models.Commands
{
  public static class Commands
  {
    private static readonly IReadOnlyCollection<Command> commands = new Command[]
    {
      new AgricultureCommand(),
    };

    public static Command Get(CharacterCommandType type)
    {
      return commands.Single(c => c.Type == type);
    }
  }
}
