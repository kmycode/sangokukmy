﻿using System;
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

      var size = await CountryService.GetCountryBuildingSizeAsync(repo, country.Id, CountryBuilding.Secretary);
      if (size <= 0.0f)
      {
        await game.CharacterLogAsync($"政務官を雇おうとしましたが、対応する国家施設がないか、十分な耐久がありません");
        return;
      }

      var charas = await repo.Country.GetCharactersAsync(country.Id);
      if (charas.Count(c => c.Character.AiType.IsSecretary()) > 0)
      {
        await game.CharacterLogAsync($"政務官を雇おうとしましたが、すでに政務官の数が上限 <num>1</num> に達しています");
        return;
      }

      if (country.SafeMoney < 2000)
      {
        await game.CharacterLogAsync($"政務官を雇おうとしましたが、国庫に金 <num>2000</num> がありません");
        return;
      }

      country.SafeMoney -= 2000;

      var system = await repo.System.GetAsync();
      var ai = AiCharacterFactory.Create(type);
      ai.Initialize(game.GameDateTime);
      ai.Character.CountryId = character.CountryId;
      ai.Character.TownId = country.CapitalTownId;
      ai.Character.Money = 10000;
      ai.Character.Rice = 10000;
      ai.Character.LastUpdated = DateTime.Now.AddSeconds(10);
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

      character.Contribution += 30;
      character.AddLeadershipEx(50);
      await game.CharacterLogAsync($"政務官 <character>{ai.Character.Name}</character> を雇いました");
      await game.MapLogAsync(EventType.SecretaryAdded, $"<country>{country.Name}</country> は、新しく政務官 <character>{ai.Character.Name}</character> を雇いました", false);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var type = (CharacterAiType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      if (!type.IsSecretary())
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      await this.CheckCanInputAsync(repo, characterId);

      var has = await CountryService.HasCountryBuildingAsync(repo, this.Country.Id, CountryBuilding.Secretary);
      if (!has)
      {
        ErrorCode.InvalidOperationError.Throw();
      }

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

      await repo.Character.GetByIdAsync((uint)id).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
      await repo.Unit.GetByIdAsync((uint)unitId).GetOrErrorAsync(ErrorCode.UnitNotFoundError);

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

      secretaryOptional.Data.AiType = CharacterAiType.RemovedSecretary;
      character.Contribution += 30;
      character.AddLeadershipEx(50);
      await game.CharacterLogAsync($"政務官 <character>{secretaryOptional.Data.Name}</character> を解雇しました。政務官武将更新のタイミングで削除されます");
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var id = (CharacterAiType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      await repo.Character.GetByIdAsync((uint)id).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);

      await this.CheckCanInputAsync(repo, characterId);

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}