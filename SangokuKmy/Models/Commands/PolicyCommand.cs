using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  public class PolicyCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Policy;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (townOptional.HasData)
      {
        var town = townOptional.Data;
        if (town.CountryId != character.CountryId)
        {
          await game.CharacterLogAsync("<town>" + town.Name + "</town>で政策開発しようとしましたが、自国の都市ではありません");
          return;
        }

        var countryOptional = await repo.Country.GetAliveByIdAsync(character.CountryId);
        if (!countryOptional.HasData)
        {
          await game.CharacterLogAsync($"政策開発しようとしましたが、無所属は実行できません");
          return;
        }
        var country = countryOptional.Data;

        // 内政値に加算する
        // $kgat += int($klea/6 + rand($klea/6));
        var current = country.PolicyPoint;
        var attribute = Math.Max(character.Strong, character.Intellect);
        var add = (int)(attribute / 20.0f + RandomService.Next(0, attribute / 40));
        if (add < 1)
        {
          add = 1;
        }
        country.PolicyPoint += add;

        // 経験値、金の増減
        character.Contribution += 30;
        if (character.Strong > character.Intellect)
        {
          character.AddStrongEx(50);
        }
        else
        {
          character.AddIntellectEx(50);
        }

        await game.CharacterLogAsync($"<country>{country.Name}</country> の政策ポイントを <num>+{add}</num> 上げました");

        await StatusStreaming.Default.SendCountryAsync(ApiData.From(country), country.Id);
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
