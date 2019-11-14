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
  public class BuildSubBuildingCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.BuildSubBuilding;

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
        await game.CharacterLogAsync($"建築物建設に必要なパラメータが足りません。<emerge>管理者にお問い合わせください</emerge>");
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

      if (country.SafeMoney + character.Money < info.Money)
      {
        await game.CharacterLogAsync($"{info.Name} (金: <num>{info.Money}</num>) を建設しようとしましたが、金が足りません");
        return;
      }

      var subBuildings = await repo.Town.GetSubBuildingsAsync();
      var townSubBuildings = subBuildings.Where(s => s.TownId == town.Id);
      var sizeMax = town.Type == TownType.Agriculture ? 2 :
        town.Type == TownType.Commercial ? 2 :
        town.Type == TownType.Fortress ? 3 :
        town.Type == TownType.Large ? 4 : 0;
      var currentSize = townSubBuildings.GetInfoes().Sum(i => i.Size);
      if (currentSize + info.Size > sizeMax)
      {
        await game.CharacterLogAsync($"<town>{town.Name}</town> に {info.Name} (敷地: <num>{info.Size}</num>) を建設しようとしましたが、敷地上限 <num>{sizeMax}</num> を超過します。残り敷地: <num>{sizeMax - currentSize}</num>");
        return;
      }
      if (!info.CanBuildMultiple && townSubBuildings.Any(b => b.Type == type && b.Status != TownSubBuildingStatus.Removing))
      {
        await game.CharacterLogAsync($"<town>{town.Name}</town> に {info.Name} (敷地: <num>{info.Size}</num>) を建設しようとしましたが、同一都市に複数建設することはできません");
        return;
      }

      if (subBuildings.Any(d => d.CharacterId == character.Id && (d.Status == TownSubBuildingStatus.Removing || d.Status == TownSubBuildingStatus.UnderConstruction)))
      {
        await game.CharacterLogAsync($"建築物を建設しようとしましたが、１人の武将が建設または撤去を同時に行うことはできません");
        return;
      }

      var end = game.GameDateTime.AddMonth(info.BuildDuring);
      var subBuilding = new TownSubBuilding
      {
        TownId = town.Id,
        CharacterId = character.Id,
        Type = type,
        Status = TownSubBuildingStatus.UnderConstruction,
        StatusFinishGameDateTime = end,
      };
      await repo.Town.AddSubBuildingAsync(subBuilding);

      if (country.SafeMoney >= info.Money)
      {
        country.SafeMoney -= info.Money;
      }
      else
      {
        character.Money -= info.Money - country.SafeMoney;
        country.SafeMoney = 0;
      }
      character.Contribution += 100;
      character.AddStrongEx(100);
      character.SkillPoint++;

      await game.CharacterLogAsync($"<town>{town.Name}</town> で {info.Name} の建設を開始しました。終了: <num>{end.Year}</num>年<num>{end.Month}</num>月");

      await repo.SaveChangesAsync();
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
