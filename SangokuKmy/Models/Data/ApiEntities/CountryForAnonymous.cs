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

    [JsonIgnore]
    public CountryAiType AiType { get; set; }

    [JsonProperty("aiType")]
    public short ApiAiType
    {
      get => (short)this.AiType;
      set => this.AiType = (CountryAiType)value;
    }

    [JsonProperty("isHaveGyokuji")]
    public bool IsHaveGyokuji { get; set; }

    [JsonProperty("gyokujiGameDate")]
    public GameDateTime GyokujiGameDate { get; set; }

    [JsonProperty("religion")]
    public short ApiReligion { get; set; }

    [JsonProperty("townSubBuildingExtraSpace")]
    public short TownSubBuildingExtraSpace { get; set; }

    public CountryForAnonymous(Country country)
    {
      this.Id = country.Id;
      this.Name = country.Name;
      this.CountryColorId = country.CountryColorId;
      this.CapitalTownId = country.CapitalTownId;
      this.HasOverthrown = country.HasOverthrown;
      this.OverthrownGameDate = country.OverthrownGameDate;
      this.AiType = country.AiType;
      this.IsHaveGyokuji = country.IsHaveGyokuji;
      this.GyokujiGameDate = country.GyokujiGameDate;
      this.ApiReligion = country.ApiReligion;
      this.TownSubBuildingExtraSpace = country.TownSubBuildingExtraSpace;
    }
  }
}
