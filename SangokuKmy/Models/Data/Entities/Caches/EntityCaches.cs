using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Entities.Caches
{
  public static class EntityCaches
  {
    public static IList<CharacterCache> Characters { get; private set; }

    public static CharacterCache ToCache(this Character chara) => new CharacterCache(chara);

    public static async Task UpdateCharactersAsync(MainRepository repo)
    {
      Characters = (await repo.Character.GetAllCachesAsync()).ToList();
    }

    public static void UpdateCharacter(Character chara)
    {
      var exists = Characters.FirstOrDefault(c => c.Id == chara.Id);
      if (exists != null)
      {
        Characters.Remove(exists);
      }
      Characters.Add(chara.ToCache());
    }
  }
}
