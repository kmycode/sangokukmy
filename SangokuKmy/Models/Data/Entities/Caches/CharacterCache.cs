using System;
namespace SangokuKmy.Models.Data.Entities.Caches
{
  /// <summary>
  /// 武将のキャッシュデータ
  /// </summary>
  public class CharacterCache : IEntityCache
  {
    public uint Id { get; }

    public string AliasId { get; }

    public string Name { get; }

    public uint CountryId { get; }

    public CharacterCache(Character chara)
    {
      this.Id = chara.Id;
      this.AliasId = chara.AliasId;
      this.Name = chara.Name;
      this.CountryId = chara.CountryId;
    }
  }
}
