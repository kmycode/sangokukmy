using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Migrations;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  public class CreateTownCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.CreateTown;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      var countryOptional = await repo.Country.GetAliveByIdAsync(character.CountryId);
      var directionOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();
      var townTypeOptional = options.FirstOrDefault(p => p.Type == 2).ToOptional();

      if (character.Money < 200_0000)
      {
        await game.CharacterLogAsync("都市建設しようとしましたが、金が足りません。<num>200 0000</num> 必要です");
        return;
      }

      if (!townOptional.HasData)
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var town = townOptional.Data;

      var countryName = countryOptional.Data?.Name ?? "無所属";

      if (!directionOptional.HasData)
      {
        await game.CharacterLogAsync("都市建設のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var direction = (CreateTownDirection)directionOptional.Data.NumberValue;

      if (!townTypeOptional.HasData)
      {
        await game.CharacterLogAsync("都市建設のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var townType = (TownType)townTypeOptional.Data.NumberValue;

      var items = await repo.Character.GetItemsAsync(character.Id);
      var item = items.FirstOrDefault(i => i.Type == CharacterItemType.CastleBlueprint);
      if (item == null)
      {
        await game.CharacterLogAsync("都市建設しようとしましたが、コマンド実行に必要なアイテムを所持していません");
        return;
      }

      var x = (direction == CreateTownDirection.Left || direction == CreateTownDirection.LeftBottom || direction == CreateTownDirection.LeftTop) ? town.X - 1 :
        (direction == CreateTownDirection.Right || direction == CreateTownDirection.RightBottom || direction == CreateTownDirection.RightTop) ? town.X + 1 : town.X;
      var y = (direction == CreateTownDirection.Top || direction == CreateTownDirection.LeftTop || direction == CreateTownDirection.RightTop) ? town.Y - 1 :
        (direction == CreateTownDirection.Bottom || direction == CreateTownDirection.LeftBottom || direction == CreateTownDirection.RightBottom) ? town.Y + 1 : town.Y;

      if (x < 0 || x >= 10 || y < 0 || y >= 10)
      {
        await game.CharacterLogAsync($"<town>{town.Name}</town> の隣に都市を建設しようとしましたが、そこには建設できません");
        return;
      }

      var towns = await repo.Town.GetAllAsync();
      var t = towns.FirstOrDefault(t => t.X == x && t.Y == y);
      if (t != null)
      {
        await game.CharacterLogAsync($"<town>{town.Name}</town> の隣に都市を建設しようとしましたが、その方向には別の都市 <town>{t.Name}</town> があります");
        return;
      }

      var newTown = MapService.CreateTown(townType);
      newTown.X = (short)x;
      newTown.Y = (short)y;
      newTown.Name = MapService.GetTownName((short)x, (short)y);
      newTown.CountryId = character.CountryId;
      await repo.Town.AddTownsAsync(new Town[] { newTown, });
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(newTown));
      await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(newTown), repo);

      character.Money -= 200_0000;
      await ItemService.SpendCharacterAsync(repo, item, character);
      await game.MapLogAsync(EventType.NewTown, $"<country>{countryName}</country> の <character>{character.Name}</character> は新たな都市 <town>{newTown.Name}</town> を建設しました", true);
      await game.CharacterLogAsync($"都市 <town>{newTown.Name}</town> を建設しました");
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var direction = (CreateTownDirection)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var townType = options.FirstOrDefault(p => p.Type == 2).Or(ErrorCode.LackOfCommandParameter).NumberValue;

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }

  public enum CreateTownDirection: short
  {
    LeftTop = 1,
    Top = 2,
    RightTop = 3,
    Left = 4,
    Right = 5,
    LeftBottom = 6,
    Bottom = 7,
    RightBottom = 8,
  }
}
