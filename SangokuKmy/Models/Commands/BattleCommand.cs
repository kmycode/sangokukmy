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
    private static readonly Random rand = new Random(DateTime.Now.Millisecond);

    public override CharacterCommandType Type => CharacterCommandType.Battle;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
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
        await game.CharacterLogAsync("自国以外の都市からは攻められません");
        return;
      }
      if (targetTown.CountryId == character.CountryId)
      {
        await game.CharacterLogAsync("自国へ侵攻しようとしました");
        return;
      }

      var targetCountryOptional = await repo.Country.GetByIdAsync(targetTown.CountryId);
      if (targetCountryOptional.HasData)
      {
        var warOptional = await repo.CountryDiplomacies.GetCountryWarAsync(character.CountryId, targetTown.CountryId);

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

      var log = new BattleLog
      {
        TownId = targetTown.Id,
        AttackerCharacterId = character.Id,
      };
      var logLines = new List<BattleLogLine>();
      var attackerCache = character.ToLogCache((await repo.Character.GetCharacterAllIconsAsync(character.Id)).GetMainOrFirst().Data ?? new CharacterIcon());
      uint mapLogId = 0;

      var mySoldierType = DefaultCharacterSoldierTypes.Get(character.SoldierType).Data ?? DefaultCharacterSoldierTypes.Get(SoldierType.Common).Data;
      CharacterSoldierType targetSoldierType;
      var myAttackCorrection = 0;
      var myDefenceCorrection = 0;
      var myAttackSoldierTypeCorrection = 0;
      var myDefenceSoldierTypeCorrection = 0;
      var myExperience = 50;
      var myContribution = 20;
      var targetAttackCorrection = 0;
      var targetDefenceCorrection = 0;
      var targetAttackSoldierTypeCorrection = 0;
      var targetDefenceSoldierTypeCorrection = 0;
      var targetExperience = 50;
      var targetContribution = 0;

      var myPostOptional = (await repo.Country.GetPostsAsync(character.CountryId)).FirstOrDefault(cp => cp.CharacterId == character.Id).ToOptional();
      if (myPostOptional.HasData)
      {
        var myPost = myPostOptional.Data;
        if (myPost.Type == CountryPostType.BowmanGeneral)
        {
          if (character.SoldierType == SoldierType.Archer)
          {
            myAttackCorrection += 10;
          }
        }
        else if (myPost.Type == CountryPostType.CavalryGeneral)
        {
          if (character.SoldierType == SoldierType.HeavyCavalry || character.SoldierType == SoldierType.LightCavalry)
          {
            myAttackCorrection += 10;
          }
        }
        else if (myPost.Type == CountryPostType.GuardGeneral)
        {
          if (character.SoldierType == SoldierType.Guard)
          {
            myAttackCorrection += 10;
          }
        }
        else if (myPost.Type == CountryPostType.GrandGeneral)
        {
          myAttackCorrection += 10;
        }
        else if (myPost.Type == CountryPostType.General)
        {
          if (character.SoldierType == SoldierType.Common)
          {
            myAttackCorrection += 10;
          }
        }
        else if (myPost.Type == CountryPostType.Monarch)
        {
          myAttackCorrection += 20;
        }
      }

      var enemy = new EnemyData();
      var wallChara = new Character();
      var trendStrong = game.GameDateTime.ToInt() / 27;
      var defenders = await repo.Town.GetDefendersAsync(targetTown.Id);
      LogCharacterCache defenderCache = null;
      if (defenders.Any())
      {
        var defender = defenders.First().Character;
        enemy.Defender = defender.ToOptional();
        enemy.SoldierNumber = defender.SoldierNumber;
        enemy.SoldierType = defender.SoldierType;
        enemy.Strong = defender.Strong;
        enemy.Proficiency = defender.Proficiency;
        log.DefenderCharacterId = defender.Id;
        log.DefenderType = DefenderType.Character;
        defenderCache = defender.ToLogCache((await repo.Character.GetCharacterAllIconsAsync(defender.Id)).GetMainOrFirst().Data ?? new CharacterIcon());

        await game.CharacterLogByIdAsync(defender.Id, $"守備をしている <town>{targetTown.Name}</town> に <character>{character.Name}</character> が攻め込み、戦闘になりました");
      }
      else
      {
        enemy.SoldierNumber = targetTown.Wall;
        log.DefenderType = DefenderType.Wall;
        defenderCache = new LogCharacterCache
        {
          CountryId = targetTown.CountryId,
          IconId = 0,
          SoldierNumber = enemy.SoldierNumber,
        };
      }

      await game.CharacterLogAsync("<town>" + targetTown.Name + "</town> に攻め込みました");
      
      for (var i = 1; i <= 50 && enemy.SoldierNumber > 0 && character.SoldierNumber > 0; i++)
      {
        var isNoDamage = false;
        BattlerEnemyType enemyType;

        if (enemy.IsWall)
        {
          if (targetTown.WallGuard > 0)
          {
            enemy.SoldierType = targetTown.Technology > 900 ? SoldierType.Guard_Step4 :
                                targetTown.Technology > 700 ? SoldierType.Guard_Step3 :
                                targetTown.Technology > 500 ? SoldierType.Guard_Step2 :
                                targetTown.Technology > 300 ? SoldierType.Guard_Step1 :
                                SoldierType.Common;
            enemy.Strong = trendStrong;
            enemy.Proficiency = 100;

            if (i == 1)
            {
              defenderCache.Name = "守兵";
              defenderCache.Strong = (short)enemy.Strong;
              defenderCache.SoldierType = enemy.SoldierType;
              defenderCache.Proficiency = (short)enemy.Proficiency;
            }

            enemyType = BattlerEnemyType.WallGuard;
          }
          else
          {
            enemy.SoldierType = SoldierType.Common;
            enemy.Strong = trendStrong / 2;
            enemy.Proficiency = 0;
            isNoDamage = true;

            if (i == 1)
            {
              defenderCache.Name = "城壁";
              defenderCache.Strong = (short)enemy.Strong;
              defenderCache.SoldierType = enemy.SoldierType;
              defenderCache.Proficiency = (short)enemy.Proficiency;
            }
            else if (defenderCache.Name == "守兵")
            {
              defenderCache.Name = "守兵/" + i + "ターン目より城壁";
            }

            enemyType = BattlerEnemyType.Wall;
          }

          wallChara.Strong = defenderCache.Strong;
          wallChara.Intellect = defenderCache.Intellect;
          wallChara.Leadership = defenderCache.Leadership;
          wallChara.Popularity = defenderCache.Popularity;
          wallChara.SoldierType = defenderCache.SoldierType;
          wallChara.SoldierNumber = defenderCache.SoldierNumber;
          wallChara.Proficiency = defenderCache.Proficiency;
        }
        else
        {
          enemyType = enemy.SoldierType == SoldierType.StrongGuards ? BattlerEnemyType.StrongGuards : BattlerEnemyType.Character;
        }
        targetSoldierType = DefaultCharacterSoldierTypes.Get(enemy.SoldierType).Data ?? DefaultCharacterSoldierTypes.Get(SoldierType.Common).Data;

        var (ka, kd) = mySoldierType.CalcCorrections(character, enemyType);
        myAttackSoldierTypeCorrection = ka;
        myDefenceSoldierTypeCorrection = kd;

        var (ea, ed) = targetSoldierType.CalcCorrections(enemy.Defender.Data ?? wallChara, BattlerEnemyType.Character);
        targetAttackSoldierTypeCorrection = ea;
        targetDefenceSoldierTypeCorrection = ed;

        var myAttack = Math.Max((int)((character.Strong + myAttackCorrection + myAttackSoldierTypeCorrection - targetDefenceCorrection - targetDefenceSoldierTypeCorrection - enemy.Proficiency / 2.5f) / 8), 0);
        var targetAttack = Math.Max((int)((enemy.Strong + targetAttackCorrection + targetAttackSoldierTypeCorrection - myDefenceCorrection - myDefenceSoldierTypeCorrection - character.Proficiency / 2.5f) / 8), 0);
        var targetDamage = Math.Min(Math.Max(rand.Next(myAttack + 1), 1), enemy.SoldierNumber);
        var myDamage = Math.Min(Math.Max(rand.Next(targetAttack + 1), 1), character.SoldierNumber);
        if (isNoDamage)
        {
          myDamage = 1;
        }

        character.SoldierNumber -= myDamage;
        myExperience += targetDamage;
        enemy.SoldierNumber -= targetDamage;
        targetExperience += myDamage;

        await game.CharacterLogAsync("  戦闘 ターン<num>" + i + "</num> <character>" + character.Name + "</character> <num>" + character.SoldierNumber + "</num> (↓<num>" + myDamage + "</num>) | <character>" + enemy.Name + "</character> <num>" + enemy.SoldierNumber + "</num> (↓<num>" + targetDamage + "</num>)");
        if (!enemy.IsWall)
        {
          await game.CharacterLogByIdAsync(enemy.Defender.Data.Id, "  戦闘 ターン<num>" + i + "</num> <character>" + character.Name + "</character> <num>" + character.SoldierNumber + "</num> (↓<num>" + myDamage + "</num>) | <character>" + enemy.Name + "</character> <num>" + enemy.SoldierNumber + "</num> (↓<num>" + targetDamage + "</num>)");
        }
        else
        {
          targetTown.WallGuard = Math.Max(targetTown.WallGuard - targetDamage, 0);
          targetTown.Wall = enemy.SoldierNumber;
        }

        logLines.Add(new BattleLogLine
        {
          Turn = (short)i,
          AttackerNumber = (short)character.SoldierNumber,
          AttackerDamage = (short)myDamage,
          DefenderNumber = (short)enemy.SoldierNumber,
          DefenderDamage = (short)targetDamage,
        });
      }

      {
        var targetCountry = targetCountryOptional.Data ?? new Country
        {
          Name = "無所属",
        };
        var prefix = "[<town>" + targetTown.Name + "</town>] <country>" + myCountry.Name + "</country> の <character>" + character.Name + "</character> は <country>" + targetCountry.Name + "</country> の <character>" + enemy.Name + "</character>";
        if (character.SoldierNumber <= 0 && enemy.SoldierNumber <= 0)
        {
          repo.Town.RemoveDefender(enemy.Defender.Data.Id);
          mapLogId = await game.MapLogAndSaveAsync(EventType.BattleDrawLose, prefix + " と引き分けました", false);
          await game.CharacterLogAsync("<character>" + enemy.Name + "</character> と引き分けました");
          if (enemy.Defender.HasData)
          {
            await game.CharacterLogByIdAsync(enemy.Defender.Data.Id, $"<character>{character.Name}</character> と引き分けました。<town>{targetTown.Name}</town> の守備から外れました");
          }
        }
        else if (character.SoldierNumber <= 0 && enemy.SoldierNumber > 0)
        {
          mapLogId = await game.MapLogAndSaveAsync(EventType.BattleLose, prefix + " に敗北しました", false);
          await game.CharacterLogAsync($"<character>{enemy.Name}</character> に敗北しました。<town>{targetTown.Name}</town> の守備から外れました");
          if (enemy.Defender.HasData)
          {
            await game.CharacterLogByIdAsync(enemy.Defender.Data.Id, "<character>" + character.Name + "</character> を撃退しました");
          }
        }
        else if (character.SoldierNumber > 0 && enemy.SoldierNumber > 0)
        {
          mapLogId = await game.MapLogAndSaveAsync(EventType.BattleDraw, prefix + " と引き分けました", false);
          await game.CharacterLogAsync("<character>" + enemy.Name + "</character> と引き分けました");
          if (enemy.Defender.HasData)
          {
            await game.CharacterLogByIdAsync(enemy.Defender.Data.Id, "<character>" + character.Name + "</character> と引き分けました");
          }
        }
        else
        {
          if (!enemy.IsWall)
          {
            repo.Town.RemoveDefender(enemy.Defender.Data.Id);
            mapLogId = await game.MapLogAndSaveAsync(EventType.BattleWin, prefix + " を倒しました", false);
            await game.CharacterLogAsync("<character>" + enemy.Name + "</character> に勝利しました");
            await game.CharacterLogByIdAsync(enemy.Defender.Data.Id, $"<character>{character.Name}</character> に敗北しました。<town>{targetTown.Name}</town> の守備から外れました");
          }
          else
          {
            targetTown.CountryId = character.CountryId;
            targetTown.Agriculture = (int)(targetTown.Agriculture * 0.8f);
            targetTown.Commercial = (int)(targetTown.Commercial * 0.8f);
            targetTown.Technology = (int)(targetTown.Technology * 0.8f);
            targetTown.People = (int)(targetTown.People * 0.8f);
            targetTown.Security = (short)(targetTown.Security * 0.8f);
            myExperience += 50;
            myContribution += myExperience;
            character.TownId = targetTown.Id;
            await repo.Town.SetDefenderAsync(character.Id, targetTown.Id);
            mapLogId = await game.MapLogAndSaveAsync(EventType.TakeAway, "<country>" + myCountry.Name + "</country> の <character>" + character.Name + "</character> は <country>" + targetCountry.Name + "</country> の <town>" + targetTown.Name + "</town> を支配しました", true);
            await game.CharacterLogAsync("<town>" + targetTown.Name + "</town> を支配しました");

            if (targetCountryOptional.HasData)
            {
              var targetCountryTownCount = await repo.Town.CountByCountryIdAsync(targetCountry.Id);
              if (targetCountryTownCount <= 0)
              {
                targetCountry.HasOverthrown = true;
                targetCountry.OverthrownGameDate = game.GameDateTime;
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
                foreach (var targetCountryCharacter in await repo.Country.GetCharactersAsync(targetCountry.Id))
                {
                  await StatusStreaming.Default.SendCharacterAsync(ApiData.From(targetCountryCharacter.Character), targetCountryCharacter.Character.Id);
                  await StatusStreaming.Default.SendCharacterAsync(ApiData.From(commanders), targetCountryCharacter.Character.Id);
                }

                StatusStreaming.Default.UpdateCache(targetCountryCharacters);
              }
            }

            if (await repo.Town.IsUnifiedAsync(character.CountryId))
            {
              await game.MapLogAsync(EventType.Unified, "<country>" + myCountry.Name + "</country> によって統一されました", true);
              await ResetService.RequestResetAsync(repo);
            }
          }
        }
      }

      // 戦闘ログを保存
      log.MapLogId = mapLogId;
      await repo.BattleLog.AddLogWithSaveAsync(log, logLines, attackerCache, defenderCache);
      await repo.MapLog.SetBattleLogIdAsync(mapLogId, log.Id);

      // 貢献、経験値の設定
      myContribution += myExperience;
      character.Contribution += (int)(myContribution * 4.6f);
      character.AddStrongEx((short)myExperience);
      await game.CharacterLogAsync($"戦闘終了 貢献: <num>{myContribution}</num> 武力経験: <num>{myExperience}</num>");
      if (enemy.Defender.HasData)
      {
        var defender = enemy.Defender.Data;
        targetContribution += targetExperience;
        defender.Contribution += (int)(targetContribution * 4.6f);
        defender.AddStrongEx((short)targetExperience);
        defender.SoldierNumber = enemy.SoldierNumber;
        await game.CharacterLogByIdAsync(defender.Id, $"戦闘終了 貢献: <num>{targetContribution}</num> 武力経験: <num>{targetExperience}</num>");

        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(defender), defender.Id);
      }

      // 更新された都市データを通知
      await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(targetTown));
    }

    private class EnemyData
    {
      public Optional<Character> Defender { get; set; }

      public string Name => this.IsWall ? "城壁" : this.Defender.Data.Name;

      public bool IsWall => !this.Defender.HasData;

      public int SoldierNumber { get; set; }

      public SoldierType SoldierType { get; set; }

      public int Strong { get; set; }

      public int Proficiency { get; set; }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var townId = (uint)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var town = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.InternalDataNotFoundError, new { command = "move", townId, });
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
