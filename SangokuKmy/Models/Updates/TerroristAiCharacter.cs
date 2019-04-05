using System;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using System.Collections.Generic;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Updates
{
  public class TerroristBattlerAiCharacter : WorkerAiCharacter
  {
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

    protected override DefendLevel NeedDefendLevel
    {
      get
      {
        if ((this.Town.People > 20000 && this.Town.Security > 50) || this.Town.Wall < 600)
        {
          return DefendLevel.NeedMyDefend;
        }
        if ((this.Town.People > 10000 && this.Town.Security > 50) || this.Town.Wall < 1000)
        {
          return DefendLevel.NeedThreeDefend;
        }
        if ((this.BorderTown != null && this.Town.Id == this.BorderTown.Id) || (this.Town.People > 5000 && this.Town.Security > 30) || this.Town.Wall < 1400)
        {
          return DefendLevel.NeedTwoDefend;
        }
        return DefendLevel.NeedAnyDefends;
      }
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "異民族_武将";
      this.Character.Strong = (short)(current.ToInt() * 1.08f / 12);
      this.Character.Leadership = 200;
      this.Character.Money = 99999999;
      this.Character.Rice = 99999999;
    }

    protected override async Task ActionAsync(MainRepository repo)
    {
      if (await this.InputDefendLoopAsync(repo, 5000))
      {
        return;
      }

      if (await this.InputDefendAsync(repo))
      {
        return;
      }

      if (await this.InputBattleAsync(repo))
      {
        return;
      }

      if (await this.InputMoveToBorderTownAsync(repo))
      {
        return;
      }

      if (this.InputSoldierTraining())
      {
        return;
      }

      this.InputTraining(TrainingType.Strong);
    }
  }

  public class TerroristRyofuAiCharacter : TerroristBattlerAiCharacter
  {
    public TerroristRyofuAiCharacter(Character character) : base(character)
    {
    }

    protected override SoldierType FindSoldierType()
    {
      if (this.Town.Technology >= 950)
      {
        return SoldierType.TerroristCommonC;
      }
      if (this.Town.Technology >= 700)
      {
        return SoldierType.TerroristCommonB;
      }
      if (this.Town.Technology >= 400)
      {
        return SoldierType.TerroristCommonA;
      }
      return SoldierType.LightInfantry;
    }

    public override void Initialize(GameDateTime current)
    {
      base.Initialize(current);
      this.Character.Name = "異民族_呂布";
      this.Character.Strong = (short)(current.ToInt() * 0.9f / 12);
      this.Character.Leadership = 90;
    }
  }

  public class TerroristCivilOfficialAiCharacter : WorkerAiCharacter
  {
    public TerroristCivilOfficialAiCharacter(Character character) : base(character)
    {
    }

    protected override SoldierType FindSoldierType()
    {
      if (this.Town.Technology >= 800)
      {
        return SoldierType.Intellect;
      }
      if (this.Town.Technology >= 500)
      {
        return SoldierType.LightIntellect;
      }
      return SoldierType.TerroristCommonA;
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "異民族_文官";
      this.Character.Strong = 10;
      this.Character.Intellect = (short)(current.ToInt() * 0.81f / 12);
      this.Character.Leadership = 100;
      this.Character.Money = 99999999;
      this.Character.Rice = 99999999;
    }

    protected override async Task ActionAsync(MainRepository repo)
    {
      if (await this.InputDefendAsync(repo, DefendLevel.NeedAnyDefends))
      {
        return;
      }

      if (await this.InputBattleAsync(repo))
      {
        return;
      }

      if (await this.InputMoveToBorderTownAsync(repo))
      {
        return;
      }

      if (this.InputSoldierTraining())
      {
        return;
      }

      if (this.InputDevelop())
      {
        return;
      }

      this.InputTraining(TrainingType.Intellect);
    }
  }

  public class TerroristPatrollerAiCharacter : WorkerAiCharacter
  {
    public TerroristPatrollerAiCharacter(Character character) : base(character)
    {
    }

    protected override SoldierType FindSoldierType()
    {
      if (this.Town.Technology >= 800)
      {
        return SoldierType.Intellect;
      }
      if (this.Town.Technology >= 500)
      {
        return SoldierType.LightIntellect;
      }
      return SoldierType.TerroristCommonA;
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "異民族_仁官";
      this.Character.Intellect = (short)(current.ToInt() * 0.8f / 12);
      this.Character.Popularity = 300;
      this.Character.Leadership = 100;
      this.Character.Money = 99999999;
      this.Character.Rice = 99999999;
    }

    protected override async Task ActionAsync(MainRepository repo)
    {
      if (await this.InputMoveToBorderTownAsync(repo))
      {
        return;
      }

      if (this.InputSecurity())
      {
        return;
      }

      if (await this.InputDefendLoopAsync(repo, 20000))
      {
        return;
      }

      if (await this.InputDefendAsync(repo, DefendLevel.NeedMyDefend))
      {
        return;
      }

      if (this.InputSoldierTraining())
      {
        return;
      }

      if (this.InputDevelopOnBorderOrMain())
      {
        return;
      }

      if (this.InputWallDevelop())
      {
        return;
      }

      if (await this.InputMoveToMainTownAsync(repo))
      {
        return;
      }

      this.InputTraining(TrainingType.Popularity);
    }
  }

  public class TerroristMainPatrollerAiCharacter : TerroristPatrollerAiCharacter
  {
    public TerroristMainPatrollerAiCharacter(Character character) : base(character)
    {
    }

    protected override async Task ActionAsync(MainRepository repo)
    {
      if (await this.InputMoveToMainTownAsync(repo))
      {
        return;
      }

      if (this.InputSecurity())
      {
        return;
      }

      if (await this.InputDefendLoopAsync(repo, 20000))
      {
        return;
      }

      if (await this.InputDefendAsync(repo, DefendLevel.NeedMyDefend))
      {
        return;
      }

      if (this.InputSoldierTraining())
      {
        return;
      }

      if (this.InputDevelopOnBorderOrMain())
      {
        return;
      }

      this.InputTraining(TrainingType.Popularity);
    }
  }

  public class OldTerroristBattlerAiCharacter : FarmerBattlerAiCharacter
  {
    protected override bool CanSoldierForce => false;

    public OldTerroristBattlerAiCharacter(Character character) : base(character)
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

  public class OldTerroristWallBattlerAiCharacter : OldTerroristBattlerAiCharacter
  {
    protected override bool CanWall => true;

    public OldTerroristWallBattlerAiCharacter(Character character) : base(character)
    {
    }

    protected override SoldierType FindSoldierType()
    {
      return SoldierType.Seiran;
    }
  }

  public class OldTerroristRyofuAiCharacter : OldTerroristBattlerAiCharacter
  {
    protected override bool CanWall => false;

    protected override SoldierType FindSoldierType()
    {
      if (this.Town.Technology >= 950)
      {
        return SoldierType.TerroristCommonC;
      }
      if (this.Town.Technology >= 700)
      {
        return SoldierType.TerroristCommonB;
      }
      if (this.Town.Technology >= 400)
      {
        return SoldierType.TerroristCommonA;
      }
      return SoldierType.LightInfantry;
    }

    public OldTerroristRyofuAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      base.Initialize(current);
      this.Character.Name = "異民族_呂布";
      this.Character.Strong = 30;
    }
  }

  public class OldTerroristCivilOfficialAiCharacter : FarmerCivilOfficialAiCharacter
  {
    protected override bool CanSoldierForce => false;

    protected override SoldierType SoldierType => SoldierType.Intellect;

    public OldTerroristCivilOfficialAiCharacter(Character character) : base(character)
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
      var v = this.GameDateTime.Month % 2;
      if (v == 0)
      {
        command.Type = CharacterCommandType.Wall;
      }
      else
      {
        command.Type = CharacterCommandType.Technology;
      }

      if (command.Type == CharacterCommandType.Wall && this.Town.Wall >= this.Town.WallMax)
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

  public class OldTerroristPatrollerAiCharacter : FarmerPatrollerAiCharacter
  {
    public OldTerroristPatrollerAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      base.Initialize(current);
      this.Character.Name = "異民族_仁官";
      this.Character.Intellect = 200;
      this.Character.Popularity = (short)(this.Character.Popularity * 1.6f);
    }

    protected override void SetCommandOnNoWars(CharacterCommand command)
    {
      if (this.Town.Security < 70)
      {
        command.Type = CharacterCommandType.SuperSecurity;
      }
      else if (this.Town.Technology < this.Town.TechnologyMax)
      {
        command.Type = CharacterCommandType.Technology;
      }
      else if (this.Town.Wall < this.Town.WallMax / 3)
      {
        command.Type = CharacterCommandType.Wall;
      }
      else if (this.Town.Wall < this.Town.WallMax / 2)
      {
        command.Type = CharacterCommandType.Wall;
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
