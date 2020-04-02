using Newtonsoft.Json;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using SangokuKmy.Models.Common;

namespace SangokuKmy.Models.Data.ApiEntities
{
  /// <summary>
  /// 本人以外でもだれでも見れる武将データ
  /// </summary>
  public class CharacterForAnonymous
  {
    [JsonProperty("id")]
    public uint Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    [JsonProperty("townId")]
    public uint TownId { get; set; }

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

    [JsonProperty("hasRemoved")]
    public bool HasRemoved { get; set; }

    [JsonProperty("money")]
    public int Money { get; set; }

    [JsonProperty("classValue")]
    public int Class { get; set; }

    [JsonProperty("reinforcement")]
    public Reinforcement Reinforcement { get; set; }

    [JsonProperty("from")]
    public CharacterFrom From { get; set; }

    [JsonIgnore]
    public DateTime LastUpdated { get; set; }

    [JsonProperty("lastUpdated")]
    public ApiDateTime ApiLastUpdated
    {
      get => ApiDateTime.FromDateTime(this.LastUpdated);
      set => this.LastUpdated = value.ToDateTime();
    }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("lastUpdatedGameDate")]
    public GameDateTime LastUpdatedGameDate
    {
      get => GameDateTime.FromInt(this.IntLastUpdatedGameDate);
      set => this.IntLastUpdatedGameDate = value.ToInt();
    }

    [JsonIgnore]
    public int IntLastUpdatedGameDate { get; set; }

    [JsonProperty("battleWonCount")]
    public int BattleWonCount { get; set; }

    [JsonProperty("battleLostCount")]
    public int BattleLostCount { get; set; }

    [JsonProperty("battleBrokeWallSize")]
    public int BattleBrokeWallSize { get; set; }

    [JsonProperty("battleDominateCount")]
    public int BattleDominateCount { get; set; }

    [JsonProperty("battleContinuousCount")]
    public int BattleContinuousCount { get; set; }

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
      this.Name = character.Name;
      this.MainIcon = mainIcon;
      this.ApiAiType = character.ApiAiType;
      this.CountryId = character.CountryId;
      this.Strong = character.Strong;
      this.Intellect = character.Intellect;
      this.Leadership = character.Leadership;
      this.Popularity = character.Popularity;
      this.Reinforcement = reinforcement;
      this.From = character.From;
      this.HasRemoved = character.HasRemoved;
      this.LastUpdated = character.LastUpdated;
      this.IntLastUpdatedGameDate = character.IntLastUpdatedGameDate;
      if (level == CharacterShareLevel.SameCountry || level == CharacterShareLevel.SameTown || level == CharacterShareLevel.SameTownAndSameCountry || level == CharacterShareLevel.SameCountryTownOtherCountry)
      {
        this.TownId = character.TownId;
      }
      if (level == CharacterShareLevel.SameTown || level == CharacterShareLevel.SameTownAndSameCountry || level == CharacterShareLevel.SameCountry)
      {
        this.SoldierNumber = character.SoldierNumber;
        this.SoldierType = character.ApiSoldierType;
      }
      if (level == CharacterShareLevel.SameTownAndSameCountry || level == CharacterShareLevel.SameCountry)
      {
        if (commands != null)
        {
          var cmds = new List<CharacterCommand>();
          var intStartMonth = character.LastUpdatedGameDate.Year >= Config.UpdateStartYear ? character.IntLastUpdatedGameDate + 1 : new GameDateTime { Year = Config.UpdateStartYear, Month = 1, }.ToInt();
          for (var i = intStartMonth; i < intStartMonth + 4; i++)
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
        this.Money = character.Money + character.Rice;
        this.Class = character.Class;
        this.Message = character.Message;
        this.BattleBrokeWallSize = character.BattleBrokeWallSize;
        this.BattleContinuousCount = character.BattleContinuousCount;
        this.BattleDominateCount = character.BattleDominateCount;
        this.BattleLostCount = character.BattleLostCount;
        this.BattleWonCount = character.BattleWonCount;
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
    /// 同じ国にいる他国の武将
    /// </summary>
    SameCountryTownOtherCountry,

    /// <summary>
    /// 登録武将一覧または名将一覧
    /// </summary>
    AllCharacterList,
  }
}
