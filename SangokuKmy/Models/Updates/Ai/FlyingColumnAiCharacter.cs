using System;
using System.Threading.Tasks;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;

namespace SangokuKmy.Models.Updates.Ai
{
  public class FlyingColumnAiCharacter : WorkerAiCharacter
  {
    private AiCharacterManagement Management { get; set; }

    protected override AttackMode AttackType => AttackMode.Normal;

    protected override bool IsWarEmpty => true;

    protected override DefendLevel NeedDefendLevel => this.Management.Action == AiCharacterAction.Defend ? DefendLevel.NeedMyDefend : DefendLevel.NotCare;

    protected override DefendSeiranLevel NeedDefendSeiranLevel => DefendSeiranLevel.NotCare;

    protected override SoldierType FindSoldierType()
    {
      if (this.Management != null)
      {
        return this.Management.SoldierType == AiCharacterSoldierType.Infancty ? SoldierType.HeavyInfantry :
          this.Management.SoldierType == AiCharacterSoldierType.Cavalry ? SoldierType.HeavyCavalry :
          this.Management.SoldierType == AiCharacterSoldierType.Archer ? SoldierType.StrongCrossbow : SoldierType.Common;
      }
      return SoldierType.Common;
    }

    protected override int GetSoldierNumberMax(SoldierType type)
    {
      return this.Character.Leadership;
    }

    public FlyingColumnAiCharacter(Character character) : base(character)
    {
    }

    public override void Initialize(GameDateTime current)
    {
      this.Character.Name = "別動隊";
      this.Character.Strong = (short)(100 + current.Year / 3.3f);
      this.Character.Intellect = (short)(100 + current.Year / 3.3f);
      this.Character.Leadership = 100;
      this.Character.Money = 12_0000;
      this.Character.Rice = 12_0000;
    }

    protected override async Task ActionAsync(MainRepository repo)
    {
      var management = await repo.Character.GetManagementByAiCharacterIdAsync(this.Character.Id);
      if (management.HasData)
      {
        var m = management.Data;
        this.Management = m;

        var holder = await repo.Character.GetByIdAsync(m.HolderCharacterId);
        if (holder.HasData)
        {
          if (holder.Data.HasRemoved)
          {
            this.Character.DeleteTurn = (short)Config.DeleteTurns;
            return;
          }
          else if (holder.Data.DeleteTurn > 0)
          {
            // 主人が放置削除中は自分も何もしない
            return;
          }
        }
        else
        {
          this.Character.DeleteTurn = (short)Config.DeleteTurns;
          return;
        }

        if (this.Country == null || this.Character.CountryId == 0 || holder.Data.CountryId == 0)
        {
          // 所持者の国が消えたらこっちも削除する
          this.Character.DeleteTurn = (short)Config.DeleteTurns;
          return;
        }

        this.Character.Money = 100_0000;
        this.Character.Rice = 100_0000;
        var r = false;
        if (m.Action == AiCharacterAction.Assault || m.Action == AiCharacterAction.Attack)
        {
          r = r || await this.InputMoveToBorderTownAsync(repo) || await this.InputBattleAsync(repo);
        }
        else if (m.Action == AiCharacterAction.Defend)
        {
          r = r || await this.InputMoveToBorderTownAsync(repo) || await this.InputDefendAsync(repo);
        }
        else if (m.Action == AiCharacterAction.DomesticAffairs)
        {
          r = r || await this.InputMoveToDevelopTownForceAsync(repo) || this.InputDevelop();
        }

        if (!r)
        {
          this.InputPolicyForce();
        }
      }
    }
  }
}
