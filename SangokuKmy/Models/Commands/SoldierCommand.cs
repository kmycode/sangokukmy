using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Services;
using SangokuKmy.Streamings;
using System.Collections;

namespace SangokuKmy.Models.Commands
{
  /// <summary>
  /// 徴兵
  /// </summary>
  public class SoldierCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Soldier;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
      var soldierTypeOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();
      var soldierNumberOptional = options.FirstOrDefault(p => p.Type == 2).ToOptional();
      var skills = await repo.Character.GetSkillsAsync(character.Id);
      var items = await repo.Character.GetItemsAsync(character.Id);
      var onSucceeds = new List<Func<Task>>();

      if (!townOptional.HasData)
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
      }
      else if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync("徴兵しようとしましたが、国に所属していません");
      }
      else if (!soldierTypeOptional.HasData || !soldierNumberOptional.HasData)
      {
        await game.CharacterLogAsync("徴兵のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
      }
      else
      {
        var town = townOptional.Data;
        var country = countryOptional.Data;
        var soldierTypeName = string.Empty;
        var soldierType = (SoldierType)soldierTypeOptional.Data.NumberValue;
        var soldierNumber = soldierNumberOptional.Data.NumberValue;

        var peopleCost = Config.SoldierPeopleCost;
        if (country.IsLargeCountryPenalty)
        {
          peopleCost *= 1.1f;
        }

        if (soldierNumber == null)
        {
          await game.CharacterLogAsync("パラメータ soldierNumber の値がnullです。<emerge>管理者にお問い合わせください</emerge>");
          return;
        }

        // 首都なら雑兵ではなく禁兵
        if (soldierType == SoldierType.Common)
        {
          if (country.CapitalTownId == character.TownId)
          {
            soldierType = SoldierType.Guard;
          }
        }

        // 実際に徴兵する数を計算する
        // soldierNumber: 入力値、add: 実際に徴兵する数
        var add = soldierNumber.Value;
        if (character.SoldierType == soldierType)
        {
          // 兵種は変えない
          if (character.SoldierNumber + add > character.Leadership)
          {
            add = character.Leadership - character.SoldierNumber;
          }
          onSucceeds.Add(async () => character.SoldierNumber += add);
        }
        else
        {
          // 兵種を変える
          if (add > character.Leadership)
          {
            add = character.Leadership;
          }
          onSucceeds.Add(async () => character.SoldierNumber = add);
        }

        CharacterSoldierTypeData soldierTypeData = null;

        var type = DefaultCharacterSoldierTypeParts.Get(soldierType).Data;
        if (type == null)
        {
          await game.CharacterLogAsync("ID: <num>" + (int)soldierType + "</num> の兵種は存在しません。<emerge>管理者にお問い合わせください</emerge>");
          return;
        }
        if (!type.CanConscript && character.AiType == CharacterAiType.Human)
        {
          await game.CharacterLogAsync($"{type.Name} を徴兵しようとしましたが、その兵種を人間が徴兵することはできません");
          return;
        }
        if (!type.CanConscriptWithoutResource && character.AiType == CharacterAiType.Human)
        {
          // 資源による徴兵可能判定
          var resources = items.GetResourcesAvailable(CharacterItemEffectType.AddSoldierType, ef => ef.Value == (int)soldierType, (int)soldierNumber);
          var needResources = add;
          foreach (var resource in resources)
          {
            var newResource = Math.Max(0, resource.Item.Resource - needResources);
            onSucceeds.Add(async () => await SpendResourceAsync(resource.Item, resource.Info, newResource));

            needResources -= resource.Item.Resource;
          }

          if (needResources == add)
          {
            await game.CharacterLogAsync($"{type.Name} を徴兵しようとしましたが、資源が必要なため徴兵できませんでした");
            return;
          }

          // needResourcesは負になることもある。資源が足りなかった時は徴兵数を資源にそろえる
          add -= Math.Max(needResources, 0);
        }
        soldierTypeName = type.Name;
        soldierTypeData = DefaultCharacterSoldierTypeParts.GetDataByDefault(soldierType);

        var moneyPerSoldier = soldierTypeData.Money;
        var discountMoney = 0;

        var policies = (await repo.Country.GetPoliciesAsync(character.CountryId)).GetAvailableTypes();
        if (soldierType == SoldierType.Seiran)
        {
          if (policies.Contains(CountryPolicyType.Siege))
          {
            moneyPerSoldier -= 50;
          }
        }
        else if (soldierType == SoldierType.HeavyCavalry)
        {
          if (policies.Contains(CountryPolicyType.GetTerrorists))
          {
            moneyPerSoldier /= 2;
          }
        }
        if (country.Civilization == CountryCivilization.B)
        {
          moneyPerSoldier = (short)(moneyPerSoldier * 0.92f);
        }

        if (town.CountryId != character.CountryId)
        {
          await game.CharacterLogAsync("<town>" + town.Name + "</town> は自国の都市ではありません");
        }
        else if (town.People < add * peopleCost)
        {
          await game.CharacterLogAsync("徴兵しようとしましたが、農民が足りません。");
        }
        else if (type.Kind == SoldierKind.Battle && town.Security < add / 10)
        {
          await game.CharacterLogAsync("徴兵しようとしましたが、都市の民忠不足のため、農民が拒否しました。");
        }
        else if (type.Kind == SoldierKind.Religion && town.GetReligionPoint(country.Religion) < add / 10)
        {
          await game.CharacterLogAsync("徴兵しようとしましたが、都市の宗教ポイント不足のため、農民が拒否しました。");
        }
        else
        {
          // 資源による割引
          IEnumerable<(CharacterItem Item, CharacterItemInfo Info, CharacterResourceItemEffect Effect)> resources = null;
          if (soldierTypeData.TypeInfantry > 0)
          {
            resources = items.GetResourcesAvailable(CharacterItemEffectType.DiscountInfantrySoldierPercentage, ef => true, add);
          }
          else if (soldierTypeData.TypeCavalry > 0)
          {
            resources = items.GetResourcesAvailable(CharacterItemEffectType.DiscountCavalrySoldierPercentage, ef => true, add);
          }
          else if (soldierTypeData.TypeCrossbow > 0)
          {
            resources = items.GetResourcesAvailable(CharacterItemEffectType.DiscountCrossbowSoldierPercentage, ef => true, add);
          }

          // 特定兵種の割引
          {
            var discountResources = items.GetResourcesAvailable(CharacterItemEffectType.DiscountSoldierPercentageWithResource, ef => ef.DiscountSoldierTypes.Contains(soldierType), add);
            resources = resources?.Concat(discountResources) ?? discountResources;
          }
          resources = resources.OrderByDescending(r => r.Info.ResourceLevel);

          var needResources = add;
          var lastResourceLevel = resources.FirstOrDefault().Info?.ResourceLevel ?? 0;
          foreach (var resource in resources)
          {
            discountMoney += (int)(Math.Min(resource.Item.Resource, needResources) * moneyPerSoldier * (resource.Effect.Value / 100.0f));

            var newResource = Math.Max(0, resource.Item.Resource - needResources);
            onSucceeds.Add(async () => await SpendResourceAsync(resource.Item, resource.Info, newResource));

            if (lastResourceLevel != resource.Info.ResourceLevel)
            {
              needResources -= resource.Item.Resource;
              lastResourceLevel = resource.Info.ResourceLevel;
            }
          }

          var needMoney = add * moneyPerSoldier - discountMoney;
          var discount = skills.GetSumOfValues(CharacterSkillEffectType.SoldierDiscountPercentage);
          needMoney = (int)(needMoney * (1.0f - discount / 100.0f));

          if (character.Money < needMoney)
          {
            await game.CharacterLogAsync("所持金が足りません。");
          }
          else
          {
            // 資源による訓練値下限
            var minProficiency = (short)0;
            resources = items.GetResourcesAvailable(CharacterItemEffectType.ProficiencyMinimum, ef => true, add);
            needResources = add;
            lastResourceLevel = resources.FirstOrDefault().Info?.ResourceLevel ?? 0;
            foreach (var resource in resources)
            {
              var newResource = Math.Max(0, resource.Item.Resource - needResources);
              onSucceeds.Add(async () => await SpendResourceAsync(resource.Item, resource.Info, newResource));

              if (lastResourceLevel != resource.Info.ResourceLevel)
              {
                needResources -= resource.Item.Resource;
                lastResourceLevel = resource.Info.ResourceLevel;
              }

              if (resource.Effect.Type == CharacterItemEffectType.ProficiencyMinimum && minProficiency < resource.Effect.Value)
              {
                minProficiency = (short)resource.Effect.Value;
              }
            }

            character.Proficiency -= (short)add;
            if (character.Proficiency < minProficiency)
            {
              character.Proficiency = minProficiency;
            }
            character.SoldierType = soldierType;
            character.Contribution += 10;
            character.SkillPoint++;
            character.Money -= needMoney;
            town.People -= (int)(add * peopleCost);
            if (type.Kind == SoldierKind.Battle)
            {
              town.Security -= (short)(add / 10);
            }
            else
            {
              if (country.Religion == ReligionType.Buddhism)
              {
                town.Buddhism -= add / 10;
              }
              if (country.Religion == ReligionType.Taoism)
              {
                town.Taoism -= add / 10;
              }
              if (country.Religion == ReligionType.Confucianism)
              {
                town.Confucianism -= add / 10;
              }
            }

            await game.CharacterLogAsync($"金 <num>{needMoney}</num> を費やして、{soldierTypeName} を <num>+{add}</num> 徴兵しました");
            character.AddLeadershipEx(50);

            foreach (var onSucceed in onSucceeds)
            {
              await onSucceed();
            }

            var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
            if (wars.Any(w => (w.InsistedCountryId == character.CountryId || w.RequestedCountryId == character.CountryId) && (w.Status != CountryWarStatus.None && w.Status != CountryWarStatus.Stoped)) &&
              RandomService.Next(0, 128) == 0)
            {
              var info = await ItemService.PickTownHiddenItemAsync(repo, character.TownId, character);
              if (info.HasData)
              {
                await game.CharacterLogAsync($"<town>{town.Name}</town> に隠されたアイテム {info.Data.Name} を手に入れました");
              }
            }
          }
        }
      }


      async Task SpendResourceAsync(CharacterItem item, CharacterItemInfo info, int newResource)
      {
        item.Resource = newResource;
        if (item.Resource <= 0)
        {
          await ItemService.SpendCharacterAsync(repo, item, character);
          await game.CharacterLogAsync($"アイテム {info.Name} はすべての資源を使い果たし、消滅しました");
        }
        else
        {
          await StatusStreaming.Default.SendCharacterAsync(ApiData.From(item), character.Id);
        }
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
      var town = await repo.Town.GetByIdAsync(chara.TownId).GetOrErrorAsync(ErrorCode.InternalDataNotFoundError, new { command = "soldier", townId = chara.TownId, });
      var soldierType = (SoldierType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var soldierNumber = options.FirstOrDefault(p => p.Type == 2).Or(ErrorCode.LackOfCommandParameter);

      var soldierTypeData = DefaultCharacterSoldierTypeParts.GetDataByDefault(soldierType);
      var policies = (await repo.Country.GetPoliciesAsync(chara.CountryId)).GetAvailableTypes();

      if (soldierNumber.NumberValue == null || soldierNumber.NumberValue <= 0)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      var info = DefaultCharacterSoldierTypeParts.Get(soldierType).GetOrError(ErrorCode.InvalidCommandParameter);

      // 禁兵は、雑兵と同じタイプにする（実行時判定なので）
      if (soldierType == SoldierType.Guard)
      {
        soldierType = SoldierType.Common;
      }
      else if (soldierType == SoldierType.Military)
      {
        if (!policies.Contains(CountryPolicyType.JusticeMessage))
        {
          var skills = await repo.Character.GetSkillsAsync(chara.Id);
          if (!skills.AnySkillEffects(CharacterSkillEffectType.SoldierType, (int)SoldierType.Military))
          {
            ErrorCode.NotPermissionError.Throw();
          }
        }
      }
      else
      {
        if (info.CanConscriptWithoutResource && !info.CanConscriptWithoutSkill)
        {
          var skills = await repo.Character.GetSkillsAsync(chara.Id);
          if (!CharacterSkillInfoes.AnySkillEffects(skills, CharacterSkillEffectType.SoldierType, (int)soldierType))
          {
            ErrorCode.NotSkillError.Throw();
          }
        }
      }

      // 徴兵可能な都市特化チェック
      if (info.TownTypes != null && !info.TownTypes.Contains(town.Type) && !info.TownTypes.Contains(town.SubType))
      {
        ErrorCode.NotTownTypeError.Throw();
      }

      // 都市の支配国チェック
      if (soldierTypeData.Technology > 0 && (town.CountryId != chara.CountryId || town.Technology < soldierTypeData.Technology))
      {
        ErrorCode.LackOfTownTechnologyForSoldier.Throw();
      }

      // 建築物チェック
      if (!info.CanConscriptWithoutSubBuilding)
      {
        var sbs = await repo.Town.GetSubBuildingsAsync(town.Id);
        if (!sbs.Any(s => s.Type == info.NeededSubBuildingType))
        {
          ErrorCode.LackOfTownSubBuildingForSoldier.Throw();
        }
      }

      // 入力
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
