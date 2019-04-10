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

namespace SangokuKmy.Models.Commands
{
  public abstract class ScouterCommand : Command
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

      var policies = await repo.Country.GetPoliciesAsync(country.Id);
      if (!policies.Any(p => p.Type == CountryPolicyType.Scouter))
      {
        ErrorCode.InvalidOperationError.Throw();
      }

      this.Character = chara;
      this.Country = country;
    }
  }

  public class AddScouterCommand : SecretaryCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.AddScouter;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var townIdParam = options.FirstOrDefault(p => p.Type == 1);
      if (townIdParam == null)
      {
        await game.CharacterLogAsync($"斥候を雇おうとしましたが、コマンドパラメータが足りません。<emerge>管理者に連絡してください</emerge>");
        return;
      }
      var townId = (uint)townIdParam.NumberValue;

      var townOptional = await repo.Town.GetByIdAsync(townId);
      if (!townOptional.HasData)
      {
        await game.CharacterLogAsync($"斥候を雇おうとしましたが、ID: <num>{townId}</num> の都市が見つかりません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var town = townOptional.Data;

      var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync($"斥候を雇おうとしましたが、あなたの国はすでに滅亡しているか無所属です");
        return;
      }
      var country = countryOptional.Data;

      var charas = await repo.Country.GetCharactersAsync(country.Id);
      var scouters = await repo.Country.GetScoutersAsync(country.Id);
      if (scouters.Count >= Config.ScouterMax)
      {
        await game.CharacterLogAsync($"斥候を雇おうとしましたが、すでに斥候の数が上限 <num>{Config.ScouterMax}</num> に達しています");
        return;
      }

      if (scouters.Any(s => s.TownId == townId))
      {
        await game.CharacterLogAsync($"斥候を雇おうとしましたが、<town>{town.Name}</town> にはすでに斥候が派遣されています");
        return;
      }

      var cost = Config.ScouterCost;
      if (character.Money < cost && country.SafeMoney < cost)
      {
        await game.CharacterLogAsync($"斥候を雇おうとしましたが、武将所持または国庫に金 <num>{cost}</num> がありません");
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

      var scouter = new CountryScouter
      {
        CountryId = country.Id,
        TownId = town.Id,
      };

      await repo.Country.AddScouterAsync(scouter);
      await repo.SaveChangesAsync();

      character.Contribution += 30;
      character.AddLeadershipEx(50);
      await game.CharacterLogAsync($"<town>{town.Name}</town> に斥候を派遣しました");

      await StatusStreaming.Default.SendCountryAsync(ApiData.From(scouter), country.Id);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var townId = (uint)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      if (!(await repo.Town.GetByIdAsync(townId)).HasData)
      {
        ErrorCode.TownNotFoundError.Throw();
      }

      await this.CheckCanInputAsync(repo, characterId);

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }

  public class RemoveScouterCommand : SecretaryCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.RemoveScouter;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var townIdParam = options.FirstOrDefault(p => p.Type == 1);
      if (townIdParam == null)
      {
        await game.CharacterLogAsync($"斥候を解雇しようとしましたが、コマンドパラメータが足りません。<emerge>管理者に連絡してください</emerge>");
        return;
      }
      var townId = (uint)townIdParam.NumberValue;

      var townOptional = await repo.Town.GetByIdAsync(townId);
      if (!townOptional.HasData)
      {
        await game.CharacterLogAsync($"斥候を解雇しようとしましたが、ID: <num>{townId}</num> の都市が見つかりません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var town = townOptional.Data;

      var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync($"斥候を解雇しようとしましたが、あなたの国はすでに滅亡しているか無所属です");
        return;
      }
      var country = countryOptional.Data;

      var scouters = await repo.Country.GetScoutersAsync(country.Id);
      var targetScouter = scouters.FirstOrDefault(s => s.TownId == town.Id);

      if (targetScouter == null)
      {
        await game.CharacterLogAsync($"斥候を解雇しようとしましたが、<town>{town.Name}</town> には斥候が派遣されていません");
        return;
      }

      repo.Country.RemoveScouter(targetScouter);
      await game.CharacterLogAsync($"<town>{town.Name}</town> に派遣していた斥候を解雇しました");

      targetScouter.IsRemoved = true;
      await StatusStreaming.Default.SendCountryAsync(ApiData.From(targetScouter), country.Id);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var townId = (uint)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      if (!(await repo.Town.GetByIdAsync(townId)).HasData)
      {
        ErrorCode.TownNotFoundError.Throw();
      }

      await this.CheckCanInputAsync(repo, characterId);

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
