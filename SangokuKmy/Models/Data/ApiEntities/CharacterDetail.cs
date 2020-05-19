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

    [JsonProperty("formation")]
    public Formation Formation { get; set; }

    [JsonProperty("money")]
    public int Money { get; set; }

    [JsonProperty("rice")]
    public int Rice { get; set; }

    [JsonProperty("isStopCommand")]
    public bool IsStopCommand { get; set; }

    public CharacterDetail(Character chara)
    {
      this.Id = chara.Id;
      this.Message = chara.Message;
    }

    public CharacterDetail(Character chara, IEnumerable<CharacterSkill> skills, Formation formation, bool isStopCommand)
    {
      this.Skills = skills;
      this.Id = chara.Id;
      this.Message = chara.Message;
      this.Money = chara.Money;
      this.Rice = chara.Rice;
      this.Formation = formation;
      this.IsStopCommand = isStopCommand;
    }
  }
}
