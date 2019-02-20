using Newtonsoft.Json;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.ApiEntities
{
  public class CharacterOnline
  {
    [JsonIgnore]
    public OnlineStatus Status { get; set; }

    [JsonProperty("status")]
    public short ApiStatus
    {
      get => (short)this.Status;
      set => this.Status = (OnlineStatus)value;
    }

    [JsonProperty("character")]
    public CharacterData Character { get; set; }

    public class CharacterData
    {
      [JsonProperty("id")]
      public uint Id { get; set; }

      [JsonProperty("name")]
      public string Name { get; set; }

      [JsonProperty("mainIcon")]
      public CharacterIcon Icon { get; set; }

      [JsonProperty("countryId")]
      public uint CountryId { get; set; }
    }
  }

  public enum OnlineStatus : short
  {
    Offline = 0,
    Active = 1,
    Inactive = 2,
  }
}
