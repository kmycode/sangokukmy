using System;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using System.Collections.Generic;
using SangokuKmy.Models.Common;

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
      if (this.Town.Technology >= 800)
      {
        return SoldierType.TerroristCommonC;
      }
      if (this.Town.Technology >= 500)
      {
        return SoldierType.TerroristCommonB;
      }
      if (this.Town.Technology >= 200)
      {
        return SoldierType.TerroristCommonA;
      }
      return SoldierType.LightInfantry;
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

  public class TerroristRyofuAiCharacter : TerroristBattlerAiCharacter
  {
    protected override bool CanWall => false;

    public TerroristRyofuAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      base.Initialize(current);
      this.Character.Name = "異民族_呂布";
      this.Character.Strong = 1;
    }
  }

  public class TerroristCivilOfficialAiCharacter : FarmerCivilOfficialAiCharacter
  {
    protected override bool CanSoldierForce => false;

    protected override SoldierType SoldierType => SoldierType.Intellect;

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

      if (command.Type == CharacterCommandType.Wall && this.Town.Wall >= this.Town.WallMax)
      {
        command.Type = CharacterCommandType.WallGuard;
      }
      if (command.Type == CharacterCommandType.WallGuard && this.Town.WallGuard >= this.Town.WallGuardMax)
      {
        command.Type = CharacterCommandType.Technology;
      }
      if (command.Type == CharacterCommandType.Technology && this.Town.Technology >= this.Town.TechnologyMax)
      {
        command.Type = CharacterCommandType.TownBuilding;
      }
      if (command.Type == CharacterCommandType.TownBuilding && this.Town.TownBuildingValue >= Config.TownBuildingMax)
      {
        command.Type = CharacterCommandType.Training;
        command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 1,
          NumberValue = 2,
        });
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
      this.Character.Intellect = 120;
      this.Character.Popularity = (short)(this.Character.Popularity * 1.2f);
    }

    protected override void SetCommandOnNoWars(CharacterCommand command)
    {
      if (this.Town.Technology < this.Town.TechnologyMax)
      {
        command.Type = CharacterCommandType.Technology;
      }
      else if (this.Town.Wall < this.Town.WallMax)
      {
        command.Type = CharacterCommandType.Wall;
      }
      else if (this.Town.TownBuildingValue < Config.TownBuildingMax * 2 / 3)
      {
        command.Type = CharacterCommandType.TownBuilding;
      }
      else
      {
        command.Type = CharacterCommandType.Training;
        command.Parameters.Add(new CharacterCommandParameter
        {
          Type = 1,
          NumberValue = 4,
        });
      }
    }
  }
}
