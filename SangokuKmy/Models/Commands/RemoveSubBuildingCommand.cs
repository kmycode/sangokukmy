using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  public class RemoveSubBuildingCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.RemoveSubBuilding;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (!townOptional.HasData)
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }

      var town = townOptional.Data;
      if (town.CountryId != character.CountryId)
      {
        await game.CharacterLogAsync("<town>" + town.Name + "</town>で内政しようとしましたが、自国の都市ではありません");
        return;
      }

      var countryOptional = await repo.Country.GetAliveByIdAsync(character.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync("ID:" + character.CountryId + " の国は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var country = countryOptional.Data;

      var typeParameter = options.FirstOrDefault(p => p.Type == 1);
      if (typeParameter == null)
      {
        await game.CharacterLogAsync($"建築物撤去に必要なパラメータが足りません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var type = (TownSubBuildingType)typeParameter.NumberValue;

      var infoOptional = TownSubBuildingTypeInfoes.Get(type);
      if (!infoOptional.HasData)
      {
        await game.CharacterLogAsync($"ID: <num>{(int)type}</num> の建築物の情報が見つかりません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var info = infoOptional.Data;

      if (country.SafeMoney + character.Money < info.Money / 2)
      {
        await game.CharacterLogAsync($"{info.Name} (金: <num>{info.Money / 2}</num>) を建設しようとしましたが、金が足りません");
        return;
      }

      var subBuildings = await repo.Town.GetSubBuildingsAsync();
      var townSubBuildings = subBuildings.Where(s => s.TownId == town.Id);

      if (subBuildings.Any(d => d.CharacterId == character.Id && (d.Status == TownSubBuildingStatus.Removing || d.Status == TownSubBuildingStatus.UnderConstruction)))
      {
        await game.CharacterLogAsync($"建築物を撤去しようとしましたが、１人の武将が建設または撤去を同時に行うことはできません");
        return;
      }

      var end = game.GameDateTime.AddMonth(info.BuildDuring / 2);
      var subBuilding = townSubBuildings.FirstOrDefault(s => s.TownId == town.Id && s.Type == type && s.Status == TownSubBuildingStatus.Available);
      if (subBuilding == null)
      {
        await game.CharacterLogAsync($"建築物を撤去しようとしましたが、{info.Name} は <town>{town.Name}</town> に存在しないか、建設中です");
        return;
      }

      if (country.SafeMoney >= info.Money / 2)
      {
        country.SafeMoney -= info.Money / 2;
      }
      else
      {
        character.Money -= info.Money / 2 - country.SafeMoney;
        country.SafeMoney = 0;
      }
      character.Contribution += 100;
      character.AddStrongEx(100);
      character.SkillPoint++;

      subBuilding.Status = TownSubBuildingStatus.Removing;
      info.OnRemoving?.Invoke(town);

      await game.CharacterLogAsync($"<town>{town.Name}</town> で {info.Name} の撤去を開始しました。終了: <num>{end.Year}</num>年<num>{end.Month}</num>月");
      await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(subBuilding), repo, town);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var type = (TownSubBuildingType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
      var posts = await repo.Country.GetPostsAsync(chara.CountryId);
      if (!posts.CanBuildTownSubBuildings())
      {
        ErrorCode.NotPermissionError.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
