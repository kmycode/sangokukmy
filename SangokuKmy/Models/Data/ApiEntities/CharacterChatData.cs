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

    /// <summary>
    /// 初心者フラグ
    /// </summary>
    [JsonProperty("isBeginner")]
    public bool IsBeginner { get; set; }

    /// <summary>
    /// 援軍情報
    /// </summary>
    [JsonProperty("reinforcement")]
    public Reinforcement Reinforcement { get; set; }

    public CharacterChatData(Character character, Reinforcement reinforcement)
    {
      this.Id = character.Id;
      this.Name = character.Name;
      this.IsBeginner = character.IsBeginner;
      this.Reinforcement = reinforcement;
    }
  }
}
