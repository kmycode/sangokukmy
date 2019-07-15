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
      var soldierTypeOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();
      var soldierNumberOptional = options.FirstOrDefault(p => p.Type == 2).ToOptional();
      var isDefaultSoldierTypeOptional = options.FirstOrDefault(p => p.Type == 3).ToOptional();
      var skills = await repo.Character.GetSkillsAsync(character.Id);

      if (!townOptional.HasData)
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
      }
      else if (!soldierTypeOptional.HasData || !soldierNumberOptional.HasData)
      {
        await game.CharacterLogAsync("徴兵のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
      }
      else if (!isDefaultSoldierTypeOptional.HasData)
      {
        await game.CharacterLogAsync("徴兵のパラメータが不正です。新しく追加されたコマンドパラメータが正常に設定されていない可能性がありますので、再度徴兵コマンドを入力してください。それでもダメな場合、１<emerge>管理者にお問い合わせください</emerge>");
      }
      else
      {
        var town = townOptional.Data;
        var soldierTypeName = string.Empty;
        var soldierType = (SoldierType)soldierTypeOptional.Data.NumberValue;
        var isDefaultSoldierType = isDefaultSoldierTypeOptional.Data.NumberValue == 0;

        CharacterSoldierTypeData soldierTypeData = null;
        if (isDefaultSoldierType)
        {
          var type = DefaultCharacterSoldierTypeParts.Get(soldierType).Data;
          if (type == null)
          {
            await game.CharacterLogAsync("ID: <num>" + (int)soldierType + "</num> の兵種は存在しません。<emerge>管理者にお問い合わせください</emerge>");
            return;
          }
          if (!type.CanConscript && character.AiType == CharacterAiType.Human)
          {
            await game.CharacterLogAsync($"{type.Name} を徴兵しようとしましたが、現在徴兵することはできません");
            return;
          }
          soldierTypeName = type.Name;
          soldierTypeData = DefaultCharacterSoldierTypeParts.GetDataByDefault(soldierType);
        }
        else
        {
          var typeOptional = await repo.CharacterSoldierType.GetByIdAsync((uint)soldierType);
          if (typeOptional.HasData)
          {
            if (typeOptional.Data.Status != CharacterSoldierStatus.Available)
            {
              await game.CharacterLogAsync($"カスタム兵種 {typeOptional.Data.Name} を徴兵しようとしましたが、その兵種は研究中または削除済です");
              return;
            }
            var type = typeOptional.Data;
            soldierTypeName = type.Name;
            soldierTypeData = type.ToParts().ToData();
          }
          else
          {
            await game.CharacterLogAsync($"ID: {soldierType} のカスタム兵種を徴兵することができませんでした。<emerge>管理者にお問い合わせください</emerge>");
            return;
          }
        }

        var moneyPerSoldier = soldierTypeData.Money;
        if (isDefaultSoldierType)
        {
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
        }

        var soldierNumber = soldierNumberOptional.Data.NumberValue;
        if (town.CountryId != character.CountryId)
        {
          await game.CharacterLogAsync("<town>" + town.Name + "</town>は自国の都市ではありません");
        }
        else if (soldierNumber == null)
        {
          await game.CharacterLogAsync("パラメータ soldierNumber の値がnullです。<emerge>管理者にお問い合わせください</emerge>");
        }
        else if (town.People < soldierNumber * Config.SoldierPeopleCost)
        {
          await game.CharacterLogAsync("農民が足りません。");
        }
        else if (town.Security < soldierNumber / 10)
        {
          await game.CharacterLogAsync("農民が拒否しました。");
        }
        else
        {
          var add = soldierNumber.Value;

          // 首都なら雑兵ではなく禁兵
          if (isDefaultSoldierType && soldierType == SoldierType.Common)
          {
            var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
            countryOptional.Some((country) =>
            {
              if (country.CapitalTownId == character.TownId)
              {
                soldierType = SoldierType.Guard;
              }
            });
          }

          if ((isDefaultSoldierType && character.SoldierType == soldierType) || (!isDefaultSoldierType && character.SoldierType == SoldierType.Custom && character.CharacterSoldierTypeId == (uint)soldierType))
          {
            // 兵種は変えない
            if (character.SoldierNumber + add > character.Leadership)
            {
              add = character.Leadership - character.SoldierNumber;
            }
            character.SoldierNumber += add;
          }
          else
          {
            // 兵種を変える
            if (add > character.Leadership)
            {
              add = character.Leadership;
            }
            character.SoldierNumber = add;
          }

          var needMoney = add * moneyPerSoldier;
          var discount = skills.GetSumOfValues(CharacterSkillEffectType.SoldierDiscountPercentage);
          needMoney = (int)(needMoney * (1.0f - discount / 100.0f));

          if (character.Money < needMoney)
          {
            await game.CharacterLogAsync("所持金が足りません。");
          }
          else
          {
            character.Proficiency -= (short)add;
            if (character.Proficiency < 0)
            {
              character.Proficiency = 0;
            }
            character.SoldierType = soldierType;
            if (!isDefaultSoldierType)
            {
              character.SoldierType = SoldierType.Custom;
              character.CharacterSoldierTypeId = (uint)soldierType;
            }
            character.Contribution += 10;
            character.Money -= (short)needMoney;
            town.People -= (int)(add * Config.SoldierPeopleCost);
            town.Security -= (short)(add / 10);

            await game.CharacterLogAsync(soldierTypeName + " を <num>+" + add + "</num> 徴兵しました");
            character.AddStrongEx(50);

            if (RandomService.Next(0, 120) <= (int)MathF.Log(needMoney))
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
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
      var town = await repo.Town.GetByIdAsync(chara.TownId).GetOrErrorAsync(ErrorCode.InternalDataNotFoundError, new { command = "soldier", townId = chara.TownId, });
      var soldierType = (SoldierType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var soldierNumber = options.FirstOrDefault(p => p.Type == 2).Or(ErrorCode.LackOfCommandParameter);
      var isDefaultSoldierType = options.FirstOrDefault(p => p.Type == 3).Or(ErrorCode.LackOfCommandParameter).NumberValue == 0;

      var soldierTypeData = isDefaultSoldierType ?
        DefaultCharacterSoldierTypeParts.GetDataByDefault(soldierType) :
        (await repo.CharacterSoldierType.GetByIdAsync((uint)soldierType)
          .GetOrErrorAsync(ErrorCode.InvalidCommandParameter))
          .ToParts()
          .ToData();

      if (soldierNumber.NumberValue == null || soldierNumber.NumberValue <= 0)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      if (isDefaultSoldierType)
      {
        // 禁兵は、雑兵と同じタイプにする（実行時判定なので）
        if (soldierType == SoldierType.Guard)
        {
          soldierType = SoldierType.Common;
        }
        else if (soldierType == SoldierType.Military)
        {
          var policies = await repo.Country.GetPoliciesAsync(chara.CountryId);
          if (!policies.GetAvailableTypes().Contains(CountryPolicyType.JusticeMessage))
          {
            ErrorCode.NotPermissionError.Throw();
          }
        }
        else if (soldierType == SoldierType.RepeatingCrossbow)
        {
          var skills = await repo.Character.GetSkillsAsync(chara.Id);
          if (!CharacterSkillInfoes.AnySkillEffects(skills, CharacterSkillEffectType.SoldierType, (int)SoldierType.RepeatingCrossbow))
          {
            ErrorCode.NotSkillError.Throw();
          }
        }
      }
      else
      {
        var type = await repo.CharacterSoldierType.GetByIdAsync((uint)soldierType);
        if (!type.HasData || type.Data.Status != CharacterSoldierStatus.Available || type.Data.CharacterId != characterId)
        {
          ErrorCode.InvalidCommandParameter.Throw();
        }
      }

      // 都市の支配国チェック
      if (soldierTypeData.Technology > 0 && (town.CountryId != chara.CountryId || town.Technology < soldierTypeData.Technology))
      {
        ErrorCode.LackOfTownTechnologyForSoldier.Throw();
      }

      // 入力
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
