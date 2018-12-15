using System;
using System.Linq;
namespace SangokuKmy.Models.Data.Entities.Caches
{
  public static class EntityCaches
  {
    public static CharacterCache ToCache(this Character chara) => new CharacterCache(chara);
  }
}
