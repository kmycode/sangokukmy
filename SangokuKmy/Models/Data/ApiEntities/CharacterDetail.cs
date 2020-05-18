using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Data.ApiEntities
{
  public class CharacterDetail
  {
    [JsonProperty("id")]
    public uint Id { get; set; }

    [JsonProperty("skills")]
    public IEnumerable<CharacterSkill> Skills { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    public CharacterDetail(Character chara, IEnumerable<CharacterSkill> skills)
    {
      this.Skills = skills;
      this.Id = chara.Id;
      this.Message = chara.Message;
    }
  }
}
