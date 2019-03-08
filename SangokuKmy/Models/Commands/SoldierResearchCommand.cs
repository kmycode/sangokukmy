using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  public class SoldierResearchCommand : Command
  {
    private static readonly Random rand = new Random(DateTime.Now.Millisecond);

    public override CharacterCommandType Type => CharacterCommandType.ResearchSoldier;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      if (character.CountryId == 0)
      {
        await game.CharacterLogAsync($"兵種研究をしようとしましたが、無所属は実行できません");
        return;
      }

      var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync($"ID: <num>{character.CountryId}</num> の国は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }

      var country = countryOptional.Data;
      if (country.HasOverthrown)
      {
        await game.CharacterLogAsync($"兵種研究をしようとしましたが、あなたの所属国 <country>{country.Name}</country> はすでに滅亡しています");
        return;
      }

      var soldierTypeId = options.FirstOrDefault(o => o.Type == 1)?.NumberValue ?? 0;
      var soldierTypeOptional = await repo.CharacterSoldierType.GetByIdAsync((uint)soldierTypeId);
      if (!soldierTypeOptional.HasData)
      {
        await game.CharacterLogAsync($"ID: <num>{soldierTypeId}</num> の兵種は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var soldierType = soldierTypeOptional.Data;

      if (soldierType.CharacterId != character.Id)
      {
        await game.CharacterLogAsync($"{soldierType.Name} はあなたの兵種ではありません");
        return;
      }
      var soldierTypeData = soldierType.ToParts().ToData();

      var buildingSize = await CountryService.GetCountryBuildingSizeAsync(repo, character.CountryId, CountryBuilding.SoldierLaboratory);
      if (buildingSize <= 0.0f)
      {
        await game.CharacterLogAsync($"兵種研究をしようとしましたが、対応する国家施設がないか、必要な耐久がありません");
        return;
      }

      if (soldierType.Status == CharacterSoldierStatus.InDraft)
      {
        var researchMoney = (int)(soldierTypeData.ResearchMoneyBase / buildingSize);
        var researchCost = (int)(soldierTypeData.ResearchCostBase / buildingSize);

        if (character.Money < researchMoney)
        {
          await game.CharacterLogAsync($"兵種 {soldierType.Name} の研究には初期費用 <num>{researchMoney}</num> が必要ですが、所持金が足りません");
          return;
        }

        character.Money -= researchMoney;
        soldierType.ResearchCost = (short)researchCost;
        soldierType.Status = CharacterSoldierStatus.Available;
        await game.CharacterLogAsync($"兵種 {soldierType.Name} の研究の初期費用として <num>{researchMoney}</num> を消費しました");
        await game.CharacterLogAsync($"兵種 {soldierType.Name} の研究のコストは <num>{researchCost}</num> で確定しました");
      }
      else if (soldierType.Status == CharacterSoldierStatus.Available)
      {
        await game.CharacterLogAsync($"兵種 {soldierType.Name} の研究はすでに終了しています");
      }
      else if (soldierType.Status == CharacterSoldierStatus.Removed)
      {
        await game.CharacterLogAsync($"兵種 {soldierType.Name} は削除されています");
        return;
      }

      if (soldierType.Status == CharacterSoldierStatus.Researching)
      {
        var add = (short)(character.Leadership / 20.0f + rand.Next(0, character.Leadership) / 40.0f);
        if (add > soldierType.ResearchCost)
        {
          add = soldierType.ResearchCost;
        }
        soldierType.ResearchCost -= add;

        await game.CharacterLogAsync($"兵種 {soldierType.Name} を <num>{add}</num> 研究しました（研究コスト残り<num>{soldierType.ResearchCost}</num>）");

        if (soldierType.ResearchCost <= 0)
        {
          soldierType.Status = CharacterSoldierStatus.Available;
          await game.CharacterLogAsync($"兵種 {soldierType.Name} は使用可能になりました");
        }
      }

      character.Contribution += 30;
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(soldierType), character.Id);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
      var country = await repo.Country.GetAliveByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
      var type = options.FirstOrDefault(o => o.Type == 1).Or(ErrorCode.LackOfParameterError).NumberValue;

      if (!(await repo.Town.GetByCountryIdAsync(country.Id)).Any(t => t.CountryBuilding == CountryBuilding.SoldierLaboratory))
      {
        ErrorCode.InvalidOperationError.Throw();
      }

      var typeObj = await repo.CharacterSoldierType.GetByIdAsync((uint)type);
      if (!typeObj.HasData)
      {
        ErrorCode.SoldierTypeNotFoundError.Throw();
      }
      if (typeObj.Data.Status != CharacterSoldierStatus.InDraft && typeObj.Data.Status != CharacterSoldierStatus.Researching)
      {
        ErrorCode.InvalidOperationError.Throw();
      }
      if (typeObj.Data.CharacterId != characterId)
      {
        ErrorCode.NotPermissionError.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
