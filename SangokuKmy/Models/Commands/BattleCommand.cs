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
      if (character.SoldierNumber <= 0)
      {
        await game.CharacterLogAsync("兵士がいません");
        return;
      }

      var targetTownId = options.FirstOrDefault(p => p.Type == 1).NumberValue;
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
      if (targetTown.CountryId == character.CountryId)
      {
        await game.CharacterLogAsync($"自国の <town>{targetTown.Name}</town> へ侵攻しようとしました");
        return;
      }
      if (!targetTown.IsNextToTown(myTown))
      {
        await game.CharacterLogAsync($"<town>{targetTown.Name}</town> に侵攻しようとしましたが、<town>{myTown.Name}</town> とは隣接していません");
        return;
      }

      var targetCountryOptional = await repo.Country.GetByIdAsync(targetTown.CountryId);
      if (targetCountryOptional.HasData)
      {
        if (targetCountryOptional.Data.IntEstablished + Config.CountryBattleStopDuring > game.GameDateTime.ToInt())
        {
          await game.CharacterLogAsync("<town>" + targetTown.Name + "</town> の所持国 <country>" + targetCountryOptional.Data.Name + "</country> は戦闘解除されていません");
          return;
        }

        if (myCountry.AiType != CountryAiType.Thiefs && targetCountryOptional.Data.AiType != CountryAiType.Thiefs)
        {
          var warOptional = await repo.CountryDiplomacies.GetCountryWarAsync(character.CountryId, targetTown.CountryId);
          var targetTownWarOptional = await repo.CountryDiplomacies.GetTownWarAsync(targetTown.CountryId, character.CountryId, targetTown.Id);

          var isTownWar = false;
          if (targetTownWarOptional.HasData)
          {
            var townWar = targetTownWarOptional.Data;
            if (townWar.Status == TownWarStatus.Available && townWar.RequestedCountryId == character.CountryId)
            {
              isTownWar = true;
            }
          }

          if (!isTownWar)
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

      // 連戦カウント
      var continuousCount = options.FirstOrDefault(o => o.Type == 32)?.NumberValue ?? 1;
      var continuousTurns = options.FirstOrDefault(o => o.Type == 33)?.NumberValue ?? 1;

      var log = new BattleLog
      {
        TownId = targetTown.Id,
        AttackerCharacterId = character.Id,
      };
      var logLines = new List<BattleLogLine>();
      uint mapLogId = 0;

      CharacterSoldierTypeData mySoldierType;
      int myRicePerSoldier;
      if (character.SoldierType != SoldierType.Custom)
      {
        mySoldierType = DefaultCharacterSoldierTypeParts.GetDataByDefault(character.SoldierType);
        myRicePerSoldier = 1;
      }
      else
      {
        var type = await repo.CharacterSoldierType.GetByIdAsync(character.CharacterSoldierTypeId);
        if (type.HasData)
        {
          mySoldierType = type.Data.ToParts().ToData();
          myRicePerSoldier = 1 + type.Data.RicePerTurn;
        }
        else
        {
          await game.CharacterLogAsync($"カスタム兵種ID: {character.CharacterSoldierTypeId} は存在しません。<emerge>管理者にお問い合わせください</emerge>");
          return;
        }
      }
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
      var targetAttackCorrection = 0;
      var targetDefenceCorrection = 0;
      var targetAttackSoldierTypeCorrection = 0;
      var targetDefenceSoldierTypeCorrection = 0;
      var targetExperience = 50;
      var targetFormationExperience = 0.0f;
      var targetContribution = 0;
      IReadOnlyList<CharacterSkill> targetSkills = Enumerable.Empty<CharacterSkill>().ToArray();
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

      mySoldierType
        .Append(myFormation.GetDataFromLevel(myFormationData.Level))
        .Append(mySkills.GetSoldierTypeData());

      var myPostOptional = (await repo.Country.GetPostsAsync(character.CountryId)).FirstOrDefault(cp => cp.CharacterId == character.Id).ToOptional();
      if (myPostOptional.HasData)
      {
        var myPost = myPostOptional.Data;
        var postCorrections = mySoldierType.CalcPostCorrections(myPost.Type);
        myAttackCorrection += postCorrections.AttackCorrection;
        myDefenceCorrection += postCorrections.DefendCorrection;
      }

      Character targetCharacter;
      Formation targetFormationData;
      FormationTypeInfo targetFormation;
      bool isWall;
      var trendStrong = (short)Math.Max((int)((game.GameDateTime.ToInt() - Config.StartYear * 12 - Config.CountryBattleStopDuring) * 0.94f / 12), 20);
      var defenders = await repo.Town.GetDefendersAsync(targetTown.Id);
      LogCharacterCache defenderCache = null;
      if (defenders.Any())
      {
        targetCharacter = defenders.First().Character;
        log.DefenderCharacterId = targetCharacter.Id;
        log.DefenderType = DefenderType.Character;
        aiLog.DefenderId = targetCharacter.Id;
        aiLog.TargetType = targetCharacter.SoldierNumber < 10 ? AiBattleTargetType.CharacterLowSoldiers : AiBattleTargetType.Character;

        if (targetCharacter.SoldierType != SoldierType.Custom)
        {
          targetSoldierType = DefaultCharacterSoldierTypeParts.GetDataByDefault(targetCharacter.SoldierType);
        }
        else
        {
          var type = await repo.CharacterSoldierType.GetByIdAsync(targetCharacter.CharacterSoldierTypeId);
          if (type.HasData)
          {
            targetSoldierType = type.Data.ToParts().ToData();
          }
          else
          {
            targetSoldierType = DefaultCharacterSoldierTypeParts.GetDataByDefault(SoldierType.Common);
          }
        }

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

        isWall = false;
        await game.CharacterLogByIdAsync(targetCharacter.Id, $"守備をしている <town>{targetTown.Name}</town> に <character>{character.Name}</character> が攻め込み、戦闘になりました");
      }
      else
      {
        targetCharacter = new Character();
        targetCharacter.CountryId = targetTown.CountryId;
        targetCharacter.Name = targetTown.Name + "城壁";
        targetCharacter.SoldierNumber = targetTown.Wall;
        log.DefenderType = DefenderType.Wall;
        aiLog.TargetType = AiBattleTargetType.Wall;

        var policies = (await repo.Country.GetPoliciesAsync(targetTown.CountryId)).GetAvailableTypes();

        targetCharacter.SoldierType = targetTown.Technology >= 999 ? SoldierType.Guard_Step4 :
          targetTown.Technology >= 600 ? SoldierType.Guard_Step3 :
          targetTown.Technology >= 300 ? SoldierType.Guard_Step2 :
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

        var myPolicies = (await repo.Country.GetPoliciesAsync(character.CountryId)).GetAvailableTypes();
        if (myPolicies.Contains(CountryPolicyType.Shosha))
        {
          myAttackCorrection += Math.Max((int)(60 * ((100 - mySoldierType.TypeAntiWall) / 100.0f)), 0);
        }
      }

      await game.CharacterLogAsync("<town>" + targetTown.Name + "</town> に攻め込みました");

      var (ka, kd) = mySoldierType.CalcCorrections(character, mySkills, targetSoldierType);
      myAttackSoldierTypeCorrection = ka;
      myDefenceSoldierTypeCorrection = kd;

      var (ea, ed) = targetSoldierType.CalcCorrections(targetCharacter, targetSkills, mySoldierType);
      targetAttackSoldierTypeCorrection = ea;
      targetDefenceSoldierTypeCorrection = ed;

      var myPower = mySoldierType.CalcPower(character);
      var targetPower = targetSoldierType.CalcPower(targetCharacter);

      var myAttack = Math.Max((int)((myPower + myAttackCorrection + myAttackSoldierTypeCorrection - targetDefenceCorrection - targetDefenceSoldierTypeCorrection - targetCharacter.Proficiency / 2.5f) / 8), 0);
      var targetAttack = Math.Max((int)((targetPower + targetAttackCorrection + targetAttackSoldierTypeCorrection - myDefenceCorrection - myDefenceSoldierTypeCorrection - character.Proficiency / 2.5f) / 8), 0);

      for (var i = continuousTurns; i <= 50 && targetCharacter.SoldierNumber > 0 && character.SoldierNumber > 0; i++)
      {
        continuousTurns = i;

        var targetDamage = Math.Min(Math.Max(RandomService.Next(myAttack + 1), 1), targetCharacter.SoldierNumber);
        var myDamage = Math.Min(Math.Max(RandomService.Next(targetAttack + 1), 1), character.SoldierNumber);

        // 突撃
        if (mySoldierType.IsRush())
        {
          targetDamage = Math.Min(Math.Max((int)(targetDamage + mySoldierType.CalcRushAttack()), 14), targetCharacter.SoldierNumber);
        }
        else if (targetSoldierType.IsRush())
        {
          myDamage = Math.Min(Math.Max((int)(myDamage + targetSoldierType.CalcRushAttack()), 8), character.SoldierNumber);
        }

        character.SoldierNumber -= myDamage;
        if (!isWall)
        {
          myFormationExperience += targetDamage * 0.42f * Math.Max(targetSoldierType.FakeMoney / 24.0f, 1.0f);
        }
        else
        {
          myFormationExperience += Math.Min((targetDamage * 0.17f), 40.0f);
        }
        targetCharacter.SoldierNumber -= targetDamage;
        targetFormationExperience += myDamage * 0.39f * Math.Max(mySoldierType.FakeMoney / 24.0f, 1.0f);

        myExperience += (int)(targetDamage * 0.32f);
        targetExperience += (int)(myDamage * 0.29f);

        await game.CharacterLogAsync("  戦闘 ターン<num>" + i + "</num> <character>" + character.Name + "</character> <num>" + character.SoldierNumber + "</num> (↓<num>" + myDamage + "</num>) | <character>" + targetCharacter.Name + "</character> <num>" + targetCharacter.SoldierNumber + "</num> (↓<num>" + targetDamage + "</num>)");
        if (!isWall)
        {
          await game.CharacterLogByIdAsync(targetCharacter.Id, "  戦闘 ターン<num>" + i + "</num> <character>" + character.Name + "</character> <num>" + character.SoldierNumber + "</num> (↓<num>" + myDamage + "</num>) | <character>" + targetCharacter.Name + "</character> <num>" + targetCharacter.SoldierNumber + "</num> (↓<num>" + targetDamage + "</num>)");
        }

        logLines.Add(new BattleLogLine
        {
          Turn = (short)i,
          AttackerNumber = (short)character.SoldierNumber,
          AttackerDamage = (short)myDamage,
          DefenderNumber = (short)targetCharacter.SoldierNumber,
          DefenderDamage = (short)targetDamage,
        });
      }

      if (isWall)
      {
        // 支配後の配信に影響するので、後の武将の更新処理とはまとめずここで
        targetTown.Wall = targetCharacter.SoldierNumber;
      }

      IEnumerable<TownDefender> removedDefenders = null;
      TownDefender newDefender = null;
      {
        var targetCountry = targetCountryOptional.Data ?? new Country
        {
          Name = "無所属",
        };
        var prefix = $"[<town>{targetTown.Name}</town>]{(continuousCount > 1 ? "(連戦)" : "")} <country>{myCountry.Name}</country> の <character>{character.Name}</character> は <country>{targetCountry.Name}</country> の <character>{targetCharacter.Name}</character>";
        if (character.SoldierNumber <= 0 && targetCharacter.SoldierNumber <= 0)
        {
          removedDefenders = await repo.Town.RemoveDefenderAsync(targetCharacter.Id);
          mapLogId = await game.MapLogAndSaveAsync(EventType.BattleDrawLose, prefix + " と引き分けました", false);
          await game.CharacterLogAsync("<character>" + targetCharacter.Name + "</character> と引き分けました");
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
          await game.CharacterLogAsync($"<character>{targetCharacter.Name}</character> に敗北しました");
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
            await game.CharacterLogByIdAsync(targetCharacter.Id, "<character>" + character.Name + "</character> を撃退しました");
          }
        }
        else if (character.SoldierNumber > 0 && targetCharacter.SoldierNumber > 0)
        {
          mapLogId = await game.MapLogAndSaveAsync(EventType.BattleDraw, prefix + " と引き分けました", false);
          await game.CharacterLogAsync("<character>" + targetCharacter.Name + "</character> と引き分けました");
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

          if (!isWall)
          {
            // 連戦
            if (continuousTurns < 50)
            {
              if (continuousTurns <= 2)
              {
                canContinuous = mySoldierType.CanContinuousOnSingleTurn();
              }
              else
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
            await game.CharacterLogAsync("<character>" + targetCharacter.Name + "</character> に勝利しました");
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
            myExperience += 50;
            myContribution += myExperience;
            character.TownId = targetTown.Id;
            newDefender = await repo.Town.SetDefenderAsync(character.Id, targetTown.Id);
            mapLogId = await game.MapLogAndSaveAsync(EventType.TakeAway, "<country>" + myCountry.Name + "</country> の <character>" + character.Name + "</character> は <country>" + targetCountry.Name + "</country> の <town>" + targetTown.Name + "</town> を支配しました", true);
            await game.CharacterLogAsync("<town>" + targetTown.Name + "</town> を支配しました");

            // 支配したときのみ匿名ストリーミング
            await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(targetTown)));

            if (targetCountryOptional.HasData)
            {
              var targetCountryTownCount = await repo.Town.CountByCountryIdAsync(targetCountry.Id);
              if (targetCountryTownCount <= 0)
              {
                if (targetCountry.AiType != CountryAiType.Farmers)
                {
                  myCountry.PolicyPoint += 2000;
                }
                if (targetCountry.AiType == CountryAiType.Terrorists)
                {
                  await CountryService.SetPolicyAndSaveAsync(repo, myCountry, CountryPolicyType.GetTerrorists, isCheckSubjects: false);
                }
                targetCountry.HasOverthrown = true;
                targetCountry.OverthrownGameDate = game.GameDateTime;
                await StatusStreaming.Default.SendAllAsync(ApiData.From(targetCountry));
                await AnonymousStreaming.Default.SendAllAsync(ApiData.From(targetCountry));
                await game.MapLogAsync(EventType.Overthrown, "<country>" + targetCountry.Name + "</country> は滅亡しました", true);

                var targetCountryCharacters = await repo.Character.RemoveCountryAsync(targetCountry.Id);
                repo.Unit.RemoveUnitsByCountryId(targetCountry.Id);
                repo.Reinforcement.RemoveByCountryId(targetCountry.Id);
                repo.ChatMessage.RemoveByCountryId(targetCountry.Id);
                repo.CountryDiplomacies.RemoveByCountryId(targetCountry.Id);
                repo.Country.RemoveDataByCountryId(targetCountry.Id);

                // 滅亡国武将に通知
                var commanders = new CountryMessage
                {
                  Type = CountryMessageType.Commanders,
                  Message = string.Empty,
                  CountryId = 0,
                };
                foreach (var targetCountryCharacter in await repo.Country.GetCharactersWithIconsAndCommandsAsync(targetCountry.Id))
                {
                  await StatusStreaming.Default.SendCharacterAsync(ApiData.From(targetCountryCharacter.Character), targetCountryCharacter.Character.Id);
                  await StatusStreaming.Default.SendCharacterAsync(ApiData.From(commanders), targetCountryCharacter.Character.Id);
                }

                // 登用分を無効化
                await ChatService.DenyCountryPromotions(repo, targetCountry);

                await StatusStreaming.Default.SendCountryAsync(ApiData.From(myCountry), myCountry.Id);
                StatusStreaming.Default.UpdateCache(targetCountryCharacters);
              }
            }

            var allTowns = await repo.Town.GetAllAsync();
            var allCountries = await repo.Country.GetAllAsync();
            var townAiMap = allTowns.Join(allCountries, t => t.CountryId, c => c.Id, (t, c) => new { CountryId = c.Id, c.AiType, });
            var humanCountry = townAiMap.FirstOrDefault(t => t.AiType != CountryAiType.Terrorists);
            if (allTowns.All(t => t.CountryId > 0) &&
              townAiMap.All(t => t.CountryId == humanCountry.CountryId || t.AiType == CountryAiType.Terrorists))
            {
              var system = await repo.System.GetAsync();
              if (!system.IsWaitingReset)
              {
                await game.MapLogAsync(EventType.Unified, "大陸は、<country>" + myCountry.Name + "</country> によって統一されました", true);
                await ResetService.RequestResetAsync(repo);
              }
            }
          }

          // アイテム
          if (RandomService.Next(0, 120) == 0)
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
      var myFormationPoint = Math.Max(1, (int)myFormationExperience / 10);
      myFormationExperience = Math.Max(1, (int)myFormationExperience);
      myContribution += myExperience;
      character.Contribution += (int)(myContribution);
      character.FormationPoint += myFormationPoint;
      await game.CharacterLogAsync($"戦闘終了 貢献: <num>{myContribution}</num>" + this.AddExperience(myExperience, character, mySoldierType) + $" 陣形ex: <num>{myFormationExperience}</num> 陣形P: <num>{myFormationPoint}</num>");
      myFormationData.Experience += (int)myFormationExperience;
      if (myFormation.CheckLevelUp(myFormationData))
      {
        await game.CharacterLogAsync($"陣形 {myFormation.Name} のレベルが <num>{myFormationData.Level}</num> に上昇しました");
      }
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(myFormationData), character.Id);
      if (!isWall)
      {
        var targetFormationPoint = (int)targetFormationExperience / 10;
        targetContribution += targetExperience;
        targetCharacter.Contribution += (int)(targetContribution);
        targetCharacter.FormationPoint += targetFormationPoint;
        await game.CharacterLogByIdAsync(targetCharacter.Id, $"戦闘終了 貢献: <num>{targetContribution}</num>" + this.AddExperience(targetExperience, targetCharacter, targetSoldierType) + $" 陣形ex: <num>{targetFormationExperience}</num> 陣形P: <num>{targetFormationPoint}</num>");

        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(targetCharacter), targetCharacter.Id);
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(new ApiSignal
        {
          Type = SignalType.DefenderBattled,
          Data = new { townName = targetTown.Name, targetName = character.Name, isWin = targetCharacter.SoldierNumber > 0, },
        }), targetCharacter.Id);

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
