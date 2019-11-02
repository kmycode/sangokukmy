using System;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data.Entities;
using System.Linq;
using SangokuKmy.Models.Data.ApiEntities;
using System.Collections.Generic;
using SangokuKmy.Models.Services;
using SangokuKmy.Models.Updates;
using SangokuKmy.Models.Common;
using SangokuKmy.Streamings;
using SangokuKmy.Models.Updates.Ai;

namespace SangokuKmy.Models.Commands
{
  public abstract class SecretaryCommand : Command
  {
    protected Character Character { get; set; }

    protected Country Country { get; set; }

    protected async Task CheckCanInputAsync(MainRepository repo, uint characterId)
    {
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
      var country = await repo.Country.GetAliveByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
      var posts = await repo.Country.GetPostsAsync(country.Id);
      if (!posts.Any(p => p.CharacterId == characterId && p.Type.CanSecretary()))
      {
        ErrorCode.NotPermissionError.Throw();
      }

      var policies = (await repo.Country.GetPoliciesAsync(country.Id)).Where(p => p.Status == CountryPolicyStatus.Available);
      var secretaryMax = CountryService.GetSecretaryMax(policies.Select(p => p.Type));
      if (secretaryMax < 1)
      {
        ErrorCode.InvalidOperationError.Throw();
      }

      this.Character = chara;
      this.Country = country;
    }
  }

  public class AddSecretaryCommand : SecretaryCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.AddSecretary;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var typeParam = options.FirstOrDefault(p => p.Type == 1);
      if (typeParam == null)
      {
        await game.CharacterLogAsync($"政務官を雇おうとしましたが、コマンドパラメータが足りません。<emerge>管理者に連絡してください</emerge>");
        return;
      }
      var type = (CharacterAiType)typeParam.NumberValue;

