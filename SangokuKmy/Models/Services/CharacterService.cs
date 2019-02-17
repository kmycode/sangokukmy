using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Services
{
  public static class CharacterService
  {
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
