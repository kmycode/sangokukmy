using System;
using Newtonsoft.Json;
using System.Collections.Generic;
using SangokuKmy.Models.Data.Entities;
namespace SangokuKmy.Models.Data.ApiEntities
{
  public class CountryForAnonymous
  {
    [JsonProperty("id")]
    public uint Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("colorId")]
    public int CountryColorId { get; set; }

    [JsonProperty("capitalTownId")]
    public uint CapitalTownId { get; set; }

    [JsonProperty("hasOverthrown")]
    public bool HasOverthrown { get; set; }

    [JsonProperty("overthrownGameDate")]
    public GameDateTime OverthrownGameDate { get; set; }

    [JsonProperty("posts")]
    public IEnumerable<CountryPost> Posts { get; set; }

    public CountryForAnonymous(Country country)
    {
      this.Id = country.Id;
      this.Name = country.Name;
      this.CountryColorId = country.CountryColorId;
      this.CapitalTownId = country.CapitalTownId;
      this.HasOverthrown = country.HasOverthrown;
      this.OverthrownGameDate = country.OverthrownGameDate;
    }
  }
}
