using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  /// <summary>
  /// 都市購入
  /// </summary>
  public class BuyTownCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.BuyTown;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var townIdOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();

      if (!townIdOptional.HasData)
      {
        await game.CharacterLogAsync("徴兵のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
      }
      else
      {
        var townOptional = await repo.Town.GetByIdAsync((uint)townIdOptional.Data.NumberValue);
        var currentTownOptional = await repo.Town.GetByIdAsync(character.TownId);
        var countryOptional = await repo.Country.GetAliveByIdAsync(character.CountryId);
        if (!townOptional.HasData)
        {
          await game.CharacterLogAsync("ID:" + townIdOptional.Data + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        }
        else if (!currentTownOptional.HasData)
        {
          await game.CharacterLogAsync("現在所在するID:" + townIdOptional.Data + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        }
        else if (!countryOptional.HasData)
        {
          await game.CharacterLogAsync("都市を購入しようとしましたが、国に所属していません");
        }
        else
        {
          var town = townOptional.Data;
          var currentTown = currentTownOptional.Data;
          var country = countryOptional.Data;
          var system = await repo.System.GetAsync();
          var cost = await TownService.GetTownBuyCostAsync(repo, town, country);
          var targetCountryOptional = await repo.Country.GetAliveByIdAsync(town.CountryId);

          if (town.Id == currentTown.Id)
          {
            await game.CharacterLogAsync("<town>" + town.Name + "</town> を購入しようとしましたが、すでに所在しています");
          }
          else if (currentTown.CountryId != character.CountryId)
          {
            await game.CharacterLogAsync($"都市購入しようとしましたが、自国以外の都市からは実行できません");
          }
          else if (!currentTown.IsNextToTown(town))
          {
            await game.CharacterLogAsync($"<town>{town.Name}</town> を購入しようとしましたが、現在都市 <town>{currentTown.Name}</town> と隣接していません");
          }
          else if (country.PolicyPoint < cost)
          {
            await game.CharacterLogAsync($"<town>{town.Name}</town> を購入しようとしましたが、必要な政策ポイントが足りません。<num>{cost}</num> 必要です");
          }
          else if (!targetCountryOptional.HasData)
          {
            await game.CharacterLogAsync($"<town>{town.Name}</town> を購入しようとしましたが、無所属都市は購入できません");
          }
          else if (currentTown.IsNextToTown(town))
          {
            var isSurrender = (await repo.Town.CountByCountryIdAsync(town.CountryId)) == 1;

            // 購入
            country.PolicyPoint -= cost;
            await StatusStreaming.Default.SendCountryAsync(ApiData.From(country), country.Id);

            var defenders = (await repo.Town.GetAllDefendersAsync()).Where(d => d.TownId == town.Id);
            foreach (var d in defenders)
            {
              repo.Town.RemoveDefender(d.CharacterId);
              await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(d), repo, town);
            }
            town.CountryId = character.CountryId;
            town.Agriculture = (int)(town.Agriculture * 0.9f);
            town.Commercial = (int)(town.Commercial * 0.9f);
            town.Technology = (int)(town.Technology * 0.8f);
            town.TownBuildingValue = (int)(town.TownBuildingValue * 0.8f);
            town.People = (int)(town.People * 0.8f);
            town.Security = (short)(town.Security * 0.8f);
            var townCharacters = await repo.Town.GetCharactersAsync(town.Id);
            foreach (var c in townCharacters)
            {
              await CharacterService.StreamCharacterAsync(repo, c);
            }
            await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(town), repo);
            await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(town)));
            await LogService.AddMapLogAsync(repo, true, EventType.BuyTown, $"<country>{country.Name}</country> は、<country>{targetCountryOptional.Data.Name}</country> の <town>{town.Name}</town> を購入しました");

            if (isSurrender)
            {
              // 滅亡
              await CountryService.OverThrowAsync(repo, targetCountryOptional.Data, country, false);
              await LogService.AddMapLogAsync(repo, true, EventType.Surrender, $"<country>{targetCountryOptional.Data.Name}</country> は、<country>{country.Name}</country> に降伏しました");
            }

            character.Contribution += 30;
            character.SkillPoint++;
            await game.CharacterLogAsync("<town>" + town.Name + "</town> を購入しました");
          }
          else
          {
            await game.CharacterLogAsync("現在都市と隣接していないため、 <town>" + town.Name + "</town> を購入できませんでした");
          }
        }
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
      var townId = (uint)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var town = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.InternalDataNotFoundError, new { command = "move", townId, });

      var posts = await repo.Country.GetCharacterPostsAsync(chara.Id);
      if (!posts.Any(p => p.Type.CanPolicy()))
      {
        ErrorCode.NotPermissionError.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
