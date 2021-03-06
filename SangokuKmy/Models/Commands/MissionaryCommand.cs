﻿using System;
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
  public class MissionaryCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Missionary;

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

      var religion = ReligionType.Any;
      var countryOptional = await repo.Country.GetAliveByIdAsync(character.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync("布教しようとしましたが、国に仕官しないと布教できません");
        return;
      }
      var country = countryOptional.Data;
      religion = country.Religion;

      if (religion == ReligionType.None)
      {
        await game.CharacterLogAsync("布教しようとしましたが、国教が無神のため布教できません");
        return;
      }

      if (religion == ReligionType.Any)
      {
        await game.CharacterLogAsync("布教しようとしましたが、国教が設定されていません");
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
        var add = (int)(attribute / 20.0f + RandomService.Next(0, attribute) / 40.0f);
        if (add < 1)
        {
          add = 1;
        }

        var skills = await repo.Character.GetSkillsAsync(character.Id);
        add = (int)(add * (1 + skills.GetSumOfValues(CharacterSkillEffectType.MissionaryPercentage) / 100.0f));

        if (character.Religion == religion)
        {
          add = (int)(add * 1.4f);
        }

        var oldReligion = town.Religion;

        string religionName = religion.GetString();
        town.AddReligionPoint(religion, add);

        // 経験値、金の増減
        if (countryOptional.HasData)
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
      var system = await repo.System.GetAsync();
      if (system.RuleSet == GameRuleSet.SimpleBattle)
      {
        ErrorCode.RuleSetError.Throw();
      }

      var skills = await repo.Character.GetSkillsAsync(characterId);
      if (!skills.AnySkillEffects(CharacterSkillEffectType.Command, (int)this.Type))
      {
        ErrorCode.NotSkillError.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates);
    }
  }
}
