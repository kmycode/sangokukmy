using System;
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
  public class TownPatrolCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.TownPatrol;

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
        var country = await repo.Country.GetByIdAsync(character.CountryId);

        var town = townOptional.Data;
        if (town.CountryId != character.CountryId && country.HasData && !country.Data.HasOverthrown)
        {
          await game.CharacterLogAsync($"<town>{town.Name}</town> で巡回しようとしましたが、自国の都市ではありません");
          return;
        }
        
        if (RandomService.Next(0, 300) == 0)
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
        else if (RandomService.Next(0, 100) == 0 && country.HasData && !country.Data.HasOverthrown)
        {
          var policies = await repo.Country.GetPoliciesAsync(country.Data.Id);
          var allPolicies = CountryPolicyTypeInfoes.GetAll();
          var notPolicies = allPolicies.Where(pi => !policies.Any(p => p.Status != CountryPolicyStatus.Unadopted && p.Status != CountryPolicyStatus.Boosting && p.Type == pi.Type));
          if (notPolicies.Any())
          {
            var info = RandomService.Next(notPolicies);
            await CountryService.SetPolicyAndSaveAsync(repo, country.Data, info.Type, CountryPolicyStatus.Boosted, false);
            await game.CharacterLogAsync($"<country>{country.Data.Name}</country> の政策 {info.Name} について新しい知見を得、政策をブーストしました");
          }
          else
          {
            await game.CharacterLogAsync($"政策について夜遅くまで討論しました。政策 <num>+100</num>、武力Ex <num>+300</num>、知力Ex <num>+300</num>");
            country.Data.PolicyPoint += 100;
            character.AddStrongEx(300);
            character.AddIntellectEx(300);
          }
        }
        else if (RandomService.Next(0, 250) == 0)
        {
          var formations = await repo.Character.GetFormationsAsync(character.Id);
          var allFormations = FormationTypeInfoes.GetAllGettables(formations);
          var notFormations = allFormations.Where(f => !formations.Any(ff => f.Type == ff.Type));
          if (notFormations.Any())
          {
            var info = RandomService.Next(notFormations);
            var formation = new Formation
            {
              CharacterId = character.Id,
              Level = 1,
              Type = info.Type,
            };
            await repo.Character.AddFormationAsync(formation);
            await StatusStreaming.Default.SendCharacterAsync(ApiData.From(formation), character.Id);
            await game.CharacterLogAsync($"陣形 {info.Name} を獲得しました");
          }
          else
          {
            await game.CharacterLogAsync($"金 <num>10000</num> を入手しました");
            character.Money += 10000;
          }
        }
        else if (RandomService.Next(0, 100) == 0)
        {
          await game.CharacterLogAsync($"技能ポイントを <num>+1</num> 獲得しました");
          character.SkillPoint += 1;
        }
        else
        {
          var targets = default(List<TownPatrolResult>);
          if (character.From == CharacterFrom.Warrior)
          {
            targets = new List<TownPatrolResult>
            {
              TownPatrolResult.Strong,
              TownPatrolResult.Wall,
              TownPatrolResult.WallMax,
              TownPatrolResult.Policy,
              TownPatrolResult.Money,
              TownPatrolResult.FormationPoint,
            };
          }
          else
          {
            targets = new List<TownPatrolResult>
            {
              TownPatrolResult.Strong,
              TownPatrolResult.Intellect,
              TownPatrolResult.Leadership,
              TownPatrolResult.Popularity,
              TownPatrolResult.Agriculture,
              TownPatrolResult.AgricultureMax,
              TownPatrolResult.Commercial,
              TownPatrolResult.CommercialMax,
              TownPatrolResult.Technology,
              TownPatrolResult.TechnologyMax,
              TownPatrolResult.Wall,
              TownPatrolResult.WallMax,
              TownPatrolResult.TownBuilding,
              TownPatrolResult.Policy,
              TownPatrolResult.Money,
              TownPatrolResult.FormationPoint,
            };
          }
          var result = RandomService.Next(targets);

          if (result == TownPatrolResult.Strong)
          {
            await game.CharacterLogAsync($"武力経験値が <num>+100</num> 上がりました");
            character.AddStrongEx(100);
          }
          else if (result == TownPatrolResult.Intellect)
          {
            await game.CharacterLogAsync($"知力経験値が <num>+100</num> 上がりました");
            character.AddIntellectEx(100);
          }
          else if (result == TownPatrolResult.Leadership)
          {
            await game.CharacterLogAsync($"統率経験値が <num>+100</num> 上がりました");
            character.AddLeadershipEx(100);
          }
          else if (result == TownPatrolResult.Popularity)
          {
            await game.CharacterLogAsync($"人望経験値が <num>+100</num> 上がりました");
            character.AddPopularityEx(100);
          }
          else if (result == TownPatrolResult.Gang)
          {
            if (RandomService.Next(0, character.Strong) < 50)
            {
              await game.CharacterLogAsync($"賊を発見し、戦闘しましたが敗北しました。民忠 <num>-1</num>、武力Ex <num>+30</num>");
              town.Security -= 1;
              character.AddStrongEx(30);
            }
            else
            {
              await game.CharacterLogAsync($"賊を発見し、撃退しました。民忠 <num>+2</num>、武力Ex <num>+100</num>");
              town.Security += 2;
              character.AddStrongEx(100);
            }
          }
          else if (result == TownPatrolResult.Agriculture)
          {
            await game.CharacterLogAsync($"水田を耕しました。農業 <num>+12</num>、知力Ex <num>+50</num>");
            town.Agriculture = Math.Min(town.AgricultureMax, town.Agriculture + 12);
            character.AddIntellectEx(50);
          }
          else if (result == TownPatrolResult.Commercial)
          {
            await game.CharacterLogAsync($"市場を開拓しました。商業 <num>+12</num>、知力Ex <num>+50</num>");
            town.Commercial = Math.Min(town.CommercialMax, town.Commercial + 12);
            character.AddIntellectEx(50);
          }
          else if (result == TownPatrolResult.Technology)
          {
            await game.CharacterLogAsync($"技術開発の手助けをしました。技術 <num>+12</num>、知力Ex <num>+50</num>");
            town.Technology = Math.Min(town.TechnologyMax, town.Technology + 12);
            character.AddIntellectEx(50);
          }
          else if (result == TownPatrolResult.Wall)
          {
            await game.CharacterLogAsync($"城壁を補強しました。城壁 <num>+12</num>、武力Ex <num>+50</num>");
            town.Wall = Math.Min(town.WallMax, town.Wall + 12);
            character.AddStrongEx(50);
          }
          else if (result == TownPatrolResult.TownBuilding)
          {
            await game.CharacterLogAsync($"都市施設を強化しました。都市施設 <num>+20</num>、知力Ex <num>+50</num>");
            town.TownBuildingValue = Math.Min(Config.TownBuildingMax, town.TownBuildingValue + 20);
            character.AddIntellectEx(50);
          }
          else if (result == TownPatrolResult.AgricultureMax)
          {
            await game.CharacterLogAsync($"新たな耕地を発見しました。農業最大 <num>+4</num>、武力Ex <num>+50</num>");
            town.AgricultureMax += 4;
            character.AddStrongEx(50);
          }
          else if (result == TownPatrolResult.CommercialMax)
          {
            await game.CharacterLogAsync($"市場用の用地を確保しました。商業最大 <num>+4</num>、武力Ex <num>+50</num>");
            town.CommercialMax += 4;
            character.AddStrongEx(50);
          }
          else if (result == TownPatrolResult.TechnologyMax)
          {
            await game.CharacterLogAsync($"工房を拡張しました。技術最大 <num>+4</num>、武力Ex <num>+50</num>");
            town.TechnologyMax += 4;
            character.AddStrongEx(50);
          }
          else if (result == TownPatrolResult.WallMax)
          {
            await game.CharacterLogAsync($"城壁を拡張しました。城壁最大 <num>+4</num>、武力Ex <num>+50</num>");
            town.WallMax += 4;
            character.AddStrongEx(50);
          }
          else if (result == TownPatrolResult.Policy)
          {
            await game.CharacterLogAsync($"政策について討論しました。政策 <num>+4</num>、武力Ex <num>+50</num>");
            if (country.HasData && !country.Data.HasOverthrown)
            {
              country.Data.PolicyPoint += 4;
            }
            character.AddStrongEx(50);
          }
          else if (result == TownPatrolResult.Money)
          {
            var m = RandomService.Next(Math.Min(character.Strong, character.Intellect), 2000);
            await game.CharacterLogAsync($"金 <num>{m}</num> を発見しました");
            character.Money += m;
          }
          else if (result == TownPatrolResult.FormationPoint)
          {
            await game.CharacterLogAsync($"陣形ポイント <num>2</num> を獲得しました");
            character.FormationPoint += 2;
          }
        }

        if (country.HasData && !country.Data.HasOverthrown)
        {
          character.Contribution += 20;
        }
        character.Money -= 50;
      }
      else
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
      }
    }

    private enum TownPatrolResult
    {
      Strong,
      Intellect,
      Leadership,
      Popularity,
      Gang,
      Agriculture,
      Commercial,
      Technology,
      Wall,
      TownBuilding,
      AgricultureMax,
      CommercialMax,
      TechnologyMax,
      WallMax,
      Policy,
      Money,
      FormationPoint,
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var skills = await repo.Character.GetSkillsAsync(characterId);
      if (!skills.AnySkillEffects(CharacterSkillEffectType.Command, (int)this.Type))
      {
        ErrorCode.InvalidOperationError.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates);
    }
  }
}
