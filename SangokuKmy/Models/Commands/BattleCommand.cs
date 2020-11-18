using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  public class BattleCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Battle;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      Optional<CountryWar> warOptional = default;

      if (character.CountryId == 0)
      {
        await game.CharacterLogAsync("あなたの国はすでに滅亡しているか無所属です。無所属は侵攻できません");
        return;
      }
      var myCountryOptional = await repo.Country.GetByIdAsync(character.CountryId);
      if (!myCountryOptional.HasData)
      {
        await game.CharacterLogAsync("ID " + character.CountryId + " の国は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var myCountry = myCountryOptional.Data;
      if (myCountry.HasOverthrown)
      {
        await game.CharacterLogAsync("あなたの国はすでに滅亡しています");
        return;
      }
      if (myCountry.IntEstablished + Config.CountryBattleStopDuring > game.GameDateTime.ToInt())
      {
        await game.CharacterLogAsync("あなたの国はまだ戦闘解除されていません");
        return;
      }

      var targetTownId = options.FirstOrDefault(p => p.Type == 1)?.NumberValue;
      if (targetTownId == null)
      {
        await game.CharacterLogAsync("targetTownId の値はnullです。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      if ((uint)targetTownId == character.TownId)
      {
        await game.CharacterLogAsync("自分の現在の所在都市へ侵攻しようとしました");
        return;
      }

      // 侵攻に失敗した時、移動するか
      var isMoveIfFailedValue = options.FirstOrDefault(p => p.Type == 2)?.NumberValue;
      var isMoveIfFailed = isMoveIfFailedValue != null && isMoveIfFailedValue != 0;

      var targetTownOptional = await repo.Town.GetByIdAsync((uint)targetTownId);
      var myTownOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (!targetTownOptional.HasData)
      {
        await game.CharacterLogAsync("ID " + targetTownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      if (!myTownOptional.HasData)
      {
        await game.CharacterLogAsync("ID " + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }

      var targetTown = targetTownOptional.Data;
      var myTown = myTownOptional.Data;
      if (myTown.CountryId != character.CountryId)
      {
        await game.CharacterLogAsync($"<town>{targetTown.Name}</town> へ <town>{myTown.Name}</town> から攻め込もうとしましたが、自国以外の都市からは攻められません");
        return;
      }
      if (!targetTown.IsNextToTown(myTown))
      {
        await game.CharacterLogAsync($"<town>{targetTown.Name}</town> に侵攻しようとしましたが、<town>{myTown.Name}</town> とは隣接していません");
        return;
      }

      var targetCountryOptional = await repo.Country.GetByIdAsync(targetTown.CountryId);
      var system = await repo.System.GetAsync();
      var cause = BattleCause.War;
      if (targetCountryOptional.HasData && !system.IsBattleRoyaleMode)
      {
        if (targetCountryOptional.Data.IntEstablished + Config.CountryBattleStopDuring > game.GameDateTime.ToInt())
        {
          await game.CharacterLogAsync("<town>" + targetTown.Name + "</town> の所持国 <country>" + targetCountryOptional.Data.Name + "</country> は戦闘解除されていません");
          return;
        }

        if (myCountry.AiType != CountryAiType.Thiefs && targetCountryOptional.Data.AiType != CountryAiType.Thiefs)
        {
          warOptional = await repo.CountryDiplomacies.GetCountryWarAsync(character.CountryId, targetTown.CountryId);
          var targetTownWarOptional = await repo.CountryDiplomacies.GetTownWarAsync(targetTown.CountryId, character.CountryId, targetTown.Id);

          var isTownWar = false;
          if (targetTownWarOptional.HasData)
          {
            var townWar = targetTownWarOptional.Data;
            if (townWar.Status == TownWarStatus.Available && townWar.RequestedCountryId == character.CountryId)
            {
              isTownWar = true;
              cause = BattleCause.TownWar;
            }
          }

          if (!isTownWar && targetCountryOptional.Data.AiType != CountryAiType.Farmers)
          {
            if (!warOptional.HasData)
            {
              await game.CharacterLogAsync("<town>" + targetTown.Name + "</town> の所持国 <country>" + targetCountryOptional.Data.Name + "</country> とは宣戦の関係にありません");
              return;
            }
            var war = warOptional.Data;
            if (war.Status == CountryWarStatus.Available || war.Status == CountryWarStatus.StopRequesting || war.Status == CountryWarStatus.InReady)
            {
              if (war.StartGameDate.ToInt() > game.GameDateTime.ToInt())
              {
                await game.CharacterLogAsync("<town>" + targetTown.Name + "</town> の所持国 <country>" + targetCountryOptional.Data.Name + "</country> とはまだ開戦していません");
                return;
              }
            }
            else
            {
              await game.CharacterLogAsync("<town>" + targetTown.Name + "</town> の所持国 <country>" + targetCountryOptional.Data.Name + "</country> とは宣戦の関係にありません");
              return;
            }
          }
        }
      }

      var isWaring = warOptional.HasData && warOptional.Data.IntStartGameDate <= system.IntGameDateTime;
      if (character.SoldierNumber <= 0)
      {
        await game.CharacterLogAsync("兵士がいません");
        if (isWaring)
        {
          character.SkillPoint++;
        }
        return;
      }
      if (targetTown.CountryId == character.CountryId)
      {
        if (isWaring)
        {
          character.SkillPoint++;
        }
        if (isMoveIfFailed)
        {
          await CharacterService.ChangeTownAsync(repo, targetTown.Id, character);
          await game.CharacterLogAsync($"自国の <town>{targetTown.Name}</town> へ侵攻しようとしました。代わりに移動しました");
        }
        else
        {
          await game.CharacterLogAsync($"自国の <town>{targetTown.Name}</town> へ侵攻しようとしました");
        }
        return;
      }

      var mySoldierTypeInfo = DefaultCharacterSoldierTypeParts.Get(character.SoldierType).Data;
      if (mySoldierTypeInfo == null)
      {
        await game.CharacterLogAsync($"ID: {character.SoldierType} の兵種は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      if (mySoldierTypeInfo.Kind == SoldierKind.Religion && myCountry.Religion == targetCountryOptional.Data?.Religion)
      {
        await game.CharacterLogAsync($"同じ宗教を国教とする国同士の戦闘で兵種 {mySoldierTypeInfo.Name} を用いることはできません");
        return;
      }
      if (mySoldierTypeInfo.Kind == SoldierKind.Religion && warOptional.HasData && warOptional.Data.Mode != CountryWarMode.Religion)
      {
        await game.CharacterLogAsync($"兵種 {mySoldierTypeInfo.Name} は、宗教戦争以外で使うことはできません");
        return;
      }
      if (mySoldierTypeInfo.Kind != SoldierKind.Religion && warOptional.HasData && warOptional.Data.Mode == CountryWarMode.Religion)
      {
        await game.CharacterLogAsync($"兵種 {mySoldierTypeInfo.Name} は、宗教戦争で使うことはできません");
        return;
      }
      if (mySoldierTypeInfo.Kind == SoldierKind.Religion && (targetTown.CountryId == 0 || !targetCountryOptional.HasData))
      {
        await game.CharacterLogAsync($"兵種 {mySoldierTypeInfo.Name} は、無所属都市との戦闘で使うことはできません");
        return;
      }
      if (mySoldierTypeInfo.Kind == SoldierKind.Religion && system.IsBattleRoyaleMode)
      {
        await game.CharacterLogAsync($"兵種 {mySoldierTypeInfo.Name} は、全国戦争で使うことはできません");
        return;
      }

      if (targetCountryOptional.HasData && targetCountryOptional.Data.AiType == CountryAiType.Farmers && !warOptional.HasData)
      {
        var farmerWars = (await repo.CountryDiplomacies.GetAllWarsAsync()).Where(w => w.IsJoinAvailable(targetCountryOptional.Data.Id));
        if (!system.IsBattleRoyaleMode && farmerWars.Any(w => w.Mode == CountryWarMode.Religion))
        {
          if (mySoldierTypeInfo.Kind != SoldierKind.Religion)
          {
            await game.CharacterLogAsync($"兵種 {mySoldierTypeInfo.Name} は、宗教戦争を布告している農民国家に対して使うことはできません");
            return;
          }
        }
        if (farmerWars.Any(w => w.Mode == CountryWarMode.Battle))
        {
          if (mySoldierTypeInfo.Kind != SoldierKind.Battle)
          {
            await game.CharacterLogAsync($"兵種 {mySoldierTypeInfo.Name} は、通常戦争を布告している農民国家に対して使うことはできません");
            return;
          }
        }
      }

      // 連戦カウント
      var continuousCount = options.FirstOrDefault(o => o.Type == 32)?.NumberValue ?? 1;
      var continuousTurns = options.FirstOrDefault(o => o.Type == 33)?.NumberValue ?? 1;

      var isMyPenalty = myCountry.IsLargeCountryPenalty;
      var isTargetPenalty = targetCountryOptional.Data?.IsLargeCountryPenalty ?? false;
      if (isMyPenalty && isTargetPenalty)
      {
        isMyPenalty = isTargetPenalty = false;
      }

      var log = new BattleLog
      {
        TownId = targetTown.Id,
        AttackerCharacterId = character.Id,
        IsSameReligion = myCountry.Religion != ReligionType.Any && myCountry.Religion != ReligionType.None &&
          myCountry.Religion != targetCountryOptional.Data?.Religion && myCountry.Religion == targetTown.Religion,
        Cause = cause,
      };
      var logLines = new List<BattleLogLine>();
      uint mapLogId = 0;

      var mySoldierType = DefaultCharacterSoldierTypeParts.GetDataByDefault(character.SoldierType);
      var myRicePerSoldier = 1;
      var myFormation = FormationTypeInfoes.Get(character.FormationType).Data;
      if (myFormation == null)
      {
        myFormation = FormationTypeInfoes.Get(FormationType.Normal).Data;
      }
      var myFormationData = await repo.Character.GetFormationAsync(character.Id, character.FormationType);
      var attackerCache = character.ToLogCache((await repo.Character.GetCharacterAllIconsAsync(character.Id)).GetMainOrFirst().Data ?? new CharacterIcon(), myFormationData);
      CharacterSoldierTypeData targetSoldierType = null;
      var canContinuous = false;
      var myAttackCorrection = 0;
      var myDefenceCorrection = 0;
      var myAttackSoldierTypeCorrection = 0;
      var myDefenceSoldierTypeCorrection = 0;
      var myExperience = 50;
      var myFormationExperience = 0.0f;
      var myContribution = 20;
      var mySkills = await repo.Character.GetSkillsAsync(character.Id);
      var myItems = await repo.Character.GetItemsAsync(character.Id);
      var myRanking = await repo.Character.GetCharacterRankingAsync(character.Id);
      var myPolicies = (await repo.Country.GetPoliciesAsync(character.CountryId)).GetAvailableTypes();
      var targetAttackCorrection = 0;
      var targetDefenceCorrection = 0;
      var targetAttackSoldierTypeCorrection = 0;
      var targetDefenceSoldierTypeCorrection = 0;
      var targetExperience = 50;
      var targetFormationExperience = 0.0f;
      var targetContribution = 0;
      IReadOnlyList<CharacterSkill> targetSkills = Enumerable.Empty<CharacterSkill>().ToArray();
      IReadOnlyList<CharacterItem> targetItems = Enumerable.Empty<CharacterItem>().ToArray();
      character.Rice -= character.SoldierNumber * myRicePerSoldier;
      var aiLog = new AiBattleHistory
      {
        CharacterId = character.Id,
        CountryId = character.CountryId,
        IntGameDateTime = game.GameDateTime.ToInt(),
        TownCountryId = targetTown.CountryId,
        TownId = targetTown.Id,
        AttackerSoldiersMoney = mySoldierType.Money * character.SoldierNumber,
      };

      var myBattleResources = myItems
        .Select(i => new { Item = i, Info = i.GetInfo().Data, })
        .Where(i => i.Item.IsAvailable && i.Info != null && i.Info.Effects != null && i.Info.Effects.Any(e => e.Type == CharacterItemEffectType.SoldierCorrectionResource));

      mySoldierType
        .Append(myFormation.GetDataFromLevel(myFormationData.Level))
        .Append(mySkills.GetSoldierTypeData())
        .Append(myItems.GetSoldierTypeData())
        .Append(myBattleResources.SelectMany(r => r.Info.Effects.Where(e => e.Type == CharacterItemEffectType.SoldierCorrectionResource).Select(e => e.SoldierTypeData)));

      var myPosts = await repo.Country.GetCharacterPostsAsync(character.Id);
      var myPostOptional = myPosts.Where(p => p.Type.BattleOrder() > 0).OrderBy(p => p.Type.BattleOrder()).FirstOrDefault().ToOptional();
      if (myPostOptional.HasData)
      {
        var myPost = myPostOptional.Data;
        var postCorrections = mySoldierType.CalcPostCorrections(myPost.Type);
        myAttackCorrection += postCorrections.AttackCorrection;
        myDefenceCorrection += postCorrections.DefendCorrection;
      }

      if (log.IsSameReligion)
      {
        targetDefenceCorrection -= 15;
        if (system.RuleSet == GameRuleSet.Religion)
        {
          targetDefenceCorrection -= 35;
        }
      }

      if (myPolicies.Contains(CountryPolicyType.BloodyAngel))
      {
        if (myTown.Religion == myCountry.Religion)
        {
          myAttackCorrection += 30;
        }
      }

      Character targetCharacter;
      var targetRanking = new CharacterRanking();
      Formation targetFormationData;
      FormationTypeInfo targetFormation;
      bool isWall;
      var trendStrong = (short)Math.Max((int)((game.GameDateTime.ToInt() - Config.StartYear * 12 - Config.CountryBattleStopDuring) * 0.94f / 12), 20);
      var defenders = (await repo.Town.GetDefendersAsync(targetTown.Id))
        .Where(d => DefaultCharacterSoldierTypeParts.Get(d.Character.SoldierType).Data?.Kind == mySoldierTypeInfo.Kind);
      LogCharacterCache defenderCache = null;
      if (defenders.Any())
      {
        targetCharacter = defenders.First().Character;
        targetRanking = await repo.Character.GetCharacterRankingAsync(targetCharacter.Id);
        log.DefenderCharacterId = targetCharacter.Id;
        log.DefenderType = DefenderType.Character;
        aiLog.DefenderId = targetCharacter.Id;
        aiLog.TargetType = targetCharacter.SoldierNumber < 10 ? AiBattleTargetType.CharacterLowSoldiers : AiBattleTargetType.Character;

        if (targetCharacter.AiType == CharacterAiType.FlyingColumn)
        {
          mySoldierType.ContinuousProbability += 4000;
        }

        targetSoldierType = DefaultCharacterSoldierTypeParts.GetDataByDefault(targetCharacter.SoldierType);

        targetFormation = FormationTypeInfoes.Get(targetCharacter.FormationType).Data;
        if (targetFormation == null)
        {
          targetFormation = FormationTypeInfoes.Get(FormationType.Normal).Data;
        }
        targetFormationData = await repo.Character.GetFormationAsync(targetCharacter.Id, targetCharacter.FormationType);
        targetSoldierType.Append(targetFormation.GetDataFromLevel(targetFormationData.Level));
        defenderCache = targetCharacter.ToLogCache((await repo.Character.GetCharacterAllIconsAsync(targetCharacter.Id)).GetMainOrFirst().Data ?? new CharacterIcon(), targetFormationData);

        targetSkills = await repo.Character.GetSkillsAsync(targetCharacter.Id);
        targetSoldierType.Append(targetSkills.GetSoldierTypeData());

        targetItems = await repo.Character.GetItemsAsync(targetCharacter.Id);
        var targetBattleResources = targetItems
          .Select(i => new { Item = i, Info = i.GetInfo().Data, })
          .Where(i => i.Item.IsAvailable && i.Info != null && i.Info.Effects != null && i.Info.Effects.Any(e => e.Type == CharacterItemEffectType.SoldierCorrectionResource));
        targetSoldierType.Append(targetItems.GetSoldierTypeData())
          .Append(targetBattleResources.SelectMany(r => r.Info.Effects.Where(e => e.Type == CharacterItemEffectType.SoldierCorrectionResource).Select(e => e.SoldierTypeData)));

        isWall = false;
        await game.CharacterLogByIdAsync(targetCharacter.Id, $"守備をしている <town>{targetTown.Name}</town> に <character>{character.Name}</character> が攻め込み、戦闘になりました");
      }
      else if (mySoldierTypeInfo.Kind == SoldierKind.Religion)
      {
        // 都市の宗教と戦う
        targetCharacter = new Character();
        targetCharacter.CountryId = targetTown.CountryId;
        targetCharacter.Name = targetTown.Name + "宗教";

        targetCharacter.SoldierNumber = targetTown.Confucianism + targetTown.Taoism + targetTown.Buddhism;
        var religion = myCountry.Religion;
        if (targetTown.Religion == religion)
        {
          targetCharacter.SoldierNumber = 0;
        }
        else if (religion == ReligionType.Confucianism)
        {
          targetCharacter.SoldierNumber -= targetTown.Confucianism;
        }
        else if (religion == ReligionType.Taoism)
        {
          targetCharacter.SoldierNumber -= targetTown.Taoism;
        }
        else if (religion == ReligionType.Buddhism)
        {
          targetCharacter.SoldierNumber -= targetTown.Buddhism;
        }

        targetCharacter.SoldierNumber = Math.Max(0, targetCharacter.SoldierNumber);

        log.DefenderType = DefenderType.Wall;
        aiLog.TargetType = AiBattleTargetType.Wall;

        targetSoldierType = DefaultCharacterSoldierTypeParts.GetDataByDefault(SoldierType.Common);
        targetFormation = FormationTypeInfoes.Get(FormationType.Normal).Data;
        targetFormationData = new Formation
        {
          Type = FormationType.Normal,
          Level = 1,
        };

        defenderCache = targetCharacter.ToLogCache(new CharacterIcon(), targetFormationData);
        isWall = true;
      }
      else
      {
        // 通常の城壁と戦う
        targetCharacter = new Character();
        targetCharacter.CountryId = targetTown.CountryId;
        targetCharacter.Name = targetTown.Name + "城壁";
        targetCharacter.SoldierNumber = targetTown.Wall;
        log.DefenderType = DefenderType.Wall;
        aiLog.TargetType = AiBattleTargetType.Wall;

        var policies = (await repo.Country.GetPoliciesAsync(targetTown.CountryId)).GetAvailableTypes();

        var evaluationTechnology = targetTown.Technology;
        if (log.IsSameReligion)
        {
          evaluationTechnology -= RandomService.Next(28, 160);
          if (system.RuleSet == GameRuleSet.Religion)
          {
            evaluationTechnology -= RandomService.Next(32, 72);
          }
          if (RandomService.Next(0, 32) == 0)
          {
            evaluationTechnology -= RandomService.Next(60, 500);
          }
        }
        targetCharacter.SoldierType = evaluationTechnology >= 1700 ? SoldierType.Guard_Step6 :
          evaluationTechnology >= 1200 ? SoldierType.Guard_Step5 :
          evaluationTechnology >= 1000 ? SoldierType.Guard_Step4 :
          evaluationTechnology >= 600 ? SoldierType.Guard_Step3 :
          evaluationTechnology >= 300 ? SoldierType.Guard_Step2 :
          SoldierType.Guard_Step1;
        targetCharacter.Strong = trendStrong;
        targetCharacter.Proficiency = 100;

        targetSoldierType = DefaultCharacterSoldierTypeParts.GetDataByDefault(targetCharacter.SoldierType);
        targetFormation = FormationTypeInfoes.Get(FormationType.Normal).Data;
        targetFormationData = new Formation
        {
          Type = FormationType.Normal,
          Level = 1,
        };

        defenderCache = targetCharacter.ToLogCache(new CharacterIcon(), targetFormationData);
        isWall = true;

        if (myPolicies.Contains(CountryPolicyType.Shosha))
        {
          myAttackCorrection += Math.Max((int)(60 * ((100 - mySoldierType.TypeWeapon) / 100.0f)), 0);
        }
      }
      var targetNameWithTag = $"<{(isWall ? "wall" : "character")}>{targetCharacter.Name}</{(isWall ? "wall" : "character")}>";

      await game.CharacterLogAsync("<town>" + targetTown.Name + "</town> に攻め込みました");

      var (ka, kd) = mySoldierType.CalcCorrections(character, mySkills, targetSoldierType);
      myAttackSoldierTypeCorrection = ka;
      myDefenceSoldierTypeCorrection = kd;

      var (ea, ed) = targetSoldierType.CalcCorrections(targetCharacter, targetSkills, mySoldierType);
      targetAttackSoldierTypeCorrection = ea;
      targetDefenceSoldierTypeCorrection = ed;

      var myPower = mySoldierType.CalcPower(character);
      var targetPower = targetSoldierType.CalcPower(targetCharacter);

      var myCampValue = Math.Min(1.6f, Math.Max(1, game.GameDateTime.Year * 0.024f * myTown.TownBuildingValue / Config.TownBuildingMax));
      var targetCampValue = Math.Min(1.6f, Math.Max(1, game.GameDateTime.Year * 0.024f * targetTown.TownBuildingValue / Config.TownBuildingMax));
      var myProficiency = myTown.TownBuilding == TownBuilding.Camp ? character.Proficiency * myCampValue : character.Proficiency;
      var targetProficiency = (targetTown.TownBuilding == TownBuilding.Camp && !isWall) ? targetCharacter.Proficiency * targetCampValue : targetCharacter.Proficiency;

      if (log.IsSameReligion && system.RuleSet == GameRuleSet.Religion)
      {
        targetProficiency = 0;
        myProficiency = 110;
      }
      
      var myAttack = Math.Max((int)((myPower + myAttackCorrection + myAttackSoldierTypeCorrection - targetDefenceCorrection - targetDefenceSoldierTypeCorrection - targetProficiency / 2.5f) / 8), 0);
      var targetAttack = Math.Max((int)((targetPower + targetAttackCorrection + targetAttackSoldierTypeCorrection - myDefenceCorrection - myDefenceSoldierTypeCorrection - myProficiency / 2.5f) / 8), 0);

      log.AttackerAttackPower = myAttack;
      log.DefenderAttackPower = targetAttack;

      if (RandomService.Next(5) <= 3)
      {
        if (isMyPenalty)
        {
          myAttack = (int)Math.Max(0, myAttack * 0.7f);
        }
        if (isTargetPenalty)
        {
          targetAttack = (int)Math.Max(0, targetAttack * 0.7f);
        }
      }

      var currentBattleTurns = 0;
      for (var i = continuousTurns; i <= 50 && targetCharacter.SoldierNumber > 0 && character.SoldierNumber > 0; i++)
      {
        continuousTurns = i;
        currentBattleTurns++;

        var targetDamage = Math.Max(RandomService.Next(myAttack + 1), 1);
        var myDamage = Math.Max(RandomService.Next(targetAttack + 1), 1);
        var myCommand = BattleTurnCommand.None;
        var targetCommand = BattleTurnCommand.None;

        // 突撃など
        if (myCommand == BattleTurnCommand.None && targetCommand == BattleTurnCommand.None && !isWall)
        {
          if (mySoldierType.IsRush())
          {
            targetDamage = Math.Min(Math.Max((int)(targetDamage + mySoldierType.CalcRushAttack(targetSoldierType, targetCharacter)), 14), targetCharacter.SoldierNumber);
            targetDamage = Math.Max(targetDamage, Math.Max(myAttack + 1, 1) / 2);
            myCommand = BattleTurnCommand.Rush;
          }
          else if (mySoldierType.IsDisorder())
          {
            myDamage = 0;
            myCommand = BattleTurnCommand.Disorder;
          }
          else if (mySoldierType.IsFriendlyFire())
          {
            targetDamage += myDamage;
            myCommand = BattleTurnCommand.FriendlyFire;
          }
          else if (targetSoldierType.IsRush())
          {
            myDamage = Math.Min(Math.Max((int)(myDamage + targetSoldierType.CalcRushAttack(mySoldierType, character)), 8), character.SoldierNumber);
            myDamage = Math.Max(myDamage, Math.Max(targetAttack + 1, 1) / 2);
            targetCommand = BattleTurnCommand.Rush;
          }
          else if (targetSoldierType.IsDisorder())
          {
            targetDamage = 0;
            targetCommand = BattleTurnCommand.Disorder;
          }
          else  if (targetSoldierType.IsFriendlyFire())
          {
            myDamage += targetDamage;
            targetCommand = BattleTurnCommand.FriendlyFire;
          }
        }
        if (myCommand != BattleTurnCommand.None)
        {
          myRanking.BattleSchemeCount++;
        }
        if (!isWall && targetCommand != BattleTurnCommand.None)
        {
          targetRanking.BattleSchemeCount++;
        }

        // 兵士数がマイナスにならないようにする
        targetDamage = Math.Min(targetDamage, targetCharacter.SoldierNumber);
        myDamage = Math.Min(myDamage, character.SoldierNumber);

        var myFormationExperienceTmp = 0.0f;
        var targetFormationExperienceTmp = 0.0f;

        character.SoldierNumber -= myDamage;
        if (!isWall)
        {
          myFormationExperienceTmp = targetDamage * 0.42f * Math.Max(targetSoldierType.FakeMoney / 24.0f, 1.0f);
        }
        else
        {
          myFormationExperienceTmp = Math.Min((targetDamage * 0.17f), 40.0f);
        }
        targetCharacter.SoldierNumber -= targetDamage;
        targetFormationExperienceTmp = myDamage * 0.39f * Math.Max(mySoldierType.FakeMoney / 24.0f, 1.0f);

        myExperience += (int)(targetDamage * 0.32f);
        targetExperience += (int)(myDamage * 0.29f);

        myFormationExperience += myFormationExperienceTmp * 0.6f + targetFormationExperienceTmp * 0.4f;
        targetFormationExperience += targetFormationExperienceTmp * 0.6f + myFormationExperienceTmp * 0.4f;

        await game.CharacterLogAsync("  戦闘 ターン<num>" + i + "</num> <character>" + character.Name + "</character> <num>" + character.SoldierNumber + "</num> (↓<num>" + myDamage + "</num>) | " + targetNameWithTag + " <num>" + targetCharacter.SoldierNumber + "</num> (↓<num>" + targetDamage + "</num>)");
        myRanking.BattleKilledCount += targetDamage;
        myRanking.BattleBeingKilledCount += myDamage;
        if (!isWall)
        {
          await game.CharacterLogByIdAsync(targetCharacter.Id, "  戦闘 ターン<num>" + i + "</num> <character>" + character.Name + "</character> <num>" + character.SoldierNumber + "</num> (↓<num>" + myDamage + "</num>) | <character>" + targetCharacter.Name + "</character> <num>" + targetCharacter.SoldierNumber + "</num> (↓<num>" + targetDamage + "</num>)");
          targetRanking.BattleKilledCount += myDamage;
          targetRanking.BattleBeingKilledCount += targetDamage;
        }
        else
        {
          myRanking.BattleBrokeWallSize += targetDamage;
        }

        logLines.Add(new BattleLogLine
        {
          Turn = (short)i,
          AttackerNumber = (short)character.SoldierNumber,
          AttackerDamage = (short)myDamage,
          DefenderNumber = (short)targetCharacter.SoldierNumber,
          DefenderDamage = (short)targetDamage,
          AttackerCommand = myCommand,
          DefenderCommand = targetCommand,
        });
      }
      continuousTurns++;

      if (isWall)
      {
        // 支配後の配信に影響するので、後の武将の更新処理とはまとめずここで
        if (mySoldierTypeInfo.Kind == SoldierKind.Religion)
        {
          var religion = myCountry.Religion;
          var targetFirstNumber = targetTown.Confucianism + targetTown.Taoism + targetTown.Buddhism;
          var targetDamage = targetFirstNumber - targetCharacter.SoldierNumber;
          if (religion == ReligionType.Confucianism)
          {
            // targetFirstNumber -= targetTown.Confucianism;
            var rate = Math.Max(0.0f, 1.0f - (float)targetDamage / targetFirstNumber);
            targetTown.Taoism = (int)(targetTown.Taoism * rate);
            targetTown.Buddhism = (int)(targetTown.Buddhism * rate);
          }
          if (religion == ReligionType.Taoism)
          {
            // targetFirstNumber -= targetTown.Taoism;
            var rate = Math.Max(0.0f, 1.0f - (float)targetDamage / targetFirstNumber);
            targetTown.Confucianism = (int)(targetTown.Confucianism * rate);
            targetTown.Buddhism = (int)(targetTown.Buddhism * rate);
          }
          if (religion == ReligionType.Buddhism)
          {
            // targetFirstNumber -= targetTown.Buddhism;
            var rate = Math.Max(0.0f, 1.0f - (float)targetDamage / targetFirstNumber);
            targetTown.Confucianism = (int)(targetTown.Confucianism * rate);
            targetTown.Taoism = (int)(targetTown.Taoism * rate);
          }
        }
        else
        {
          targetTown.Wall = targetCharacter.SoldierNumber;
        }
      }

      IEnumerable<TownDefender> removedDefenders = null;
      TownDefender newDefender = null;
      {
        var targetCountry = targetCountryOptional.Data ?? new Country
        {
          Name = "無所属",
        };
        var prefix = $"[<town>{targetTown.Name}</town>]{(continuousCount > 1 ? "(連戦)" : "")} <country>{myCountry.Name}</country> の <character>{character.Name}</character> は <country>{targetCountry.Name}</country> の {targetNameWithTag}";
        if (character.SoldierNumber <= 0 && targetCharacter.SoldierNumber <= 0)
        {
          removedDefenders = await repo.Town.RemoveDefenderAsync(targetCharacter.Id);
          mapLogId = await game.MapLogAndSaveAsync(EventType.BattleDrawLose, prefix + " と引き分けました", false);
          await game.CharacterLogAsync($"{targetNameWithTag} と引き分けました");
          if (!isWall)
          {
            if (character.AiType == CharacterAiType.TerroristRyofu)
            {
              myExperience += 500;
            }
            if (targetCharacter.AiType == CharacterAiType.TerroristRyofu)
            {
              targetExperience += 10_000;
            }
            await game.CharacterLogByIdAsync(targetCharacter.Id, $"<character>{character.Name}</character> と引き分けました。<town>{targetTown.Name}</town> の守備から外れました");
          }
        }
        else if (character.SoldierNumber <= 0 && targetCharacter.SoldierNumber > 0)
        {
          mapLogId = await game.MapLogAndSaveAsync(EventType.BattleLose, prefix + " に敗北しました", false);
          await game.CharacterLogAsync($"{targetNameWithTag} に敗北しました");
          if (!isWall)
          {
            myRanking.BattleLostCount++;
            targetRanking.BattleWonCount++;
            if (character.AiType == CharacterAiType.TerroristRyofu)
            {
              myExperience += 500;
            }
            if (targetCharacter.AiType == CharacterAiType.TerroristRyofu)
            {
              targetExperience += 10_000;
            }
            if (character.AiType == CharacterAiType.FarmerBattler ||
              character.AiType == CharacterAiType.FarmerCivilOfficial)
            {
              myExperience += (int)((character.Strong * 1000 + character.StrongEx) * 1.02f) / 1000;
            }
            await game.CharacterLogByIdAsync(targetCharacter.Id, "<character>" + character.Name + "</character> を撃退しました");
          }
        }
        else if (character.SoldierNumber > 0 && targetCharacter.SoldierNumber > 0)
        {
          mapLogId = await game.MapLogAndSaveAsync(EventType.BattleDraw, prefix + " と引き分けました", false);
          await game.CharacterLogAsync($"{targetNameWithTag} と引き分けました");
          if (!isWall)
          {
            if (character.AiType == CharacterAiType.TerroristRyofu)
            {
              myExperience += 500;
            }
            if (targetCharacter.AiType == CharacterAiType.TerroristRyofu)
            {
              targetExperience += 500;
            }
            await game.CharacterLogByIdAsync(targetCharacter.Id, "<character>" + character.Name + "</character> と引き分けました");
          }
        }
        else
        {
          if (character.AiType == CharacterAiType.TerroristRyofu)
          {
            myExperience += 10_000;
          }
          if (targetCharacter.AiType == CharacterAiType.FarmerBattler ||
            targetCharacter.AiType == CharacterAiType.FarmerCivilOfficial)
          {
            targetExperience += (int)((targetCharacter.Strong * 1000 + targetCharacter.StrongEx) * 1.02f) / 1000;
          }

          if (!isWall)
          {
            myRanking.BattleWonCount++;
            targetRanking.BattleLostCount++;

            // 連戦
            if (continuousTurns < 50)
            {
              if (logLines.Count() <= 1)
              {
                canContinuous = mySoldierType.CanContinuousOnSingleTurn();
              }
              if (!canContinuous)
              {
                canContinuous = mySoldierType.CanContinuous();
              }
            }

            if (targetCharacter.AiType == CharacterAiType.TerroristRyofu)
            {
              targetExperience += 500;
            }
            removedDefenders = await repo.Town.RemoveDefenderAsync(targetCharacter.Id);
            mapLogId = await game.MapLogAndSaveAsync(EventType.BattleWin, prefix + " を倒しました", false);
            await game.CharacterLogAsync($"{targetNameWithTag} に勝利しました");
            await game.CharacterLogByIdAsync(targetCharacter.Id, $"<character>{character.Name}</character> に敗北しました。<town>{targetTown.Name}</town> の守備から外れました");
          }
          else
          {
            targetTown.CountryId = character.CountryId;
            targetTown.Agriculture = (int)(targetTown.Agriculture * 0.8f);
            targetTown.Commercial = (int)(targetTown.Commercial * 0.8f);
            targetTown.Technology = (int)(targetTown.Technology * 0.8f);
            targetTown.TownBuildingValue = (int)(targetTown.TownBuildingValue * 0.8f);
            targetTown.People = (int)(targetTown.People * 0.8f);
            targetTown.Security = (short)(targetTown.Security * 0.8f);

            var oldReligion = targetTown.Religion;
            if (myCountry.Religion != ReligionType.Confucianism)
            {
              targetTown.Confucianism = (int)(targetTown.Confucianism * 0.95f);
            }
            if (myCountry.Religion != ReligionType.Taoism)
            {
              targetTown.Taoism = (int)(targetTown.Taoism * 0.95f);
            }
            if (myCountry.Religion != ReligionType.Buddhism)
            {
              targetTown.Buddhism = (int)(targetTown.Buddhism * 0.95f);
            }
            if (targetTown.Religion != oldReligion)
            {
              if (targetTown.Religion == ReligionType.Any)
              {
                await game.MapLogAsync(EventType.StopReligion, $"<town>{targetTown.Name}</town> は宗教の信仰をやめました", false);
              }
              else
              {
                var newReligionName = targetTown.Religion.GetString();
                await game.MapLogAsync(EventType.ChangeReligion, $"<town>{targetTown.Name}</town> は {oldReligion.GetString()} から {newReligionName} に改宗しました", false);
                myRanking.MissionaryChangeReligionCount++;
              }
              await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(targetTown), repo);
            }

            myExperience += 50;
            myContribution += 50;
            character.TownId = targetTown.Id;
            myRanking.BattleDominateCount++;
            newDefender = await repo.Town.SetDefenderAsync(character.Id, targetTown.Id);
            mapLogId = await game.MapLogAndSaveAsync(EventType.TakeAway, "<country>" + myCountry.Name + "</country> の <character>" + character.Name + "</character> は <country>" + targetCountry.Name + "</country> の <town>" + targetTown.Name + "</town> を支配しました", true);
            await game.CharacterLogAsync("<town>" + targetTown.Name + "</town> を支配しました");

            // 支配したときのみ匿名ストリーミング
            await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(targetTown)));

            // 宗教戦争などで守備が残っていることがある
            if (defenders.Any())
            {
              removedDefenders = await repo.Town.RemoveTownDefendersAsync(targetTown.Id);
            }

            if (targetCountryOptional.HasData)
            {
              var targetCountryTownCount = await repo.Town.CountByCountryIdAsync(targetCountry.Id);
              if (targetCountryTownCount <= 0)
              {
                if (targetCountry.AiType != CountryAiType.Farmers)
                {
                  myCountry.PolicyPoint += 3500;
                }
                if (targetCountry.AiType == CountryAiType.Terrorists || targetCountry.AiType == CountryAiType.TerroristsEnemy)
                {
                  await CountryService.SetPolicyAndSaveAsync(repo, myCountry, CountryPolicyType.GetTerrorists, isCheckSubjects: false);
                }

                await CountryService.OverThrowAsync(repo, targetCountry, myCountry);

                await StatusStreaming.Default.SendCountryAsync(ApiData.From(myCountry), myCountry.Id);
              }
              else
              {
                // 都市を取られた国に、都市の最新情報を配信
                var scoutedTown = ScoutedTown.From(targetTown);
                scoutedTown.ScoutedDateTime = game.GameDateTime;
                scoutedTown.ScoutedCountryId = targetCountry.Id;
                scoutedTown.ScoutMethod = ScoutMethod.InBattle;

                await repo.ScoutedTown.AddScoutAsync(scoutedTown);
                await repo.SaveChangesAsync();

                var savedScoutedTown = (await repo.ScoutedTown.GetByTownIdAsync(targetTown.Id, targetCountry.Id)).Data;
                if (savedScoutedTown != null)
                {
                  await StatusStreaming.Default.SendCountryAsync(ApiData.From(savedScoutedTown), targetCountry.Id);
                }
              }
            }
          }

          // アイテム
          if (RandomService.Next(0, 128) == 0)
          {
            var info = await ItemService.PickTownHiddenItemAsync(repo, character.TownId, character);
            if (info.HasData)
            {
              var town = await repo.Town.GetByIdAsync(character.TownId);
              if (town.HasData)
              {
                await game.CharacterLogAsync($"<town>{town.Data.Name}</town> に隠されたアイテム {info.Data.Name} を手に入れました");
              }
            }
          }
        }
      }

      // 戦闘ログを保存
      log.MapLogId = mapLogId;
      await repo.BattleLog.AddLogWithSaveAsync(log, logLines, attackerCache, defenderCache);
      await repo.MapLog.SetBattleLogIdAsync(mapLogId, log.Id);
      aiLog.RestDefenderCount = defenders.Any() ? (defenders.First().Character.SoldierNumber == 0 ? defenders.Count() - 1 : defenders.Count()) : 0;
      if (targetCountryOptional.HasData && targetCountryOptional.Data.AiType == CountryAiType.Managed)
      {
        var storategy = await repo.AiCountry.GetStorategyByCountryIdAsync(targetCountryOptional.Data.Id);
        if (storategy.HasData)
        {
          aiLog.TownType = storategy.Data.MainTownId == targetTownId && storategy.Data.BorderTownId == targetTownId ? AiBattleTownType.MainAndBorderTown :
            storategy.Data.MainTownId == targetTownId ? (AiBattleTownType.MainTown | AiBattleTownType.BorderTown) :
            storategy.Data.BorderTownId == targetTownId ? AiBattleTownType.BorderTown : AiBattleTownType.Others;
        }
        else
        {
          aiLog.TownType = AiBattleTownType.Others;
        }
        await repo.AiActionHistory.AddAsync(aiLog);
      }
      else if (character.AiType.IsManaged())
      {
        aiLog.TownType = AiBattleTownType.EnemyTown;
        await repo.AiActionHistory.AddAsync(aiLog);
      }

      // 貢献、経験値の設定
      foreach (var myr in myBattleResources)
      {
        await SpendResourceAsync(myr.Item, myr.Info, myr.Item.Resource - currentBattleTurns);
      }
      myFormationExperience = Math.Max(1, (int)myFormationExperience);
      myContribution += myExperience;
      character.Contribution += (int)(myContribution);
      character.SkillPoint++;
      await game.CharacterLogAsync($"戦闘終了 <battle-log>{log.Id}</battle-log> 貢献: <num>{myContribution}</num>" + this.AddExperience(myExperience, character, mySoldierType) + $" 陣形ex: <num>{(int)myFormationExperience}</num>");
      myFormationData.Experience += (int)myFormationExperience;
      if (myFormation.CheckLevelUp(myFormationData))
      {
        await game.CharacterLogAsync($"陣形 {myFormation.Name} のレベルが <num>{myFormationData.Level}</num> に上昇しました");
      }
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(myFormationData), character.Id);
      if (!isWall)
      {
        targetContribution += targetExperience;
        targetCharacter.Contribution += (int)(targetContribution);
        targetCharacter.SkillPoint++;
        await game.CharacterLogByIdAsync(targetCharacter.Id, $"戦闘終了 <battle-log>{log.Id}</battle-log> 貢献: <num>{targetContribution}</num>" + this.AddExperience(targetExperience, targetCharacter, targetSoldierType) + $" 陣形ex: <num>{(int)targetFormationExperience}</num>");

        await CharacterService.StreamCharacterAsync(repo, targetCharacter);
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(new ApiSignal
        {
          Type = SignalType.DefenderBattled,
          Data = new { townName = targetTown.Name, targetName = character.Name, isWin = targetCharacter.SoldierNumber > 0, },
        }), targetCharacter.Id);

        var targetBattleResources = targetItems
          .Select(i => new { Item = i, Info = i.GetInfo().Data, })
          .Where(i => i.Item.IsAvailable && i.Info != null && i.Info.Effects != null && i.Info.Effects.Any(e => e.Type == CharacterItemEffectType.SoldierCorrectionResource));
        foreach (var myr in targetBattleResources)
        {
          await SpendResourceAsync(myr.Item, myr.Info, myr.Item.Resource - currentBattleTurns, targetCharacter);
        }

        targetFormationData.Experience += (int)targetFormationExperience;
        if (targetFormation.CheckLevelUp(targetFormationData))
        {
          await game.CharacterLogByIdAsync(targetCharacter.Id, $"陣形 {targetFormation.Name} のレベルが <num>{targetFormationData.Level}</num> に上昇しました");
        }
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(targetFormationData), targetCharacter.Id);
      }

      // 更新された都市データを通知
      var townCharas = await repo.Town.GetCharactersAsync(targetTown.Id);
      await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(targetTown), repo);
      if (removedDefenders != null && removedDefenders.Any() && targetCountryOptional.HasData)
      {
        foreach (var d in removedDefenders)
        {
          d.Status = TownDefenderStatus.Losed;
          await StatusStreaming.Default.SendCountryAsync(ApiData.From(d), targetCountryOptional.Data.Id);
          await StatusStreaming.Default.SendCharacterAsync(ApiData.From(d), townCharas.Where(tc => tc.CountryId != targetCountryOptional.Data.Id).Select(tc => tc.Id));
        }
      }
      if (newDefender != null)
      {
        newDefender.Status = TownDefenderStatus.Available;
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(newDefender), character.CountryId);
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(newDefender), townCharas.Where(tc => tc.CountryId != character.CountryId).Select(tc => tc.Id));
      }

      // 連戦
      if (canContinuous)
      {
        myRanking.BattleContinuousCount++;
        await repo.SaveChangesAsync();
        continuousCount++;
        await this.ExecuteAsync(repo, character, options.Where(p => p.Type != 32 && p.Type != 33).Append(new CharacterCommandParameter
        {
          Type = 32,
          NumberValue = continuousCount,
        })
        .Append(new CharacterCommandParameter
        {
          Type = 33,
          NumberValue = continuousTurns,
        }), game);
      }


      async Task SpendResourceAsync(CharacterItem item, CharacterItemInfo info, int newResource, Character chara = null)
      {
        if (chara == null)
        {
          chara = character;
        }

        item.Resource = newResource;
        if (item.Resource <= 0)
        {
          await ItemService.SpendCharacterAsync(repo, item, chara);
          await game.CharacterLogAsync($"アイテム {info.Name} はすべての資源を使い果たし、消滅しました");
        }
        else
        {
          await StatusStreaming.Default.SendCharacterAsync(ApiData.From(item), item.CharacterId);
        }
      }
    }

    private string AddExperience(int ex, Character chara, CharacterSoldierTypeData soldierType)
    {
      var strong = (short)(ex / 10.0f * soldierType.StrongEx);
      var intellect = (short)(ex / 10.0f * soldierType.IntellectEx);
      var leadership = (short)(ex / 10.0f * soldierType.LeadershipEx);
      var popularity = (short)(ex / 10.0f * soldierType.PopularityEx);
      chara.AddStrongEx(strong);
      chara.AddIntellectEx(intellect);
      chara.AddLeadershipEx(leadership);
      chara.AddPopularityEx(popularity);

      var str = string.Empty;
      if (strong > 0)
      {
        str += $" 武力ex: <num>{strong}</num>";
      }
      if (intellect > 0)
      {
        str += $" 知力ex: <num>{intellect}</num>";
      }
      if (leadership > 0)
      {
        str += $" 統率ex: <num>{leadership}</num>";
      }
      if (popularity > 0)
      {
        str += $" 人望ex: <num>{popularity}</num>";
      }
      return str;
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var townId = (uint)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var town = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.InternalDataNotFoundError, new { command = "move", townId, });

      // 連戦カウンター
      if (options.Any(p => p.Type == 32 || p.Type == 33))
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
