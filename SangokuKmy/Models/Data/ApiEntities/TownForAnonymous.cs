using System;
using Newtonsoft.Json;
using SangokuKmy.Models.Data.Entities;
namespace SangokuKmy.Models.Data.ApiEntities
{
  /// <summary>
  /// その都市の所有国でなくても、誰でも見れる都市データ
  /// </summary>
  public class TownForAnonymous
  {
    [JsonProperty("id")]
    public uint Id { get; set; }

    [JsonProperty("type")]
    public byte ApiType { get; set; }

    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("x")]
    public short X { get; set; }

    [JsonProperty("y")]
    public short Y { get; set; }

    public TownForAnonymous(Town town)
    {
      this.Id = town.Id;
      this.ApiType = town.ApiType;
      this.CountryId = town.CountryId;
      this.Name = town.Name;
      this.X = town.X;
      this.Y = town.Y;
    }
  }
}
