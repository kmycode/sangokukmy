using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Commands
{
  /// <summary>
  /// 徴兵
  /// </summary>
  public class SoldierCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Soldier;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, Func<string, Task> loggerAsync)
    {
      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      var soldierTypeOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();
      var soldierNumberOptional = options.FirstOrDefault(p => p.Type == 2).ToOptional();

      if (!townOptional.HasData)
      {
        await loggerAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
      }
      else if (!soldierTypeOptional.HasData || !soldierNumberOptional.HasData)
      {
        await loggerAsync("徴兵のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
      }
      else
      {
        var town = townOptional.Data;
        var soldierType = (SoldierType)soldierTypeOptional.Data.NumberValue;
        var soldierTypeDataOptional = SoldierTypes.Get(soldierType);
        var soldierNumber = soldierNumberOptional.Data.NumberValue;
        if (town.CountryId != character.CountryId)
        {
          await loggerAsync("<town>" + town.Name + "</town>は自国の都市ではありません");
        }
        else if (!soldierTypeDataOptional.HasData)
        {
          await loggerAsync("種類 " + soldierType + " の兵種は徴兵することができません。<emerge>管理者にお問い合わせください</emerge>");
        }
        else if (soldierNumber == null)
        {
          await loggerAsync("パラメータ soldierNumber の値がnullです。<emerge>管理者にお問い合わせください</emerge>");
        }
        else if (character.SoldierNumber >= character.Leadership)
        {
          await loggerAsync("兵士数が最大です。");
        }
        else if (character.Money < soldierTypeDataOptional.Data.Money * soldierNumber)
        {
          await loggerAsync("所持金が足りません。");
        }
        else if (town.People < soldierNumber * 5)
        {
          await loggerAsync("農民が足りません。");
        }
        else if (town.Security < soldierNumber / 10)
        {
          await loggerAsync("農民が拒否しました。");
        }
        else
        {
          var add = soldierNumber.Value;

          // 首都なら雑兵ではなく禁兵
          if (soldierType == SoldierType.Common)
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

          if (character.SoldierType == soldierType)
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

          character.Proficiency -= (short)add;
          if (character.Proficiency < 0)
          {
            character.Proficiency = 0;
          }
          character.SoldierType = soldierType;
          character.Contribution += 10;
          character.Money -= add * soldierTypeDataOptional.Data.Money;
          town.People -= add * 5;
          town.Security -= (short)(add / 10);

          await loggerAsync(soldierTypeDataOptional.Data.Name + " を <num>+" + add + "</num> 徴兵しました");
          character.AddStrongEx(50);
        }
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
      var town = await repo.Town.GetByIdAsync(chara.TownId).GetOrErrorAsync(ErrorCode.InternalDataNotFoundError, new { command = "soldier", townId = chara.TownId, });
      var soldierType = (SoldierType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var soldierNumber = options.FirstOrDefault(p => p.Type == 2).Or(ErrorCode.LackOfCommandParameter);

      if (!SoldierTypes.Get(soldierType).HasData)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }
      if (soldierNumber.NumberValue <= 0)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      // 禁兵は、雑兵と同じタイプにする（実行時判定なので）
      if (soldierType == SoldierType.Guard)
      {
        soldierType = SoldierType.Common;
      }

      // 都市の技術チェック
      if ((soldierType != SoldierType.Common && town.Technology < 9999))
      {
        ErrorCode.LackOfTownTechnologyForSoldier.Throw();
      }

      // 入力
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
