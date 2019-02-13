using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.ApiEntities
{
  public class EntryExtraData
  {
    [JsonProperty("attributeMax")]
    public int AttributeMax { get; set; }

    [JsonProperty("attributeSumMax")]
    public int AttributeSumMax { get; set; }

    [JsonProperty("countryData")]
    public IEnumerable<CountryExtraData> CountryData { get; set; }

    public class CountryExtraData
    {
      [JsonProperty("countryId")]
      public uint CountryId { get; set; }

      [JsonProperty("isJoinLimited")]
      public bool IsJoinLimited { get; set; }
    }
  }
}
