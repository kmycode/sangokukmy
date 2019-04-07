using System;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Updates
{
  public class ThiefBattlerAiCharacter : TerroristBattlerAiCharacter
  {
    public ThiefBattlerAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "蛮族_武将";
    }
  }

  public class ThiefPatrollerAiCharacter : TerroristPatrollerAiCharacter
  {
    public ThiefPatrollerAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "蛮族_仁官";
    }
  }
}
