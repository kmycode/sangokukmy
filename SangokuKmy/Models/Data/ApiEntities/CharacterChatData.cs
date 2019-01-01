using Newtonsoft.Json;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.ApiEntities
{
  public class CharacterChatData
  {
    /// <summary>
    /// 武将ID
    /// </summary>
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 名前
    /// </summary>
    [JsonProperty("name")]
    public string Name { get; set; }

    public CharacterChatData(Character character)
    {
      this.Id = character.Id;
      this.Name = character.Name;
    }
  }
}
