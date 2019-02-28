using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;
using System;
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
      foreach (var chara in charas)
      {
        // 部隊とか役職とか
        repo.Country.RemoveCharacterPosts(chara.Id);
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

        chara.CountryId = newId;
      }

      StatusStreaming.Default.UpdateCache(charas);

      // 指令
      var commanders = (await repo.Country.GetMessageAsync(newId, CountryMessageType.Commanders)).Data ?? new CountryMessage
      {
        Type = CountryMessageType.Commanders,
        Message = string.Empty,
        CountryId = newId,
      };
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(commanders), charas.Select(c => c.Id));
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
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(newTown), newTownCharacters);
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(new TownForAnonymous(oldTown)), chara.Id);
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(oldTown), oldTownCharacters);

      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(chara), chara.Id);
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

      var countryPosts = (await repo.Country.GetPostsAsync(character.CountryId))
        .Where(p => p.CharacterId == character.Id)
        .ToArray();
      if (countryPosts.Any())
      {
        foreach (var post in countryPosts)
        {
          post.Type = CountryPostType.General;
          await repo.Country.SetPostAsync(post);
        }
      }

      repo.ScoutedTown.RemoveCharacter(character.Id);
      repo.Town.RemoveDefender(character.Id);
      repo.EntryHost.RemoveCharacter(character.Id);
      await repo.AuthenticationData.RemoveCharacterAsync(character.Id);

      character.PasswordHash = character.AliasId;
      character.AliasId = "RM";   // 新規登録で4文字未満のIDは登録できない
      character.HasRemoved = true;
    }
  }
}
