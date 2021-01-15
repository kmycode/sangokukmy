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
      var system = await repo.System.GetAsync();
      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      var countryOptional = await repo.Country.GetAliveByIdAsync(character.CountryId);
      var directionOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();
      var townTypeOptional = options.FirstOrDefault(p => p.Type == 2).ToOptional();

      if (character.Money < 50_0000 && system.RuleSet != GameRuleSet.SimpleBattle)
      {
        await game.CharacterLogAsync("都市建設しようとしましたが、金が足りません。<num>50 0000</num> 必要です");
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

      if (system.RuleSet == GameRuleSet.SimpleBattle)
      {
        await game.CharacterLogAsync("都市建設しようとしましたが、原理ルールでは都市建設ができません。代わりに大都市計画書が与えられます");
        await ItemService.SpendCharacterAsync(repo, item, character);
        await ItemService.GenerateItemAndSaveAsync(repo, new CharacterItem
        {
          CharacterId = character.Id,
          Type = CharacterItemType.LargeTownPlanningDocument,
          Status = CharacterItemStatus.CharacterHold,
        });
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
      var tt = towns.FirstOrDefault(t => t.X == x && t.Y == y);
      if (tt != null)
      {
        await game.CharacterLogAsync($"<town>{town.Name}</town> の隣に都市を建設しようとしましたが、その方向には別の都市 <town>{tt.Name}</town> があります");
        return;
      }

      var isCapital = false;
      if (towns.Any(t => t.CountryId == character.CountryId))
      {
        if (town.CountryId != character.CountryId)
        {
          await game.CharacterLogAsync($"<town>{town.Name}</town> の隣に都市を建設しようとしましたが、自国以外の国の都市から建設を行うことはできません");
          return;
        }
      }
      else if (countryOptional.HasData && !countryOptional.Data.HasOverthrown)
      {
        isCapital = true;
      }

      var newTown = MapService.CreateTown(isCapital ? TownType.Large : townType);
      newTown.X = (short)x;
      newTown.Y = (short)y;
      newTown.Name = MapService.GetTownName((short)x, (short)y);
      newTown.CountryId = character.CountryId;
      newTown.UniqueCharacterId = item.UniqueCharacterId != 0 ? item.UniqueCharacterId : character.Id;
      if (isCapital)
      {
        newTown.SubType = townType;
      }
      await repo.Town.AddTownsAsync(new Town[] { newTown, });

      if (isCapital)
      {
        if (countryOptional.Data.Religion == ReligionType.Confucianism)
        {
          newTown.Confucianism = 500;
        }
        if (countryOptional.Data.Religion == ReligionType.Taoism)
        {
          newTown.Taoism = 500;
        }
        if (countryOptional.Data.Religion == ReligionType.Buddhism)
        {
          newTown.Buddhism = 500;
        }
        await repo.SaveChangesAsync();
        countryOptional.Data.CapitalTownId = newTown.Id;
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(countryOptional.Data), countryOptional.Data.Id);
        await StatusStreaming.Default.SendAllExceptForCountryAsync(ApiData.From(new CountryForAnonymous(countryOptional.Data)), countryOptional.Data.Id);
      }

      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(newTown));
      await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(newTown), repo);

      character.Money -= 50_0000;
      await ItemService.SpendCharacterAsync(repo, item, character);
      if (isCapital)
      {
        await game.MapLogAsync(EventType.NewTownForCapital, $"<country>{countryName}</country> の <character>{character.Name}</character> は新たな都市 <town>{newTown.Name}</town> を建設し、首都としました", true);
      }
      else
      {
        await game.MapLogAsync(EventType.NewTown, $"<country>{countryName}</country> の <character>{character.Name}</character> は新たな都市 <town>{newTown.Name}</town> を建設しました", true);
      }
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
