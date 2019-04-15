using System;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Updates
{
  public class ThiefBattlerAiCharacter : TerroristBattlerAiCharacter
  {
    protected override SoldierType FindSoldierType()
    {
      if (this.Town.Technology >= 700)
      {
        return SoldierType.ThiefCommonC;
      }
      if (this.Town.Technology >= 500)
      {
        return SoldierType.ThiefCommonB;
      }
      if (this.Town.Technology >= 200)
      {
        return SoldierType.ThiefCommonA;
      }
      return SoldierType.LightInfantry;
    }

    public ThiefBattlerAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "蛮族_武将";
      this.Character.Strong = (short)(this.Character.Strong * 0.7f);
      this.Character.Leadership = 110;
    }
  }

  public class ThiefPatrollerAiCharacter : TerroristPatrollerAiCharacter
  {
    protected override DevelopModeType DevelopMode => DevelopModeType.Low;

    public ThiefPatrollerAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "蛮族_仁官";
      this.Character.Intellect = 100;
      this.Character.Leadership = 40;
    }
  }
}
