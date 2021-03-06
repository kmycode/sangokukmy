﻿using SangokuKmy.Common;
using SangokuKmy.Models.Commands;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Services
{
  public static class CharacterService
  {
    public static async Task ChangeCountryAsync(MainRepository repo, uint newId, IEnumerable<Character> charas)
    {
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(new ApiSignal
      {
        Type = SignalType.StartStreamingNewCountryInitializationData,
      }), charas.Select(c => c.Id));

      foreach (var chara in charas)
      {
        // 部隊とか役職とか
        repo.Country.RemoveCharacterPosts(chara.Id);
        repo.Town.RemoveDefender(chara.Id);
        var unitMember = await repo.Unit.GetByMemberIdAsync(chara.Id);
        if (unitMember.Member.HasData)
        {
          if (unitMember.Member.Data.Post == UnitMemberPostType.Leader)
          {
            repo.Unit.RemoveUnit(unitMember.Member.Data.UnitId);
          }
          else
          {
            repo.Unit.RemoveMember(unitMember.Member.Data.CharacterId);
          }
        }

        var commands = await repo.CharacterCommand.GetAsync(chara.Id, new GameDateTime[]
        {
          chara.LastUpdatedGameDate,
          chara.LastUpdatedGameDate.AddMonth(1),
          chara.LastUpdatedGameDate.AddMonth(2),
          chara.LastUpdatedGameDate.AddMonth(3),
          chara.LastUpdatedGameDate.AddMonth(4),
        });
        if (commands.Any())
        {
          await StatusStreaming.Default.SendCountryAsync(commands.Select(c => ApiData.From(c)), newId);
        }

        // 手紙や指令の既読
        var read = await repo.ChatMessage.GetReadByCharacterIdAsync(chara.Id);
        read.LastAllCommanderId = read.LastAttributeCommanderId = read.LastCountryChatMessageId =
          read.LastFromCommanderId = read.LastPrivateCommanderId = 0;

        chara.CountryId = newId;
      }

      StatusStreaming.Default.UpdateCache(charas);

      // 古い国のデータ、新しい国のデータ
      var country = await repo.Country.GetByIdAsync(newId);
      var towns = await repo.Town.GetAllAsync();
      var characters = (await repo.Character.GetAllAliveWithIconAsync()).Where(c => !charas.Any(cc => cc.Id != c.Character.Id));
      var charaCommands = (await repo.Country.GetCharactersWithIconsAndCommandsAsync(newId)).SelectMany(c => c.Commands);
      var defenders = await repo.Town.GetAllDefendersAsync();
      var policies = await repo.Country.GetPoliciesAsync(newId);
      var commandComments = await repo.CharacterCommand.GetMessagesAsync(newId);
      var alliances = await repo.CountryDiplomacies.GetCountryAllAlliancesAsync(newId);

      var ods = new List<TownDefender>();
      foreach (var od in defenders.Where(d => !towns.Any(t => t.Id == d.TownId && t.CountryId == newId)))
      {
        ods.Add(new TownDefender
        {
          Id = od.Id,
          TownId = od.TownId,
          CharacterId = od.CharacterId,
          Status = TownDefenderStatus.Losed,
        });
      }

      foreach (var townGroup in charas.GroupBy(c => c.TownId))
      {
        var townId = townGroup.Key;
        var townCharas = townGroup.Select(tc => tc.Id);
        await StatusStreaming.Default.SendCharacterAsync(characters.Where(c => c.Character.CountryId == newId && c.Character.TownId == townId).Select(c => ApiData.From(new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.SameTownAndSameCountry))), townCharas);
        await StatusStreaming.Default.SendCharacterAsync(characters.Where(c => c.Character.CountryId == newId && c.Character.TownId != townId).Select(c => ApiData.From(new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.SameCountry))), townCharas);
        await StatusStreaming.Default.SendCharacterAsync(characters.Where(c => c.Character.CountryId != newId && c.Character.TownId == townId && c.Character.AiType != CharacterAiType.SecretaryScouter).Select(c => ApiData.From(new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.SameTown))), townCharas);
        await StatusStreaming.Default.SendCharacterAsync(characters.Where(c => c.Character.CountryId != newId && c.Character.TownId != townId && c.Character.AiType != CharacterAiType.SecretaryScouter && towns.Any(ct => c.Character.TownId == ct.Id && ct.CountryId == newId)).Select(c => ApiData.From(new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.SameCountryTownOtherCountry))), townCharas);
        await StatusStreaming.Default.SendCharacterAsync(characters.Where(c => c.Character.CountryId != newId && (c.Character.AiType == CharacterAiType.SecretaryScouter || (c.Character.TownId != townId && !towns.Any(ct => c.Character.TownId == ct.Id && ct.CountryId == newId)))).Select(c => ApiData.From(new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.Anonymous))), townCharas);
      }

      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(new ApiSignal
      {
        Type = SignalType.EndStreamingNewCountryInitializationData,
      }), charas.Select(c => c.Id));
    }

    public static async Task ChangeTownAsync(MainRepository repo, uint newId, Character chara)
    {
      var oldTown = (await repo.Town.GetByIdAsync(chara.TownId)).Data;
      var newTown = (await repo.Town.GetByIdAsync(newId)).Data;

      if (oldTown == null || newTown == null)
      {
        return;
      }

      var oldTownCharacters = (await repo.Town.GetCharactersAsync(chara.TownId)).Where(c => c.Id != chara.Id).Select(c => c.Id);
      var newTownCharacters = (await repo.Town.GetCharactersAsync(newId)).Select(c => c.Id);

      chara.TownId = newId;

      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(newTown), chara.Id);
      if (oldTown.CountryId != chara.CountryId)
      {
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(new TownForAnonymous(oldTown)), chara.Id);
      }

      await StreamCharacterAsync(repo, chara);

      var allCharacters = await repo.Character.GetAllAliveWithIconAsync();
      var allTowns = await repo.Town.GetAllAsync();
      var townId = newId;
      await StatusStreaming.Default.SendCharacterAsync(allCharacters.Where(c => c.Character.CountryId == chara.CountryId && c.Character.TownId == townId).Select(c => ApiData.From(new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.SameTownAndSameCountry))), chara.Id);
      await StatusStreaming.Default.SendCharacterAsync(allCharacters.Where(c => c.Character.CountryId == chara.CountryId && c.Character.TownId != townId).Select(c => ApiData.From(new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.SameCountry))), chara.Id);
      await StatusStreaming.Default.SendCharacterAsync(allCharacters.Where(c => c.Character.CountryId != chara.CountryId && c.Character.TownId == townId && c.Character.AiType != CharacterAiType.SecretaryScouter).Select(c => ApiData.From(new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.SameTown))), chara.Id);
      await StatusStreaming.Default.SendCharacterAsync(allCharacters.Where(c => c.Character.CountryId != chara.CountryId && c.Character.TownId != townId && c.Character.AiType != CharacterAiType.SecretaryScouter && allTowns.Any(ct => c.Character.TownId == ct.Id && ct.CountryId == chara.CountryId)).Select(c => ApiData.From(new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.SameCountryTownOtherCountry))), chara.Id);
      await StatusStreaming.Default.SendCharacterAsync(allCharacters.Where(c => c.Character.CountryId != chara.CountryId && (c.Character.AiType == CharacterAiType.SecretaryScouter || (c.Character.TownId != townId && !allTowns.Any(ct => c.Character.TownId == ct.Id && ct.CountryId == chara.CountryId)))).Select(c => ApiData.From(new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.Anonymous))), chara.Id);
    }

    public static async Task StreamCharacterAsync(MainRepository repo, Character character)
    {
      // 同じ都市にいる他国の武将への通知、移動前の都市に滞在かつ違う国の武将への都市移動の通知は、以下の処理の中に含まれるので特段の対処は不要
      var icon = (await repo.Character.GetCharacterAllIconsAsync(character.Id)).GetMainOrFirst();
      var town = await repo.Town.GetByIdAsync(character.TownId);
      var townCharacters = await repo.Town.GetCharactersAsync(character.TownId);

      // この武将の所属する国の武将
      await StatusStreaming.Default.SendCountryExceptForCharacterAsync(ApiData.From(new CharacterForAnonymous(character, icon.Data, CharacterShareLevel.SameCountry)), character.CountryId, character.Id);

      // 都市の武将
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(new CharacterForAnonymous(character, icon.Data, character.AiType != CharacterAiType.SecretaryScouter ? CharacterShareLevel.SameTown : CharacterShareLevel.Anonymous)), townCharacters.Where(tc => tc.Id != character.Id && tc.CountryId != character.CountryId).Select(tc => tc.Id));

      if (town.HasData && town.Data.CountryId != character.CountryId)
      {
        // この都市を所有している国の武将
        await StatusStreaming.Default.SendCountryExceptForCharacterAsync(ApiData.From(new CharacterForAnonymous(character, icon.Data, character.AiType != CharacterAiType.SecretaryScouter ? CharacterShareLevel.SameCountryTownOtherCountry : CharacterShareLevel.Anonymous)), town.Data?.CountryId ?? 0, townCharacters.Where(tc => tc.CountryId == town.Data?.CountryId).Select(tc => tc.Id));
      }

      // 本人と違う国で、かつ同じ都市にいない武将
      await StatusStreaming.Default.SendAllExceptForCharactersAndCountryAsync(ApiData.From(new CharacterForAnonymous(character, icon.Data, CharacterShareLevel.Anonymous)), townCharacters.Select(tc => tc.Id), new uint[] { town.Data?.CountryId ?? 0, character.CountryId, });

      // 本人
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(character), character.Id);
    }

    public static async Task RemoveAsync(MainRepository repo, Character character)
    {
      var unitData = await repo.Unit.GetByMemberIdAsync(character.Id);
      if (unitData.Unit.HasData && unitData.Member.HasData)
      {
        if (unitData.Member.Data.Post == UnitMemberPostType.Leader)
        {
          await UnitService.RemoveAsync(repo, unitData.Unit.Data.Id);
        }
        else
        {
          UnitService.Leave(repo, character.Id);
        }
      }

      var countryPosts = (await repo.Country.GetCharacterPostsAsync(character.Id)).ToArray();
      if (countryPosts.Any())
      {
        foreach (var post in countryPosts)
        {
          repo.Country.RemoveCharacterPost(post);
        }
      }

      var items = await repo.Character.GetItemsAsync(character.Id);
      foreach (var item in items)
      {
        var flag = false;

        var info = item.GetInfo();
        if (info.HasData)
        {
          if (info.Data.IsUniqueCharacter)
          {
            // 一部のアイテムは放置削除と同時に消える
            await ItemService.SpendCharacterAsync(repo, item, character, isWithEffect: false);
            flag = true;
          }
        }

        if (!flag)
        {
          await ItemService.ReleaseCharacterAsync(repo, item, character);
        }
      }

      // 武将に関連付けられた都市を削除
      if ((await repo.System.GetAsync()).RuleSet != GameRuleSet.SimpleBattle)
      {
        var towns = await repo.Town.GetAllAsync();
        foreach (var town in towns.Where(t => t.UniqueCharacterId == character.Id))
        {
          var charas = await repo.Town.GetCharactersAsync(town.Id);
          foreach (var townChara in charas)
          {
            await ChangeTownAsync(repo, RandomService.Next(towns.Where(t => t.UniqueCharacterId != character.Id)).Id, townChara);
          }
          await repo.Town.RemoveTownDefendersAsync(town.Id);
          repo.Town.Remove(town);

          town.Type = TownType.Removed;
          await StatusStreaming.Default.SendAllAsync(ApiData.From(town));
          await AnonymousStreaming.Default.SendAllAsync(ApiData.From(town));

          await LogService.AddMapLogAsync(repo, true, EventType.TownRemoved, $"<character>{character.Name}</character> の建設した <town>{town.Name}</town> は消滅しました");

          if (!towns.Any(t => t.CountryId == town.CountryId && t.Type != TownType.Removed))
          {
            var countryOptional = await repo.Country.GetAliveByIdAsync(town.CountryId);
            if (countryOptional.HasData)
            {
              await CountryService.OverThrowAsync(repo, countryOptional.Data, null);
            }
          }
        }
      }

      var ais = await repo.Character.GetManagementByHolderCharacterIdAsync(character.Id);
      foreach (var ai in ais)
      {
        var aiChara = await repo.Character.GetByIdAsync(ai.CharacterId);
        if (aiChara.HasData && !aiChara.Data.HasRemoved)
        {
          aiChara.Data.DeleteTurn = (short)Config.DeleteTurns;
        }
        repo.Character.RemoveManagement(ai);
      }
      if (character.AiType == CharacterAiType.FlyingColumn)
      {
        var management = await repo.Character.GetManagementByAiCharacterIdAsync(character.Id);
        if (management.HasData)
        {
          repo.Character.RemoveManagement(management.Data);
        }
      }

      repo.ScoutedTown.RemoveCharacter(character.Id);
      repo.Town.RemoveDefender(character.Id);
      repo.EntryHost.RemoveCharacter(character.Id);
      repo.PushNotificationKey.RemoveByCharacterId(character.Id);
      repo.Mute.RemoveCharacter(character.Id);
      await repo.AuthenticationData.RemoveCharacterAsync(character.Id);

      character.PasswordHash = character.AliasId;
      character.AliasId = "RM";   // 新規登録で4文字未満のIDは登録できない
      character.HasRemoved = true;
    }

    public static int GetItemMax(IEnumerable<CharacterSkill> skills)
    {
      return 4 + skills.GetSumOfValues(CharacterSkillEffectType.ItemMax);
    }

    public static int CountLimitedItems(IEnumerable<CharacterItem> items)
    {
      return items.Where(i => i.Status == CharacterItemStatus.CharacterHold).GetInfos().Count(i => !i.IsResource || i.IsResourceItem);
    }
  }
}
