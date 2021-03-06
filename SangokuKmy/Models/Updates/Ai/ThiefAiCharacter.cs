﻿using System;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Updates.Ai
{
  public class ThiefBattlerAiCharacter : TerroristBattlerAiCharacter
  {
    protected override bool IsWarAll => true;

    protected override bool IsWarAllAvoidLowCharacters => true;

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
      base.Initialize(current);
      this.Character.Name = "蛮族_武将";
      this.Character.Strong = (short)(this.Character.Strong * 0.79f);
      this.Character.Leadership = 80;
    }
  }

  public class ThiefWallBattlerAiCharacter : ThiefBattlerAiCharacter
  {
    protected override AttackMode AttackType => AttackMode.GetTown;

    public ThiefWallBattlerAiCharacter(Character character) : base(character)
    {
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
      base.Initialize(current);
      this.Character.Name = "蛮族_仁官";
      this.Character.Intellect = 100;
      this.Character.Leadership = 40;
    }
  }
}
