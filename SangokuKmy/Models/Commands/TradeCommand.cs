﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  public class TradeCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Trade;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var countryOptional = await repo.Country.GetAliveByIdAsync(character.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync("交易しようとしましたが、国に仕官していません");
        return;
      }
      var country = countryOptional.Data;

      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (townOptional.HasData)
      {
        var town = townOptional.Data;

        if (town.CountryId == country.Id)
        {
          await game.CharacterLogAsync("交易しようとしましたが、自国に対しては実行できません");
          return;
        }

        var targetCountryOptional = await repo.Country.GetAliveByIdAsync(town.CountryId);
        if (!targetCountryOptional.HasData)
        {
          await game.CharacterLogAsync("交易しようとしましたが、無所属に対しては実行できません");
          return;
        }
        var targetCountry = targetCountryOptional.Data;

        if (targetCountry.AiType == CountryAiType.Terrorists)
        {
          await game.CharacterLogAsync("交易しようとしましたが、敵対化していない異民族に対しては実行できません");
          return;
        }

        var war = await repo.CountryDiplomacies.GetCountryWarAsync(character.CountryId, town.CountryId);
        if (war.HasData && war.Data.Status != CountryWarStatus.Stoped)
        {
          await game.CharacterLogAsync($"<town>{town.Name}</town> で交易しようとしましたが、戦争中です");
          return;
        }

        // 内政値に加算する
        // $kgat += int($klea/6 + rand($klea/6));
        var attribute = Math.Max(character.Intellect, character.Popularity);
        var add = (int)(attribute / 20.0f + RandomService.Next(0, attribute) / 40.0f);
        if (add < 1)
        {
          add = 1;
        }
        add *= 220;
        var missionaryAdd = Math.Max(1, add / 500);

        var policies = await repo.Country.GetPoliciesAsync();
        var max = CountryService.GetCountrySafeMax(policies.Where(p => p.Status == CountryPolicyStatus.Available && p.CountryId == country.Id).Select(p => p.Type));
        var targetMax = CountryService.GetCountrySafeMax(policies.Where(p => p.Status == CountryPolicyStatus.Available && p.CountryId == targetCountry.Id).Select(p => p.Type));

        if (character.AiType == CharacterAiType.SecretaryTrader)
        {
          if (policies.Any(p => p.Status == CountryPolicyStatus.Available && p.Type == CountryPolicyType.Obedience))
          {
            missionaryAdd = (int)(missionaryAdd * 1.8f);
          }
        }

        var canMissionary = true;
        var oldReligion = town.Religion;
        var allianceOptional = await repo.CountryDiplomacies.GetCountryAllianceAsync(country.Id, targetCountry.Id);
        if (!allianceOptional.HasData || allianceOptional.Data.CanMissionary)
        {
          town.AddReligionPoint(country.Religion, missionaryAdd);
        }
        else
        {
          canMissionary = false;
        }

        var peopleMax = (int)(town.TownBuilding != TownBuilding.Houses ? town.PeopleMax : town.PeopleMax + ((float)town.TownBuildingValue / Config.TownBuildingMax) * 10000);
        town.People = Math.Min(town.People + 200, peopleMax);
        country.SafeMoney += add;
        targetCountry.SafeMoney += add;
        country.SafeMoney = Math.Min(country.SafeMoney, max);
        targetCountry.SafeMoney = Math.Min(targetCountry.SafeMoney, targetMax);
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(country), country.Id);
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(targetCountry), targetCountry.Id);

        var ranking = await repo.Character.GetCharacterRankingAsync(character.Id);
        ranking.MissionaryCount += missionaryAdd;

        if (town.Religion != oldReligion)
        {
          if (oldReligion == ReligionType.Any || oldReligion == ReligionType.None)
          {
            await game.MapLogAsync(EventType.NewReligion, $"<town>{town.Name}</town> は {town.Religion.GetString()} の信仰を開始しました", false);
          }
          else
          {
            await game.MapLogAsync(EventType.ChangeReligion, $"<town>{town.Name}</town> は {oldReligion.GetString()} から {town.Religion.GetString()} に改宗しました", false);
          }
          ranking.MissionaryChangeReligionCount++;
          await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(town), repo);
        }

        // 経験値、金の増減
        character.Contribution += 30;
        if (character.Intellect > character.Popularity)
        {
          character.AddIntellectEx(50);
        }
        else
        {
          character.AddPopularityEx(50);
        }
        character.SkillPoint++;
        if (canMissionary)
        {
          await game.CharacterLogAsync($"<town>{town.Name}</town> と交易し、お互い金 <num>{add}</num> を入手しました。{country.Religion.GetString()} を <num>+{missionaryAdd}</num> 布教しました");
        }
        else
        {
          await game.CharacterLogAsync($"<town>{town.Name}</town> と交易し、お互い金 <num>{add}</num> を入手しました。布教は同盟条約のためできませんでした");
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
      ErrorCode.NotPermissionError.Throw();

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates);
    }
  }
}
