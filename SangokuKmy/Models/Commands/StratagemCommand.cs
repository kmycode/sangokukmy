using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using System.Linq;

namespace SangokuKmy.Models.Commands
{
  /// <summary>
  /// 謀略
  /// </summary>
  public abstract class StratagemCommand : Command
  {
    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      if (character.CountryId == 0)
      {
        await game.CharacterLogAsync($"国庫から金を搬出しようとしましたが、無所属は実行できません");
        return;
      }

      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (!townOptional.HasData)
      {
        await game.CharacterLogAsync($"ID: <num>{character.TownId}</num> の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var town = townOptional.Data;

      if (town.CountryId == character.CountryId)
      {
        await game.CharacterLogAsync($"自国の都市で謀略を実行することはできません");
        return;
      }

      var countryOptional = await repo.Country.GetByIdAsync(town.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync($"ID: <num>{character.CountryId}</num> の国は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var country = countryOptional.Data;

      var warOptional = await repo.CountryDiplomacies.GetCountryWarAsync(character.CountryId, country.Id);
      if (!warOptional.HasData || warOptional.Data.Status == CountryWarStatus.Stoped || warOptional.Data.Status == CountryWarStatus.None)
      {
        await game.CharacterLogAsync($"<country>{country.Name}</country> の <town>{town.Name}</town> で謀略を実行しようとしましたが、宣戦の関係にありません");
        return;
      }

      var war = warOptional.Data;
      if (war.Status == CountryWarStatus.InReady && war.IntStartGameDate > game.GameDateTime.ToInt() + 144)
      {
        await game.CharacterLogAsync($"<country>{country.Name}</country> の <town>{town.Name}</town> で謀略を実行しようとしましたが、実行は開戦の <num>144</num> ターン以内である必要があります");
        return;
      }

      if (character.Money < 50)
      {
        await game.CharacterLogAsync($"金が足りません。<num>50</num> 必要です");
        return;
      }

      var size = await CountryService.GetCountryBuildingSizeAsync(repo, character.CountryId, CountryBuilding.Spy);
      var defenders = await repo.Town.GetDefendersAsync(town.Id);
      await this.StratagemAsync(repo, size, character, town, defenders.Select(d => d.Character), game);

      character.Money -= 50;
      character.Contribution += 30;
      character.AddIntellectEx(50);
    }

    protected abstract Task StratagemAsync(MainRepository repo, float size, Character character, Town town, IEnumerable<Character> defenders, CommandSystemData game);

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }

  public class BreakTechnologyCommand : StratagemCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.BreakTechnology;

    protected override async Task StratagemAsync(MainRepository repo, float size, Character character, Town town, IEnumerable<Character> defenders, CommandSystemData game)
    {
      var isSucceed = RandomService.Next(0, 100) >= 40 - (int)(size * 30);
      if (isSucceed)
      {
        var result = (int)((character.Intellect / 15.0f + RandomService.Next(0, character.Intellect) / 15.0f) * size) - defenders.Count();
        if (result < 1) result = 1;
        town.Technology -= result;
        if (town.Technology < 0) town.Technology = 0;
        await game.CharacterLogAsync($"<town>{town.Name}</town> に焼討を行い、技術を <num>{result}</num> 破壊しました");
        await game.MapLogAsync(EventType.Burn, $"何者かが <town>{town.Name}</town> で技術破壊を行ったようです", false);
      }
      else
      {
        await game.CharacterLogAsync($"<town>{town.Name}</town> で技術破壊をしようとして、失敗しました");
      }
    }
  }

  public class BreakWallCommand : StratagemCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.BreakWall;

    protected override async Task StratagemAsync(MainRepository repo, float size, Character character, Town town, IEnumerable<Character> defenders, CommandSystemData game)
    {
      var isSucceed = RandomService.Next(0, 100) >= 40 - (int)(size * 30);
      if (isSucceed)
      {
        var result = (int)((character.Intellect / 15.0f + RandomService.Next(0, character.Intellect) / 15.0f) * size) - defenders.Count();
        if (result < 1) result = 1;
        town.Wall -= result;
        if (town.Wall < 0) town.Wall = 0;
        await game.CharacterLogAsync($"<town>{town.Name}</town> に焼討を行い、城壁を <num>{result}</num> 破壊しました");
        await game.MapLogAsync(EventType.Burn, $"何者かが <town>{town.Name}</town> で城壁破壊を行ったようです", false);
      }
      else
      {
        await game.CharacterLogAsync($"<town>{town.Name}</town> で城壁破壊をしようとして、失敗しました");
      }
    }
  }

  public class AgitationCommand : StratagemCommand
  {
    public override CharacterCommandType Type => CharacterCommandType.Agitation;

    protected override async Task StratagemAsync(MainRepository repo, float size, Character character, Town town, IEnumerable<Character> defenders, CommandSystemData game)
    {
      var isSucceed = RandomService.Next(0, 100) >= 40 - (int)(size * 30);
      if (isSucceed)
      {
        var result = (int)((character.Intellect / 15.0f + RandomService.Next(0, character.Intellect) / 22.0f) * size) - defenders.Count();
        if (result < 1) result = 1;
        town.Security -= (short)(result);
        town.People -= (int)(result * 8);
        if (town.Security < 0) town.Security = 0;
        if (town.People < 0) town.People = 0;
        await game.CharacterLogAsync($"<town>{town.Name}</town> の扇動を行い、民忠を <num>{result}</num> 下げました");
        await game.MapLogAsync(EventType.Agitation, $"何者かが <town>{town.Name}</town> で扇動を行ったようです", false);

        if (RandomService.Next(0, (int)(48 - size * 12.3f)) == 0 && town.Security == 0 && defenders.Count() == 0)
        {
          // 農民反乱
          await AiService.CreateFarmerCountryAsync(repo, town, game.MapLogAsync);
        }
      }
      else
      {
        await game.CharacterLogAsync($"<town>{town.Name}</town> で扇動をしようとして、失敗しました");
      }
    }
  }
}
