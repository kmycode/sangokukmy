using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  public class MissionarySelfCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.MissionarySelf;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      if (character.Religion == ReligionType.None)
      {
        await game.CharacterLogAsync("布教しようとしましたが、無神論者は布教できません");
        return;
      }

      if (character.Money < 50)
      {
        await game.CharacterLogAsync($"金が足りません。<num>50</num> 必要です");
        return;
      }

      var religion = character.Religion;

      if (religion == ReligionType.Any)
      {
        await game.CharacterLogAsync("布教しようとしましたが、武将固有の宗教を持っていません");
        return;
      }

      if (character.CountryId > 0)
      {
        await game.CharacterLogAsync("布教しようとしましたが、国に仕官している場合は国教以外を布教できません");
        return;
      }

      var countries = await repo.Country.GetAllAsync();
      if (!countries.Any(c => !c.HasOverthrown && c.Religion == religion))
      {
        await game.CharacterLogAsync($"{religion.GetString()} を布教しようとしましたが、その宗教を国教とした国が存在しないため布教できません");
        return;
      }

      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (townOptional.HasData)
      {
        var town = townOptional.Data;

        var alliance = await repo.CountryDiplomacies.GetCountryAllianceAsync(character.CountryId, town.CountryId);
        if (alliance.HasData && (alliance.Data.Status == CountryAllianceStatus.Available || alliance.Data.Status == CountryAllianceStatus.ChangeRequesting || alliance.Data.Status == CountryAllianceStatus.InBreaking) && !alliance.Data.CanMissionary)
        {
          await game.CharacterLogAsync($"<town>{town.Name}</town> で布教しようとしましたが、同盟条約により布教できません");
          return;
        }

        // 内政値に加算する
        // $kgat += int($klea/6 + rand($klea/6));
        var current = character.Proficiency;
        var attribute = Math.Max(character.Intellect, character.Popularity);
        var add = (int)(attribute / 20.0f + RandomService.Next(0, attribute / 40));
        if (add < 1)
        {
          add = 1;
        }

        if (character.CountryId > 0)
        {
          var countryOptional = await repo.Country.GetAliveByIdAsync(character.CountryId);
          if (character.Religion == countryOptional.Data?.Religion)
          {
            add = (int)(add * 1.3f);
          }

          if (town.Religion == ReligionType.Any)
          {
            add = (int)(add * 2.25f);
          }

          var skills = await repo.Character.GetSkillsAsync(character.Id);
          add = (int)(add * (1 + skills.GetSumOfValues(CharacterSkillEffectType.MissionaryPercentage) / 100.0f));
        }
        else
        {
          // 無所属は戦争中の国の布教を制限（布教のために解雇とかされると困る）
          var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
          if (wars.Any(w => w.IsJoin(town.CountryId) && (w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.InReady || w.Status == CountryWarStatus.StopRequesting)))
          {
            add /= 2;
            if (add < 1)
            {
              add = 1;
            }
          }
        }

        var oldReligion = town.Religion;

        string religionName = string.Empty;
        if (religion == ReligionType.Confucianism)
        {
          town.Confucianism += add;
          religionName = "儒教";
        }
        else if (religion == ReligionType.Buddhism)
        {
          town.Buddhism += add;
          religionName = "仏教";
        }
        else if (religion == ReligionType.Taoism)
        {
          town.Taoism += add;
          religionName = "道教";
        }

        // 経験値、金の増減
        if (character.CountryId > 0)
        {
          character.Contribution += 30;
        }
        if (character.Intellect > character.Popularity)
        {
          character.AddIntellectEx(50);
        }
        else
        {
          character.AddPopularityEx(50);
        }
        character.SkillPoint++;
        character.Money -= 50;
        await game.CharacterLogAsync($"<town>{town.Name}</town> の {religionName}信仰 を <num>+" + add + "</num> 上げました");

        var ranking = await repo.Character.GetCharacterRankingAsync(character.Id);
        ranking.MissionaryCount += add;

        if (town.Religion != oldReligion)
        {
          if (oldReligion == ReligionType.Any || oldReligion == ReligionType.None)
          {
            await game.MapLogAsync(EventType.NewReligion, $"<town>{town.Name}</town> は {religionName} の信仰を開始しました", false);
          }
          else
          {
            await game.MapLogAsync(EventType.ChangeReligion, $"<town>{town.Name}</town> は {oldReligion.GetString()} から {religionName} に改宗しました", false);
          }
          ranking.MissionaryChangeReligionCount++;
          await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(town), repo);
        }

        if (RandomService.Next(0, 256) == 0)
        {
          var info = await ItemService.PickTownHiddenItemAsync(repo, character.TownId, character);
          if (info.HasData)
          {
            await game.CharacterLogAsync($"<town>{town.Name}</town> に隠されたアイテム {info.Data.Name} を手に入れました");
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
      // AI専用のコマンド
      ErrorCode.NotSupportedError.Throw();
      // await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates);
    }
  }
}
