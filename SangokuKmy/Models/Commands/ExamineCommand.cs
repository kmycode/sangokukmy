using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  public class ExamineCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Examine;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      if (character.Money < 50)
      {
        await game.CharacterLogAsync("金が足りません。<num>50</num> 必要です");
        return;
      }

      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (townOptional.HasData)
      {
        var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);

        var town = townOptional.Data;
        if (town.CountryId != character.CountryId || (!countryOptional.HasData || countryOptional.Data.HasOverthrown))
        {
          await game.CharacterLogAsync($"<town>{town.Name}</town> で探索しようとしましたが、自国の都市ではありません。能力強化に切り替えます");
          await Commands.ExecuteAsync(CharacterCommandType.Training, repo, character, new List<CharacterCommandParameter>
          {
            new CharacterCommandParameter
            {
              Type = 0,
              NumberValue = 0,
            },
          }, game);
          return;
        }
        var country = countryOptional.Data;

        if (RandomService.Next(0, 150) == 0)
        {
          var info = await ItemService.PickTownHiddenItemAsync(repo, character.TownId, character);
          if (info.HasData)
          {
            await game.CharacterLogAsync($"<town>{town.Name}</town> に隠されたアイテム {info.Data.Name} を手に入れました");
          }
          else
          {
            await game.CharacterLogAsync($"金 <num>5000</num> を入手しました");
            character.Money += 5000;
          }
        }
        else
        {
          int money;
          if (RandomService.Next(0, 30) == 0)
          {
            money = RandomService.Next(10, 41) * 100;
          }
          else
          {
            money = RandomService.Next(50, 400);
          }
          character.Money += money;
          character.Contribution += 30;
          character.SkillPoint++;

          var safeMoneyMax = CountryService.GetCountrySafeMax((await repo.Country.GetPoliciesAsync(country.Id)).GetAvailableTypes());
          if (country.SafeMoney < safeMoneyMax)
          {
            country.SafeMoney = Math.Min(safeMoneyMax, country.SafeMoney + money);
          }

          if (character.Strong > character.Intellect && character.Strong > character.Leadership && character.Strong > character.Popularity)
          {
            character.AddStrongEx(50);
          }
          else if (character.Intellect > character.Leadership && character.Intellect > character.Popularity)
          {
            character.AddIntellectEx(50);
          }
          else if (character.Leadership > character.Popularity)
          {
            character.AddLeadershipEx(50);
          }
          else
          {
            character.AddPopularityEx(50);
          }

          await game.CharacterLogAsync($"金 <num>{money}</num> を発見し、懐中と国庫にそれぞれおさめました");
          await StatusStreaming.Default.SendCountryAsync(ApiData.From(country), country.Id);
        }
      }
      else
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates);
    }
  }
}
