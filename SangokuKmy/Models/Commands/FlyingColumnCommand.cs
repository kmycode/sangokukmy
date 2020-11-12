using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Models.Updates.Ai;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  public class AddFlyingColumnCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.AddFlyingColumn;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync($"別動隊を雇おうとしましたが、あなたの国はすでに滅亡しているか無所属です");
        return;
      }
      var country = countryOptional.Data;

      if (character.Money < 10000)
      {
        await game.CharacterLogAsync($"別動隊を雇おうとしましたが、金が足りません");
        return;
      }

      var ais = await repo.Character.GetManagementByHolderCharacterIdAsync(character.Id);
      if (ais.Any())
      {
        await game.CharacterLogAsync($"別動隊を雇おうとしましたが、すでに雇用しています");
        return;
      }

      var system = await repo.System.GetAsync();
      var ai = AiCharacterFactory.Create(CharacterAiType.FlyingColumn);
      ai.Initialize(game.GameDateTime);
      ai.Character.CountryId = character.CountryId;
      ai.Character.TownId = country.CapitalTownId;
      ai.Character.LastUpdated = character.LastUpdated.AddSeconds(Config.UpdateTime + 10);
      if (ai.Character.LastUpdated > system.CurrentMonthStartDateTime.AddSeconds(Config.UpdateTime))
      {
        ai.Character.LastUpdatedGameDate = game.GameDateTime.NextMonth();
      }
      else
      {
        ai.Character.LastUpdatedGameDate = game.GameDateTime;
      }
      await repo.Character.AddAsync(ai.Character);

      await repo.SaveChangesAsync();
      await AiService.SetIconAsync(repo, ai.Character);
      ai.Character.Name = character.Name + "_" + ai.Character.Name + ai.Character.Id;
      ai.Character.Strong = (short)(character.Strong * 0.8f);
      ai.Character.Intellect = (short)(character.Intellect * 0.8f);
      ai.Character.Leadership = (short)(character.Leadership * 0.8f);
      ai.Character.Popularity = (short)(character.Popularity * 0.8f);

      var management = new AiCharacterManagement
      {
        Action = AiCharacterAction.None,
        SoldierType = AiCharacterSoldierType.Default,
        CharacterId = ai.Character.Id,
        HolderCharacterId = character.Id,
        TargetTownId = 0,
      };
      await repo.Character.AddManagementAsync(management);

      character.Money -= 10000;
      character.SkillPoint++;
      character.Contribution += 100;
      character.AddLeadershipEx(50);

      var limit = system.GameDateTime.AddMonth(72);

      await game.CharacterLogAsync($"別動隊 <character>{ai.Character.Name}</character> を雇いました。雇用期限は <num>{limit.Year}</num> 年 <num>{limit.Month}</num> 月です");
      await game.MapLogAsync(EventType.SecretaryAdded, $"<country>{country.Name}</country> の <character>{character.Name}</character> は、新しく別動隊 <character>{ai.Character.Name}</character> を雇いました", false);
      await CharacterService.StreamCharacterAsync(repo, ai.Character);
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(management), character.Id);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }

  public class CustomizeFlyingColumnCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.CustomizeFlyingColumn;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var idParam = options.FirstOrDefault(p => p.Type == 1);
      var actionParam = options.FirstOrDefault(p => p.Type == 2);
      var soldierTypeParam = options.FirstOrDefault(p => p.Type == 3);
      var targetTownParam = options.FirstOrDefault(p => p.Type == 4);
      if (idParam == null || actionParam == null)
      {
        await game.CharacterLogAsync($"別働隊に指示しようとしましたが、コマンドパラメータが足りません。<emerge>管理者に連絡してください</emerge>");
        return;
      }
      var id = (uint)idParam.NumberValue;
      var action = (AiCharacterAction)actionParam.NumberValue;

      if (action == AiCharacterAction.Assault || action == AiCharacterAction.Attack || action == AiCharacterAction.Defend)
      {
        if (soldierTypeParam == null)
        {
          await game.CharacterLogAsync($"別働隊に指示しようとしましたが、コマンドパラメータが足りません。<emerge>管理者に連絡してください</emerge>");
          return;
        }
      }
      if (action == AiCharacterAction.Attack || action == AiCharacterAction.DomesticAffairs || action == AiCharacterAction.Defend)
      {
        if (targetTownParam == null)
        {
          await game.CharacterLogAsync($"別働隊に指示しようとしましたが、コマンドパラメータが足りません。<emerge>管理者に連絡してください</emerge>");
          return;
        }
      }
      var soldierType = (AiCharacterSoldierType?)soldierTypeParam?.NumberValue;
      var targetTownId = (uint?)targetTownParam?.NumberValue;

      var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync($"別働隊に指示しようとしましたが、あなたの国はすでに滅亡しているか無所属です");
        return;
      }
      var country = countryOptional.Data;

      var managementOptional = await repo.Character.GetManagementByAiCharacterIdAsync(id);
      if (!managementOptional.HasData || managementOptional.Data.HolderCharacterId != character.Id)
      {
        await game.CharacterLogAsync($"別働隊に指示しようとしましたが、指定された武将はあなたのものではありません");
        return;
      }
      var management = managementOptional.Data;

      var flyingColumnOptional = await repo.Character.GetByIdAsync(id);
      if (!flyingColumnOptional.HasData || flyingColumnOptional.Data.HasRemoved)
      {
        await game.CharacterLogAsync($"別働隊に指示しようとしましたが、指定された別動隊は存在しません");
        return;
      }
      if (flyingColumnOptional.Data.AiType != CharacterAiType.FlyingColumn)
      {
        await game.CharacterLogAsync($"別働隊に指示しようとしましたが、指定された武将は別働隊ではありません");
        return;
      }

      Town town = null;
      if (targetTownId != null)
      {
        var townOptional = await repo.Town.GetByIdAsync(targetTownId.Value);
        if (!townOptional.HasData)
        {
          await game.CharacterLogAsync($"別働隊に指示しようとしましたが、指定された都市は存在しません");
          return;
        }
        town = townOptional.Data;
      }

      management.Action = action;
      if (soldierType != null)
      {
        management.SoldierType = soldierType.Value;
      }
      if (targetTownId != null)
      {
        management.TargetTownId = targetTownId.Value;
      }

      character.Contribution += 30;
      character.AddLeadershipEx(50);
      character.SkillPoint++;

      var actionStr = action == AiCharacterAction.None ? "なし" :
        action == AiCharacterAction.DomesticAffairs ? "内政" :
        action == AiCharacterAction.Defend ? "守備" :
        action == AiCharacterAction.Attack ? "攻撃" :
        action == AiCharacterAction.Assault ? "遊撃" : "指定なし";
      var soldierTypeStr = soldierType == AiCharacterSoldierType.Default ? "標準" :
        soldierType == AiCharacterSoldierType.Infancty ? "戟兵" :
        soldierType == AiCharacterSoldierType.Cavalry ? "騎兵" :
        soldierType == AiCharacterSoldierType.Archer ? "弩兵" : "指定なし";
      await game.CharacterLogAsync($"別動隊 <character>{flyingColumnOptional.Data.Name}</character> の指示を更新しました: {actionStr} <town>{town?.Name ?? "なし"}</town> {soldierTypeStr}");
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(management), character.Id);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var aiId = (uint)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var action = (AiCharacterAction)options.FirstOrDefault(p => p.Type == 2).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var soldierType = (AiCharacterSoldierType?)options.FirstOrDefault(p => p.Type == 3)?.NumberValue;
      var targetTown = (uint?)options.FirstOrDefault(p => p.Type == 4)?.NumberValue;

      if (action == AiCharacterAction.Assault || action == AiCharacterAction.Attack || action == AiCharacterAction.Defend)
      {
        if (soldierType == null)
        {
          ErrorCode.LackOfCommandParameter.Throw();
        }
      }
      if (action == AiCharacterAction.Attack || action == AiCharacterAction.DomesticAffairs || action == AiCharacterAction.Defend)
      {
        if (targetTown == null)
        {
          ErrorCode.LackOfCommandParameter.Throw();
        }
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }

  public class RemoveFlyingColumnCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.RemoveFlyingColumn;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var idParam = options.FirstOrDefault(p => p.Type == 1);
      if (idParam == null)
      {
        await game.CharacterLogAsync($"別働隊を削除しようとしましたが、コマンドパラメータが足りません。<emerge>管理者に連絡してください</emerge>");
        return;
      }
      var id = (uint)idParam.NumberValue;

      var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync($"別働隊を削除しようとしましたが、あなたの国はすでに滅亡しているか無所属です");
        return;
      }
      var country = countryOptional.Data;

      var managementOptional = await repo.Character.GetManagementByAiCharacterIdAsync(id);
      if (!managementOptional.HasData || managementOptional.Data.HolderCharacterId != character.Id)
      {
        await game.CharacterLogAsync($"別働隊を削除しようとしましたが、指定された武将はあなたのものではありません");
        return;
      }
      var management = managementOptional.Data;

      var flyingColumnOptional = await repo.Character.GetByIdAsync(id);
      if (!flyingColumnOptional.HasData || flyingColumnOptional.Data.HasRemoved)
      {
        await game.CharacterLogAsync($"別働隊を削除しようとしましたが、指定された別動隊は存在しません");
        return;
      }
      if (flyingColumnOptional.Data.AiType != CharacterAiType.FlyingColumn)
      {
        await game.CharacterLogAsync($"別働隊を削除しようとしましたが、指定された武将は別働隊ではありません");
        return;
      }

      flyingColumnOptional.Data.AiType = CharacterAiType.RemovedFlyingColumn;
      flyingColumnOptional.Data.DeleteTurn = (short)(Config.DeleteTurns - 36);
      character.Contribution += 30;
      character.AddLeadershipEx(50);
      character.SkillPoint++;

      management.Action = AiCharacterAction.Removed;

      await game.CharacterLogAsync($"別動隊 <character>{flyingColumnOptional.Data.Name}</character> を削除しました。別動隊武将更新のタイミングで削除されます");
      await CharacterService.StreamCharacterAsync(repo, flyingColumnOptional.Data);
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(management), character.Id);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var aiId = (uint)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
