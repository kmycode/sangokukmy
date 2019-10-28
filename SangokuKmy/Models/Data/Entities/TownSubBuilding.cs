using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Newtonsoft.Json;
using SangokuKmy.Common;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("town_sub_buildings")]
  public class TownSubBuilding
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("town_id")]
    [JsonProperty("townId")]
    public uint TownId { get; set; }

    [Column("character_id")]
    [JsonIgnore]
    public uint CharacterId { get; set; }

    [Column("status")]
    [JsonIgnore]
    public TownSubBuildingStatus Status { get; set; }

    [NotMapped]
    [JsonProperty("status")]
    public short ApiStatus
    {
      get => (short)this.Status;
      set => this.Status = (TownSubBuildingStatus)value;
    }

    [Column("status_finish_game_date_time")]
    [JsonIgnore]
    public int IntStatusFinishGameDateTime { get; set; }

    [NotMapped]
    [JsonProperty("statusFinishGameDateTime")]
    public GameDateTime StatusFinishGameDateTime
    {
      get => GameDateTime.FromInt(this.IntStatusFinishGameDateTime);
      set => this.IntStatusFinishGameDateTime = value.ToInt();
    }

    [Column("type")]
    [JsonIgnore]
    public TownSubBuildingType Type { get; set; }

    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (TownSubBuildingType)value;
    }
  }

  public enum TownSubBuildingStatus : short
  {
    Unknown = 0,
    Available = 1,
    UnderConstruction = 2,
    Removing = 3,
  }

  public enum TownSubBuildingType : short
  {
    None = 0,

    /// <summary>
    /// 農地
    /// </summary>
    Farmland = 1,
  }

  public class TownSubBuildingTypeInfo
  {
    public TownSubBuildingType Type { get; set; }

    public string Name { get; set; }

    public short Size { get; set; }

    public int Money { get; set; }

    public bool CanBuildMultiple { get; set; }

    public int BuildDuring { get; set; } = 24;

    public Action<Town> OnBuilt { get; set; }

    public Action<Town> OnRemoved { get; set; }
  }

  public static class TownSubBuildingTypeInfoes
  {
    private static readonly List<TownSubBuildingTypeInfo> infos = new List<TownSubBuildingTypeInfo>
    {
      new TownSubBuildingTypeInfo
      {
        Type = TownSubBuildingType.Farmland,
        Name = "農地",
        Size = 1,
        Money = 10000,
        CanBuildMultiple = true,
        BuildDuring = 12,
        OnBuilt = t => t.AgricultureMax += 500,
        OnRemoved = t =>
        {
          t.AgricultureMax -= 500;
          t.Agriculture = Math.Min(t.Agriculture, t.AgricultureMax);
        },
      }
    };

    public static Optional<TownSubBuildingTypeInfo> Get(TownSubBuildingType type)
    {
      return infos.FirstOrDefault(i => i.Type == type).ToOptional();
    }

    public static IEnumerable<TownSubBuildingTypeInfo> GetInfoes(this IEnumerable<TownSubBuilding> buildings)
    {
      return buildings.Select(b => b.Type).GetInfoes();
    }

    public static IEnumerable<TownSubBuildingTypeInfo> GetInfoes(this IEnumerable<TownSubBuildingType> types)
    {
      return types.Join(infos, t => t, i => i.Type, (t, i) => i);
    }
  }
}
