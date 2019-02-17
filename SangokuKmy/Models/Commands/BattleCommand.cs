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

      var myAttackCorrection = 0;
      var myDefenceCorrection = 0;
      var myExperience = 0;
      var myContribution = 20;
      var targetAttackCorrection = 0;
      var targetDefenceCorrection = 0;
      var targetExperience = 0;
      var targetContribution = 0;

      var myCorrections = this.SoldierTypeCorrect(character);
      myAttackCorrection += myCorrections.AttackCorrection;
      myDefenceCorrection += myCorrections.DefenceCorrection;

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

        var targetCorrections = this.SoldierTypeCorrect(defender);
        targetAttackCorrection += targetCorrections.AttackCorrection;
        targetDefenceCorrection += targetCorrections.DefenceCorrection;
      }
      else
      {
        enemy.SoldierNumber = targetTown.Wall;
        log.DefenderType = DefenderType.Wall;
        defenderCache = new LogCharacterCache
        {
          CountryId = targetTown.CountryId,
          IconId = 0,
        };
      }

      var myAttack = Math.Max((int)((character.Strong + myAttackCorrection - targetDefenceCorrection - enemy.Proficiency / 2.5f) / 8), 0);
      await game.CharacterLogAsync("<town>" + targetTown.Name + "</town> に攻め込みました");
      
      for (var i = 1; i <= 50 && enemy.SoldierNumber > 0 && character.SoldierNumber > 0; i++)
      {
        var isNoDamage = false;
        if (enemy.IsWall)
        {
          if (targetTown.WallGuard > 0)
          {
            enemy.SoldierType = targetTown.Technology > 900 ? SoldierType.RepeatingCrossbow :
                                targetTown.Technology > 700 ? SoldierType.StrongCrossbow :
                                targetTown.Technology > 500 ? SoldierType.HeavyInfantry :
                                targetTown.Technology > 300 ? SoldierType.LightInfantry :
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
            else if (defenderCache.Name == "城壁")
            {
              defenderCache.Name = "守兵/" + i + "ターン目より城壁";
            }
          }
        }
        
        var targetAttack = Math.Max((int)((enemy.Strong + targetAttackCorrection - myDefenceCorrection - character.Proficiency / 2.5f) / 8), 0);
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
          await game.CharacterLogAsync("<character>" + enemy.Name + "</character> と引き分けました。貢献:<num>" + myContribution + "</num> 経験:<num>" + myExperience + "</num>");
          if (enemy.Defender.HasData)
          {
            await game.CharacterLogByIdAsync(enemy.Defender.Data.Id, "<character>" + character.Name + "</character> と引き分けました。貢献:<num>" + targetContribution + "</num> 経験:<num>" + targetExperience + "</num>");
          }
        }
        else if (character.SoldierNumber <= 0 && enemy.SoldierNumber > 0)
        {
          mapLogId = await game.MapLogAndSaveAsync(EventType.BattleLose, prefix + " に敗北しました", false);
          await game.CharacterLogAsync("<character>" + enemy.Name + "</character> に敗北しました。貢献:<num>" + myContribution + "</num> 経験:<num>" + myExperience + "</num>");
          if (enemy.Defender.HasData)
          {
            await game.CharacterLogByIdAsync(enemy.Defender.Data.Id, "<character>" + character.Name + "</character> を撃退しました。貢献:<num>" + targetContribution + "</num> 経験:<num>" + targetExperience + "</num>");
          }
        }
        else if (character.SoldierNumber > 0 && enemy.SoldierNumber > 0)
        {
          mapLogId = await game.MapLogAndSaveAsync(EventType.BattleDraw, prefix + " と引き分けました", false);
          await game.CharacterLogAsync("<character>" + enemy.Name + "</character> と引き分けました。貢献:<num>" + myContribution + "</num> 経験:<num>" + myExperience + "</num>");
          if (enemy.Defender.HasData)
          {
            await game.CharacterLogByIdAsync(enemy.Defender.Data.Id, "<character>" + character.Name + "</character> と引き分けました。貢献:<num>" + targetContribution + "</num> 経験:<num>" + targetExperience + "</num>");
          }
        }
        else
        {
          if (!enemy.IsWall)
          {
            repo.Town.RemoveDefender(enemy.Defender.Data.Id);
            mapLogId = await game.MapLogAndSaveAsync(EventType.BattleWin, prefix + " を倒しました", false);
            await game.CharacterLogAsync("<character>" + enemy.Name + "</character> に勝利しました。貢献:<num>" + myContribution + "</num> 経験:<num>" + myExperience + "</num>");
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
            await game.CharacterLogAsync("<town>" + targetTown.Name + "</town> を支配しました。貢献:<num>" + myContribution + "</num> 経験:<num>" + myExperience + "</num>");

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
                repo.CountryDiplomacies.RemoveByCountryId(targetCountry.Id);

                // 滅亡国武将に通知
                foreach (var targetCountryCharacter in await repo.Country.GetCharactersAsync(targetCountry.Id))
                {
                  await StatusStreaming.Default.SendCharacterAsync(ApiData.From(targetCountryCharacter.Character), targetCountryCharacter.Character.Id);
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
      if (enemy.Defender.HasData)
      {
        var defender = enemy.Defender.Data;
        targetContribution += targetExperience;
        defender.Contribution += (int)(targetContribution * 4.6f);
        defender.AddStrongEx((short)targetExperience);
        defender.SoldierNumber = enemy.SoldierNumber;

        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(defender), defender.Id);
      }

      // 更新された都市データを通知
      await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(targetTown));
    }

    private (int AttackCorrection, int DefenceCorrection) SoldierTypeCorrect(Character character)
    {
      var a = 0;
      var d = 0;
      switch (character.SoldierType)
      {
        case SoldierType.Guard:
          a = 20;
          d = 20;
          break;
        case SoldierType.LightInfantry:
          a = 10;
          break;
        case SoldierType.Archer:
          d = 15;
          break;
        case SoldierType.LightCavalry:
          a = 35;
          d = 10;
          break;
        case SoldierType.StrongCrossbow:
          a = 10;
          d = 35;
          break;
        case SoldierType.LightIntellect:
          a = character.Intellect;
          break;
        case SoldierType.HeavyInfantry:
          a = 50;
          d = 30;
          break;
        case SoldierType.HeavyCavalry:
          a = 60;
          d = 40;
          break;
        case SoldierType.Intellect:
          a = (int)(character.Intellect * 0.7f);
          d = (int)(character.Intellect * 0.4f);
          break;
        case SoldierType.RepeatingCrossbow:
          a = 80;
          d = 10;
          break;
        case SoldierType.StrongGuards:
          d = character.Intellect;
          break;
      }
      return (a, d);
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
