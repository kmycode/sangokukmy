using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;

namespace SangokuKmy.Models.Commands
{
  public class OppressMissionaryCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.OppressMissionary;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      if (character.Money < 50)
      {
        await game.CharacterLogAsync($"金が足りません。<num>50</num> 必要です");
        return;
      }

      var religion = ReligionType.Any;
      var countryOptional = await repo.Country.GetAliveByIdAsync(character.CountryId);
      if (!countryOptional.HasData)
      {
        if (character.Religion == ReligionType.Any)
        {
          await game.CharacterLogAsync("弾圧しようとしましたが、宗教家以外は国に仕官しないと弾圧できません");
          return;
        }
      }
      else
      {
        var country = countryOptional.Data;

        religion = country.Religion;
        if (religion == ReligionType.Any)
        {
          religion = character.Religion;
        }
      }

      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (townOptional.HasData)
      {
        var town = townOptional.Data;

        if (character.CountryId != town.CountryId)
        {
          await game.CharacterLogAsync("弾圧しようとしましたが、他国の都市は弾圧できません");
          return;
        }

        // 内政値に加算する
        // $kgat += int($klea/6 + rand($klea/6));
        var current = character.Proficiency;
        var attribute = Math.Max(character.Intellect, character.Popularity);
        var add = (int)(attribute / 14.0f + RandomService.Next(0, attribute / 28));
        if (add < 1)
        {
          add = 1;
        }

        var oldReligion = town.Religion;

        string religionName = string.Empty;
        if (religion != ReligionType.Confucianism)
        {
          town.Confucianism = Math.Max(0, town.Confucianism - add);
          religionName += "儒教,";
        }
        if (religion != ReligionType.Buddhism)
        {
          town.Buddhism = Math.Max(0, town.Buddhism - add);
          religionName += "仏教,";
        }
        if (religion != ReligionType.Taoism)
        {
          town.Taoism = Math.Max(0, town.Taoism - add);
          religionName += "道教,";
        }

        // 経験値、金の増減
        if (countryOptional.HasData)
        {
          character.Contribution += 30;
        }
        character.AddIntellectEx(50);
        character.SkillPoint++;
        character.Money -= 50;
        await game.CharacterLogAsync($"<town>{town.Name}</town> の {religionName.Substring(0, religionName.Length - 1)}信仰 を <num>-" + add + "</num> 弾圧しました");

        if (town.Religion != oldReligion)
        {
          if (town.Religion == ReligionType.Any)
          {
            await game.MapLogAsync(EventType.StopReligion, $"<town>{town.Name}</town> は宗教の信仰をやめました", false);
          }
          else
          {
            var newReligionName = town.Religion == ReligionType.Buddhism ? "仏教" : town.Religion == ReligionType.Confucianism ? "儒教" : town.Religion == ReligionType.Taoism ? "道教" : string.Empty;
            await game.MapLogAsync(EventType.ChangeReligion, $"<town>{town.Name}</town> は {newReligionName} に改宗しました", false);
          }
        }
      }
      else
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var skills = await repo.Character.GetSkillsAsync(characterId);
      if (!skills.AnySkillEffects(CharacterSkillEffectType.Command, (int)this.Type))
      {
        ErrorCode.NotSkillError.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates);
    }
  }
}