      var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync($"政務官を雇おうとしましたが、あなたの国はすでに滅亡しているか無所属です");
        return;
      }
      var country = countryOptional.Data;

      var secretaries = (await repo.Country.GetCharactersAsync(country.Id)).Where(c => c.AiType.IsSecretary());
      var policies = await repo.Country.GetPoliciesAsync(country.Id);
      var secretaryMax = CountryService.GetSecretaryMax(policies.Where(p => p.Status == CountryPolicyStatus.Available).Select(p => p.Type));
      var currentSecretaryPoint = CountryService.GetCurrentSecretaryPoint(secretaries.Select(c => c.AiType));
      if (currentSecretaryPoint >= secretaryMax)
      {
        await game.CharacterLogAsync($"政務官を雇おうとしましたが、すでに政務官ポイントが上限 <num>{secretaryMax}</num> に達しています");
        return;
      }

      var afterSecretaryPoint = CountryService.GetCurrentSecretaryPoint(secretaries.Select(c => c.AiType).Append(type));
      if (afterSecretaryPoint > secretaryMax)
      {
        await game.CharacterLogAsync($"政務官を雇おうとしましたが、雇用後の政務官ポイントが上限 <num>{secretaryMax}</num> を越えます");
        return;
      }

      var cost = Config.SecretaryCost;
      if (character.Money < cost && country.SafeMoney < cost)
      {
        await game.CharacterLogAsync($"政務官を雇おうとしましたが、武将所持または国庫に金 <num>{cost}</num> がありません");
        return;
      }

      if (country.SafeMoney >= cost)
      {
        country.SafeMoney -= cost;
      }
      else
      {
        character.Money -= cost;
      }

      var system = await repo.System.GetAsync();
      var ai = AiCharacterFactory.Create(type);
      ai.Initialize(game.GameDateTime);
      ai.Character.CountryId = character.CountryId;
      ai.Character.TownId = country.CapitalTownId;
      ai.Character.Money = 10000;
      ai.Character.Rice = 10000;
      if (type != CharacterAiType.SecretaryScouter)
      {
        ai.Character.LastUpdated = character.LastUpdated.AddSeconds(Config.UpdateTime + 10);
        if (ai.Character.LastUpdated > system.CurrentMonthStartDateTime.AddSeconds(Config.UpdateTime))
        {
          ai.Character.LastUpdatedGameDate = game.GameDateTime.NextMonth();
        }
        else
        {
          ai.Character.LastUpdatedGameDate = game.GameDateTime;
        }
      }
      else
      {
        // 斥候は0分0秒更新で諜報する
        ai.Character.LastUpdated = system.CurrentMonthStartDateTime.AddSeconds(Config.UpdateTime);
        ai.Character.LastUpdatedGameDate = game.GameDateTime;
      }
      await repo.Character.AddAsync(ai.Character);

      await repo.SaveChangesAsync();
      await AiService.SetIconAsync(repo, ai.Character);

      if (type == CharacterAiType.SecretaryUnitLeader)
      {
        await UnitService.CreateAndSaveAsync(repo, new Unit
        {
          CountryId = character.CountryId,
          IsLimited = false,
          Name = "政務官部隊 ID:" + ai.Character.Id,
        }, ai.Character.Id);
      }

      ai.Character.Name += ai.Character.Id;
      character.Contribution += 30;
      character.AddLeadershipEx(50);
      await game.CharacterLogAsync($"政務官 <character>{ai.Character.Name}</character> を雇いました");
      await game.MapLogAsync(EventType.SecretaryAdded, $"<country>{country.Name}</country> は、新しく政務官 <character>{ai.Character.Name}</character> を雇いました", false);
      await CharacterService.StreamCharacterAsync(repo, ai.Character);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var type = (CharacterAiType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      if (!type.IsSecretary())
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      await this.CheckCanInputAsync(repo, characterId);

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }

  public class EditSecretaryCommand : SecretaryCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.Secretary;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var idParam = options.FirstOrDefault(p => p.Type == 1);
      var unitIdParam = options.FirstOrDefault(p => p.Type == 2);
      if (idParam == null || unitIdParam == null)
      {
        await game.CharacterLogAsync($"政務官を配属または転属しようとしましたが、コマンドパラメータが足りません。<emerge>管理者に連絡してください</emerge>");
        return;
      }
      var id = (uint)idParam.NumberValue;
      var unitId = (uint)unitIdParam.NumberValue;

      var secretaryOptional = await repo.Character.GetByIdAsync(id);
      if (!secretaryOptional.HasData || secretaryOptional.Data.HasRemoved)
      {
        await game.CharacterLogAsync($"政務官を配属または転属しようとしましたが、指定した政務官は存在しないか、すでに削除されています");
        return;
      }
      if (secretaryOptional.Data.CountryId != character.CountryId)
      {
        await game.CharacterLogAsync($"政務官を配属または転属しようとしましたが、指定された政務官はあなたの国のものではありません");
        return;
      }
      if (!secretaryOptional.Data.AiType.IsSecretary())
      {
        await game.CharacterLogAsync($"政務官を配属または転属しようとしましたが、指定された武将は政務官ではありません");
        return;
      }
      if (secretaryOptional.Data.AiType == CharacterAiType.SecretaryUnitLeader)
      {
        await game.CharacterLogAsync($"政務官を配属または転属しようとしましたが、部隊長は部隊を変更できません");
        return;
      }

      var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync($"政務官を配属または転属しようとしましたが、あなたの国はすでに滅亡しているか無所属です");
        return;
      }
      var country = countryOptional.Data;

      var unitOptional = await repo.Unit.GetByIdAsync(unitId);
      if (!unitOptional.HasData)
      {
        await game.CharacterLogAsync($"政務官を配属または転属しようとしましたが、指定された部隊は存在しません");
        return;
      }
      var unit = unitOptional.Data;
      if (unit.CountryId != country.Id)
      {
        await game.CharacterLogAsync($"政務官を配属または転属しようとしましたが、指定された部隊はあなたの国のものではありません");
        return;
      }

      var unitMember = new UnitMember
      {
        CharacterId = id,
        UnitId = unitId,
        Post = secretaryOptional.Data.AiType == CharacterAiType.SecretaryUnitGather ? UnitMemberPostType.Helper : UnitMemberPostType.Normal,
      };
      await repo.Unit.SetMemberAsync(unitMember);

      character.Contribution += 30;
      character.AddLeadershipEx(50);
      await game.CharacterLogAsync($"政務官 <character>{secretaryOptional.Data.Name}</character> を部隊 {unit.Name} に配属しました");
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var id = (CharacterAiType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var unitId = (CharacterAiType)options.FirstOrDefault(p => p.Type == 2).Or(ErrorCode.LackOfCommandParameter).NumberValue;

      var secretary = await repo.Character.GetByIdAsync((uint)id).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
      await repo.Unit.GetByIdAsync((uint)unitId).GetOrErrorAsync(ErrorCode.UnitNotFoundError);

      if (secretary.AiType == CharacterAiType.SecretaryUnitLeader)
      {
        ErrorCode.InvalidOperationError.Throw();
      }

      await this.CheckCanInputAsync(repo, characterId);

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }

  public class EditSecretaryToTownCommand : SecretaryCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.SecretaryToTown;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var idParam = options.FirstOrDefault(p => p.Type == 1);
      var townIdParam = options.FirstOrDefault(p => p.Type == 2);
      if (idParam == null || townIdParam == null)
      {
        await game.CharacterLogAsync($"政務官を配属または転属しようとしましたが、コマンドパラメータが足りません。<emerge>管理者に連絡してください</emerge>");
        return;
      }
      var id = (uint)idParam.NumberValue;
      var townId = (uint)townIdParam.NumberValue;

      var secretaryOptional = await repo.Character.GetByIdAsync(id);
      if (!secretaryOptional.HasData || secretaryOptional.Data.HasRemoved)
      {
        await game.CharacterLogAsync($"政務官を配属または転属しようとしましたが、指定した政務官は存在しないか、すでに削除されています");
        return;
      }
      if (secretaryOptional.Data.CountryId != character.CountryId)
      {
        await game.CharacterLogAsync($"政務官を配属または転属しようとしましたが、指定された政務官はあなたの国のものではありません");
        return;
      }
      if (!secretaryOptional.Data.AiType.IsSecretary())
      {
        await game.CharacterLogAsync($"政務官を配属または転属しようとしましたが、指定された武将は政務官ではありません");
        return;
      }

      var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync($"政務官を配属または転属しようとしましたが、あなたの国はすでに滅亡しているか無所属です");
        return;
      }
      var country = countryOptional.Data;

      var townOptional = await repo.Town.GetByIdAsync(townId);
      if (!townOptional.HasData)
      {
        await game.CharacterLogAsync($"政務官を配属または転属しようとしましたが、指定された都市は存在しません");
        return;
      }

      if (secretaryOptional.Data.AiType == CharacterAiType.SecretaryUnitLeader)
      {
        var oldTownOptional = await repo.Town.GetByIdAsync(secretaryOptional.Data.TownId);
        if (!oldTownOptional.Data.IsNextToTown(townOptional.Data))
        {
          await game.CharacterLogAsync($"<town>{oldTownOptional.Data.Name}</town> にいる政務官を <town>{townOptional.Data.Name}</town> に配属または転属しようとしましたが、部隊長は所在都市に隣接する都市にしか移動できません");
          return;
        }
      }

      secretaryOptional.Data.TownId = townOptional.Data.Id;

      character.Contribution += 30;
      character.AddLeadershipEx(50);
      await game.CharacterLogAsync($"政務官 <character>{secretaryOptional.Data.Name}</character> を都市 <town>{townOptional.Data.Name}</town> に配属しました");

      await CharacterService.StreamCharacterAsync(repo, secretaryOptional.Data);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var id = (CharacterAiType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var townId = (CharacterAiType)options.FirstOrDefault(p => p.Type == 2).Or(ErrorCode.LackOfCommandParameter).NumberValue;

      await repo.Character.GetByIdAsync((uint)id).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
      await repo.Town.GetByIdAsync((uint)townId).GetOrErrorAsync(ErrorCode.TownNotFoundError);

      await this.CheckCanInputAsync(repo, characterId);

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }

  public class RemoveSecretaryCommand : SecretaryCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.RemoveSecretary;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var idParam = options.FirstOrDefault(p => p.Type == 1);
      if (idParam == null)
      {
        await game.CharacterLogAsync($"政務官を解雇しようとしましたが、コマンドパラメータが足りません。<emerge>管理者に連絡してください</emerge>");
        return;
      }
      var id = (uint)idParam.NumberValue;

      var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync($"政務官を解雇しようとしましたが、あなたの国はすでに滅亡しているか無所属です");
        return;
      }
      var country = countryOptional.Data;

      var secretaryOptional = await repo.Character.GetByIdAsync(id);
      if (!secretaryOptional.HasData || secretaryOptional.Data.HasRemoved)
      {
        await game.CharacterLogAsync($"政務官を解雇しようとしましたが、指定した政務官は存在しないか、すでに削除されています");
        return;
      }
      if (secretaryOptional.Data.CountryId != character.CountryId)
      {
        await game.CharacterLogAsync($"政務官を解雇しようとしましたが、指定された政務官はあなたの国のものではありません");
        return;
      }
      if (!secretaryOptional.Data.AiType.IsSecretary())
      {
        await game.CharacterLogAsync($"政務官を解雇しようとしましたが、指定された武将は政務官ではありません");
        return;
      }

      if (secretaryOptional.Data.AiType == CharacterAiType.SecretaryUnitLeader)
      {
        var units = await repo.Unit.GetByCountryIdAsync(secretaryOptional.Data.CountryId);
        var unit = units.FirstOrDefault(u => u.Members.Any(m => m.CharacterId == secretaryOptional.Data.Id && m.Post == UnitMemberPostType.Leader));
        if (unit != null)
        {
          await UnitService.RemoveAsync(repo, unit.Id);
        }
      }

      secretaryOptional.Data.AiType = CharacterAiType.RemovedSecretary;
      character.Contribution += 30;
      character.AddLeadershipEx(50);
      await game.CharacterLogAsync($"政務官 <character>{secretaryOptional.Data.Name}</character> を解雇しました。政務官武将更新のタイミングで削除されます");
      await CharacterService.StreamCharacterAsync(repo, secretaryOptional.Data);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var id = (uint)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      await repo.Character.GetByIdAsync(id).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);

      await this.CheckCanInputAsync(repo, characterId);

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
