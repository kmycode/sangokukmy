using System;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Updates
{
  public class TerroristBattlerAiCharacter : FarmerBattlerAiCharacter
  {
    protected override bool CanSoldierForce => false;

    public TerroristBattlerAiCharacter(Character character) : base(character)
    {
    }

    protected override SoldierType FindSoldierType()
    {
      if (this.Town.Technology >= 900)
      {
        return SoldierType.RepeatingCrossbow;
      }
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
      if (this.Town.Technology >= 100)
      {
        return SoldierType.LightInfantry;
      }
      return SoldierType.Common;
    }

    public override void Initialize(GameDateTime current)
    {
      base.Initialize(current);
      this.Character.Name = "異民族_武将";
      this.Character.Strong = (short)(this.Character.Strong * 1.4f);
      this.Character.Leadership = 170;
      this.Character.Money = 99999999;
    }
  }

  public class TerroristWallBattlerAiCharacter : TerroristBattlerAiCharacter
  {
    protected override bool CanWall => true;

    public TerroristWallBattlerAiCharacter(Character character) : base(character)
    {
    }

    protected override SoldierType FindSoldierType()
    {
      return SoldierType.Seiran;
    }
  }

  public class TerroristCivilOfficialAiCharacter : FarmerCivilOfficialAiCharacter
  {
    public TerroristCivilOfficialAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      base.Initialize(current);
      this.Character.Name = "異民族_文官";
      this.Character.Intellect = (short)(this.Character.Intellect * 1.2f);
    }

    protected override void SetCommandOnNoWars(CharacterCommand command)
    {
      var v = this.GameDateTime.Month % 3;
      if (v == 0)
      {
        command.Type = CharacterCommandType.Wall;
      }
      else if (v == 1)
      {
        command.Type = CharacterCommandType.WallGuard;
      }
      else
      {
        command.Type = CharacterCommandType.Technology;
      }
    }
  }

  public class TerroristPatrollerAiCharacter : FarmerPatrollerAiCharacter
  {
    public TerroristPatrollerAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      base.Initialize(current);
      this.Character.Name = "異民族_仁官";
      this.Character.Popularity = (short)(this.Character.Popularity * 1.2f);
    }
  }
}
