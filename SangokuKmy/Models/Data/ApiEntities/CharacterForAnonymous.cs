﻿using Newtonsoft.Json;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;

namespace SangokuKmy.Models.Data.ApiEntities
{
  /// <summary>
  /// 本人以外でもだれでも見れる武将データ
  /// </summary>
  public class CharacterForAnonymous
  {
    [JsonProperty("id")]
    public uint Id { get; set; }

    [JsonProperty("aliasId")]
    public string AliasId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    [JsonProperty("mainIcon")]
    public CharacterIcon MainIcon { get; set; }

    [JsonProperty("aiType")]
    public short ApiAiType { get; set; }

    [JsonProperty("strong")]
    public int Strong { get; set; }

    [JsonProperty("intellect")]
    public int Intellect { get; set; }

    [JsonProperty("leadership")]
    public int Leadership { get; set; }

    [JsonProperty("popularity")]
    public int Popularity { get; set; }

    [JsonProperty("soldierNumber")]
    public int? SoldierNumber { get; set; }

    [JsonProperty("soldierType")]
    public int? SoldierType { get; set; }

    [JsonProperty("deleteTurn")]
    public int DeleteTurn { get; set; }

    [JsonProperty("classValue")]
    public int Class { get; set; }

    [JsonProperty("reinforcement")]
    public Reinforcement Reinforcement { get; set; }

    [JsonIgnore]
    public DateTime LastUpdated { get; set; }

    [JsonProperty("lastUpdated")]
    public ApiDateTime ApiLastUpdated
    {
      get => ApiDateTime.FromDateTime(this.LastUpdated);
      set => this.LastUpdated = value.ToDateTime();
    }

    [JsonProperty("commands")]
    public IEnumerable<CharacterCommand> Commands { get; set; }

    public CharacterForAnonymous(Character character, CharacterIcon mainIcon, CharacterShareLevel level)
      : this(character, mainIcon, null, null, level)
    {
    }


    public CharacterForAnonymous(Character character, CharacterIcon mainIcon, IReadOnlyList<CharacterCommand> commands, CharacterShareLevel level)
      : this(character, mainIcon, null, commands, level)
    {
    }
    public CharacterForAnonymous(Character character, CharacterIcon mainIcon, Reinforcement reinforcement, IReadOnlyList<CharacterCommand> commands, CharacterShareLevel level)
    {
      this.Id = character.Id;
      if (character.AiType == CharacterAiType.SecretaryDefender)
      {
        this.AliasId = character.AliasId;
      }
      this.Name = character.Name;
      this.MainIcon = mainIcon;
      this.ApiAiType = character.ApiAiType;
      this.CountryId = character.CountryId;
      this.Strong = character.Strong;
      this.Intellect = character.Intellect;
      this.Leadership = character.Leadership;
      this.Popularity = character.Popularity;
      this.Reinforcement = reinforcement;
      if (level == CharacterShareLevel.SameTown || level == CharacterShareLevel.SameTownAndSameCountry)
      {
        this.SoldierNumber = character.SoldierNumber;
        this.SoldierType = character.ApiSoldierType;
      }
      if (level == CharacterShareLevel.SameTownAndSameCountry || level == CharacterShareLevel.SameCountry)
      {
        this.LastUpdated = character.LastUpdated;
        if (commands != null)
        {
          var cmds = new List<CharacterCommand>();
          for (var i = character.IntLastUpdatedGameDate + 1; i <= character.IntLastUpdatedGameDate + 4; i++)
          {
            cmds.Add(commands.FirstOrDefault(c => c.IntGameDateTime == i) ?? new CharacterCommand
            {
              CharacterId = character.Id,
              IntGameDateTime = i,
              Type = CharacterCommandType.None,
            });
          }
          this.Commands = cmds;
        }
      }
      if (level == CharacterShareLevel.AllCharacterList)
      {
        this.DeleteTurn = character.DeleteTurn;
        this.Class = character.Class;
      }
    }
  }

  /// <summary>
  /// 共有するパラメータの程度
  /// </summary>
  public enum CharacterShareLevel : short
  {
    /// <summary>
    /// 無条件でだれでも見れる範囲内
    /// </summary>
    Anonymous,

    /// <summary>
    /// 同じ都市に滞在する武将の範囲内
    /// </summary>
    SameTown,

    /// <summary>
    /// 同じ都市に滞在し、かつ同じ国に仕官している
    /// </summary>
    SameTownAndSameCountry,

    /// <summary>
    /// 同じ国である
    /// </summary>
    SameCountry,

    /// <summary>
    /// 登録武将一覧または名将一覧
    /// </summary>
    AllCharacterList,
  }
}
