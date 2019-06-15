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

namespace SangokuKmy.Models.Commands
{
  public class TownInvestCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.TownInvest;

    public static async Task<bool> ResultAsync(MainRepository repo, SystemData system, DelayEffect delay, Func<uint, string, Task> logAsync)
    {
      if (delay.IntAppearGameDateTime + 36 <= system.IntGameDateTime)
      {
        var chara = await repo.Character.GetByIdAsync(delay.CharacterId);
        var town = await repo.Town.GetByIdAsync(delay.TownId);
        if (chara.HasData && town.HasData)
        {
          var results = new List<string>();
          var skills = await repo.Character.GetSkillsAsync(chara.Data.Id);
          var currentItems = await repo.Character.GetItemsAsync(chara.Data.Id);
          if (CharacterService.GetItemMax(skills) > currentItems.Count)
          {
            var items = await repo.Town.GetItemsAsync(chara.Data.TownId);
            if (RandomService.Next(0, 10) < items.Count)
            {
              var item = RandomService.Next(items);
              var info = CharacterItemInfoes.Get(item.Type);
              if (info.HasData)
              {
                await ItemService.SetCharacterAsync(repo, item, chara.Data);
                results.Add($"アイテム {info.Data.Name}");
              }
            }
          }

          var money = RandomService.Next(chara.Data.Intellect * 2, Math.Max(chara.Data.Intellect * 4, system.GameDateTime.Year * 550 + 10000));
          chara.Data.Money += money;
          results.Add($"金 <num>{money}</num>");

          var country = await repo.Country.GetByIdAsync(chara.Data.CountryId);
          if (country.HasData && RandomService.Next(0, 3) == 0)
          {
            var policy = RandomService.Next(chara.Data.Intellect * 2, Math.Max(chara.Data.Intellect * 4, 100));
            country.Data.PolicyPoint += policy;
            results.Add($"政策ポイント <num>{policy}</num>");
          }
          
          var name = string.Empty;
          var add = RandomService.Next(20, 60);
          var target = RandomService.Next(0, 6);
          if (target == 0)
          {
            name = "農業";
            town.Data.Agriculture = Math.Min(town.Data.AgricultureMax, town.Data.Agriculture + add);
          }
          else if (target == 1)
          {
            name = "商業";
            town.Data.Commercial = Math.Min(town.Data.CommercialMax, town.Data.Commercial + add);
          }
          else if (target == 2)
          {
            name = "技術";
            town.Data.Technology = Math.Min(town.Data.TechnologyMax, town.Data.Technology + add);
          }
          else if (target == 3)
          {
            name = "城壁";
            town.Data.Wall = Math.Min(town.Data.WallMax, town.Data.Wall + add);
          }
          else if (target == 4)
          {
            name = "都市施設";
            town.Data.TownBuildingValue = Math.Min(Config.TownBuildingMax, town.Data.TownBuildingValue + add);
          }

          chara.Data.AddIntellectEx(500);
          await logAsync(chara.Data.Id, $"<town>{town.Data.Name}</town> に投資し、{name} の開発に <num>+{add}</num> 貢献し、{string.Join("と", results)}を得ました");

          return true;
        }
      }

      return false;
    }

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var money = game.GameDateTime.Year * 200 + 10000;
      if (character.Money < money)
      {
        await game.CharacterLogAsync("投資しようとしましたが、金が足りません。<num>" + money + "</num> 必要です");
        return;
      }

      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (townOptional.HasData)
      {
        var country = await repo.Country.GetByIdAsync(character.CountryId);

        var town = townOptional.Data;
        if (town.CountryId != character.CountryId && country.HasData && !country.Data.HasOverthrown)
        {
          await game.CharacterLogAsync($"<town>{town.Name}</town> で投資しようとしましたが、自国の都市ではありません");
          return;
        }

        var delays = await repo.DelayEffect.GetAllAsync();
        if (delays.Any(d => d.CharacterId == character.Id && d.Type == DelayEffectType.TownInvestment))
        {
          await game.CharacterLogAsync($"<town>{town.Name}</town> で投資しようとしましたが、複数の都市に同時に投資することはできません");
          return;
        }

        character.Money -= money;
        var delay = new DelayEffect
        {
          CharacterId = character.Id,
          CountryId = character.CountryId,
          AppearGameDateTime = game.GameDateTime,
          TownId = town.Id,
          Type = DelayEffectType.TownInvestment,
        };
        await repo.DelayEffect.AddAsync(delay);

        await game.CharacterLogAsync($"<town>{town.Name}</town> に投資しました。結果は数年後に来ます");
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
