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
  public class PolicyCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Policy;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      if (character.Money < 50)
      {
        await game.CharacterLogAsync($"金が足りません。<num>50</num> 必要です");
        return;
      }

      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (townOptional.HasData)
      {
        var countryOptional = await repo.Country.GetAliveByIdAsync(character.CountryId);
        if (!countryOptional.HasData)
        {
          await game.CharacterLogAsync($"政策開発しようとしましたが、無所属は実行できません");
          return;
        }
        var country = countryOptional.Data;

        var town = townOptional.Data;
        var isWandering = false;
        if (town.CountryId != character.CountryId)
        {
          var townsCount = await repo.Town.CountByCountryIdAsync(character.CountryId);
          if (townsCount > 0)
          {
            await game.CharacterLogAsync("<town>" + town.Name + "</town>で政策開発しようとしましたが、自国の都市ではありません");
            return;
          }
          else
          {
            // 放浪軍による政策開発
            isWandering = true;
          }
        }

        var skills = await repo.Character.GetSkillsAsync(character.Id);

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

        // 政策ブースト
        if (RandomService.Next(0, 1000) <= skills.GetSumOfValues(CharacterSkillEffectType.PolicyBoostProbabilityThousandth))
        {
          var policies = await repo.Country.GetPoliciesAsync(country.Id);
          var allPolicies = CountryPolicyTypeInfoes.GetAll();
          var notPolicies = allPolicies.Where(pi => pi.CanBoost && !policies.Any(p => p.Status != CountryPolicyStatus.Unadopted && p.Status != CountryPolicyStatus.Boosting && p.Type == pi.Type));
          if (notPolicies.Any())
          {
            var info = RandomService.Next(notPolicies);
            await CountryService.SetPolicyAndSaveAsync(repo, country, info.Type, CountryPolicyStatus.Boosted, false);
            await game.CharacterLogAsync($"<country>{country.Name}</country> の政策 {info.Name} について新しい知見を得、政策をブーストしました");
          }
        }

        // 経験値、金の増減
        character.Contribution += 30;
        character.SkillPoint++;
        character.Money -= 50;
        if (character.Strong > character.Intellect)
        {
          character.AddStrongEx(50);
        }
        else
        {
          character.AddIntellectEx(50);
        }

        if (isWandering)
        {
          await game.CharacterLogAsync($"<country>{country.Name}</country> の政策ポイントを <num>+{add}</num> 上げました。合計: <num>{country.PolicyPoint}</num>");
        }
        else
        {
          await game.CharacterLogAsync($"<country>{country.Name}</country> の政策ポイントを <num>+{add}</num> 上げました");
        }

        if (RandomService.Next(0, 256) == 0)
        {
          var info = await ItemService.PickTownHiddenItemAsync(repo, character.TownId, character);
          if (info.HasData)
          {
            await game.CharacterLogAsync($"<town>{town.Name}</town> に隠されたアイテム {info.Data.Name} を手に入れました");
          }
        }

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
