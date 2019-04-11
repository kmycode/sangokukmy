using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using SangokuKmy.Models.Data.Entities;
using System.Collections.Generic;
using System.Collections;
namespace SangokuKmy.Models.Data.ApiEntities
{
  public interface IApiData
  {
  }

  public class ApiData<T> : IApiData
  {
    /// <summary>
    /// 型の種類
    /// </summary>
    [JsonProperty("type")]
    public int Type { get; set; }

    [JsonProperty("isArray")]
    public bool IsArray => false;

    /// <summary>
    /// 実際のデータ
    /// </summary>
    [JsonProperty("data")]
    public T Data { get; set; }
  }

  public class ApiArrayData<T>
  {
    /// <summary>
    /// 型の種類
    /// </summary>
    [JsonProperty("type")]
    public int Type { get; set; }

    [JsonProperty("isArray")]
    public bool IsArray => true;

    /// <summary>
    /// 配列
    /// </summary>
    [JsonProperty("data")]
    public IEnumerable<T> Data { get; set; }
  }

  public static class ApiData
  {
    private static ApiData<DT> From<DT>(int type, DT data)
    {
      return new ApiData<DT>
      {
        Type = type,
        Data = data,
      };
    }

    private static ApiArrayData<DT> From<DT>(int type, IEnumerable<DT> data)
    {
      return new ApiArrayData<DT>
      {
        Type = type,
        Data = data,
      };
    }

    public static ApiData<ApiDateTime> From(ApiDateTime data) => From(1, data);
    public static ApiData<MapLog> From(MapLog data) => From(4, data);
    public static ApiData<AuthenticationData> From(AuthenticationData data) => From(6, data);
    public static ApiData<ApiError> From(ApiError data) => From(7, data);
    public static ApiData<Character> From(Character data) => From(9, data);
    public static ApiData<CharacterForAnonymous> From(CharacterForAnonymous data) => From(9, data);
    public static ApiArrayData<CharacterForAnonymous> From(IEnumerable<CharacterForAnonymous> data) => From(9, data);
    public static ApiData<Country> From(Country data) => From(10, data);
    public static ApiData<CountryForAnonymous> From(CountryForAnonymous data) => From(10, data);
    public static ApiArrayData<CountryForAnonymous> From(IEnumerable<CountryForAnonymous> data) => From(10, data);
    public static ApiData<TownBase> From(TownBase data) => From(11, data);
    public static ApiData<Town> From(Town data) => From(11, data);
    public static ApiData<TownForAnonymous> From(TownForAnonymous data) => From(11, data);
    public static ApiData<GameDateTime> From(GameDateTime data) => From(12, data);
    public static ApiData<CharacterLog> From(CharacterLog data) => From(13, data);
    public static ApiArrayData<CharacterLog> From(IEnumerable<CharacterLog> data) => From(13, data);
    public static ApiData<CharacterCommand> From(CharacterCommand data) => From(14, data);
    public static ApiArrayData<CharacterCommand> From(IEnumerable<CharacterCommand> data) => From(14, data);
    public static ApiData<ApiSignal> From(ApiSignal data) => From(15, data);
    public static ApiData<ScoutedTown> From(ScoutedTown data) => From(16, data);
    public static ApiData<ChatMessage> From(ChatMessage data) => From(17, data);
    public static ApiArrayData<ChatMessage> From(IEnumerable<ChatMessage> data) => From(17, data);
    public static ApiArrayData<CharacterIcon> From(IEnumerable<CharacterIcon> data) => From(18, data);
    public static ApiData<SystemData> From(SystemData data) => From(19, data);
    public static ApiData<CountryPost> From(CountryPost data) => From(20, data);
    public static ApiData<CountryAlliance> From(CountryAlliance data) => From(21, data);
    public static ApiData<CountryWar> From(CountryWar data) => From(22, data);
    public static ApiData<Unit> From(Unit data) => From(23, data);
    public static ApiData<UnitMember> From(UnitMember data) => From(24, data);
    public static ApiData<CharacterUpdateLog> From(CharacterUpdateLog data) => From(25, data);
    public static ApiData<ThreadBbsItem> From(ThreadBbsItem data) => From(26, data);
    public static ApiData<CharacterOnline> From(CharacterOnline data) => From(27, data);
    public static ApiData<Reinforcement> From(Reinforcement data) => From(28, data);
    public static ApiData<CountryMessage> From(CountryMessage data) => From(29, data);
    public static ApiData<TownWar> From(TownWar data) => From(30, data);
    public static ApiData<CharacterSoldierType> From(CharacterSoldierType data) => From(31, data);
    public static ApiData<CountryPolicy> From(CountryPolicy data) => From(32, data);
    public static ApiData<CountryScouter> From(CountryScouter data) => From(33, data);
  }
}
