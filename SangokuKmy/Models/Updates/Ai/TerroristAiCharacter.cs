using System;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using System.Collections.Generic;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Updates.Ai
{
  public class TerroristBattlerAiCharacter : WorkerAiCharacter
  {
    protected override UnitPolicyLevel UnitLevel => UnitPolicyLevel.NotCare;

    protected override UnitGatherPolicyLevel UnitGatherLevel => UnitGatherPolicyLevel.Always;

    protected override DefendSeiranLevel NeedDefendSeiranLevel => DefendSeiranLevel.HalfSeirans;

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
      this.Character.Strong = (short)Math.Max(current.ToInt() * 0.81f / 12 + 60, 100);
      this.Character.Leadership = (short)Math.Min(current.ToInt() * 0.3f / 12 + 83, 150.0f);
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
      this.Character.Strong = (short)Math.Max(70, this.Character.Strong - 100);
      this.Character.Leadership = 90;
    }
  }

  public class TerroristWallBattlerAiCharacter : TerroristBattlerAiCharacter
  {
    protected override AttackMode AttackType => AttackMode.BreakWall;

    protected override DefendSeiranLevel NeedDefendSeiranLevel => DefendSeiranLevel.NotCare;

    protected override SoldierType FindSoldierType()
    {
      if (this.Town.Technology >= 500)
      {
        return SoldierType.Seiran;
      }
      if (this.Town.Technology >= 300)
      {
        return SoldierType.TerroristCommonB;
      }
      if (this.Town.Technology >= 100)
      {
        return SoldierType.TerroristCommonA;
      }
      return SoldierType.LightInfantry;
    }

    public TerroristWallBattlerAiCharacter(Character character) : base(character)
    {
    }
  }

  public class TerroristCivilOfficialAiCharacter : WorkerAiCharacter
  {
    protected override UnitPolicyLevel UnitLevel => UnitPolicyLevel.NotCare;

    protected override UnitGatherPolicyLevel UnitGatherLevel => UnitGatherPolicyLevel.Always;

    protected override DefendSeiranLevel NeedDefendSeiranLevel => DefendSeiranLevel.HalfSeirans;

    public TerroristCivilOfficialAiCharacter(Character character) : base(character)
    {
    }

    protected override SoldierType FindSoldierType()
    {
      if (this.Town.Technology >= 900)
      {
        return SoldierType.IntellectHeavyCavalry;
      }
      if (this.Town.Technology >= 500)
      {
        return SoldierType.LightIntellect;
      }
      if (this.Town.Technology >= 200)
      {
        return SoldierType.IntellectCommon;
      }
      return SoldierType.TerroristCommonA;
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "異民族_文官";
      this.Character.Strong = 10;
      this.Character.Intellect = (short)Math.Max(current.ToInt() * 0.8f / 12 + 60, 100);
      this.Character.Leadership = 90;
      this.Character.Money = 99999999;
      this.Character.Rice = 99999999;
    }

    protected override async Task ActionAsync(MainRepository repo)
    {
      if (await this.InputDefendLoopAsync(repo, 8000))
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

      if (this.InputDevelop())
      {
        return;
      }

      this.InputTraining(TrainingType.Intellect);
    }
  }

  public class TerroristPatrollerAiCharacter : WorkerAiCharacter
  {
    protected enum DevelopModeType
    {
      Normal,
      Low,
    }

    protected virtual DevelopModeType DevelopMode => DevelopModeType.Normal;

    public TerroristPatrollerAiCharacter(Character character) : base(character)
    {
    }

    protected override SoldierType FindSoldierType()
    {
      if (this.Town.Technology >= 900)
      {
        return SoldierType.LightIntellect;
      }
      if (this.Town.Technology >= 500)
      {
        return SoldierType.IntellectCommon;
      }
      return SoldierType.TerroristCommonA;
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "異民族_仁官";
      this.Character.Intellect = (short)Math.Min(Math.Max(current.ToInt() * 1.4f / 12, 120), 240);
      this.Character.Popularity = 140;
      this.Character.Leadership = 30;
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

      if (this.DevelopMode == DevelopModeType.Normal)
      {
        if (this.InputDevelopOnBorderOrMain())
        {
          return;
        }
      }
      else
      {
        if (this.InputDevelopOnBorderOrMainLow())
        {
          return;
        }
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
}
