using Newtonsoft.Json;
using SangokuKmy.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("character_skills")]
  public class CharacterSkill
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    [Column("type")]
    [JsonIgnore]
    public CharacterSkillType Type { get; set; }

    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (CharacterSkillType)value;
    }

    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    [Column("status")]
    [JsonIgnore]
    public CharacterSkillStatus Status { get; set; }

    [NotMapped]
    [JsonProperty("status")]
    public short ApiStatus
    {
      get => (short)this.Status;
      set => this.Status = (CharacterSkillStatus)value;
    }
  }

  public enum CharacterSkillStatus : short
  {
    Undefined = 0,
    Available = 1,
  }

  public enum CharacterSkillType : short
  {
  }

  public enum CharacterSkillEffectType
  {
    Strong,
    Intellect,
    Leadership,
    Popularity,
    ItemMax,
    Command,
    RiceBuyMax,
  }

  public class CharacterSkillEffect
  {
    public CharacterSkillEffectType Type { get; set; }
    public int Value { get; set; }
  }

  public class CharacterSkillInfo
  {
    public CharacterSkillType Type { get; set; }
    public string Name { get; set; }
    public int RequestedPoint { get; set; }
    public Func<IEnumerable<CharacterSkill>, bool> SubjectAppear { get; set; }
    public IList<CharacterSkillEffect> Effects { get; set; }
  }

  public static class CharacterSkillInfoes
  {
    private static readonly IList<CharacterSkillInfo> infos = new List<CharacterSkillInfo>
    {
    };

    public static Optional<CharacterSkillInfo> Get(CharacterSkillType type)
    {
      return infos.FirstOrDefault(i => i.Type == type).ToOptional();
    }

    private static Optional<CharacterSkillInfo> GetInfo(this CharacterSkill item)
    {
      return infos.FirstOrDefault(i => i.Type == item.Type).ToOptional();
    }

    private static IEnumerable<CharacterSkillInfo> GetInfos(this IEnumerable<CharacterSkill> items)
    {
      return items.Join(infos, i => i.Type, i => i.Type, (it, inn) => inn);
    }

    public static bool AnySkillEffects(this IEnumerable<CharacterSkill> items, CharacterSkillEffectType type)
    {
      return items.GetInfos().SelectMany(i => i.Effects).Any(e => e.Type == type);
    }

    public static bool AnySkillEffects(this IEnumerable<CharacterSkill> items, CharacterSkillEffectType type, int value)
    {
      return items.GetInfos().SelectMany(i => i.Effects).Any(e => e.Type == type && e.Value == value);
    }

    public static int GetSumOfValues(this IEnumerable<CharacterSkill> items, CharacterSkillEffectType type)
    {
      var effects = items.GetInfos().SelectMany(i => i.Effects).Where(e => e.Type == type);
      return effects.Any() ? effects.Sum(e => e.Value) : 0;
    }

    public static int GetSumOfValues(this CharacterSkill item, CharacterSkillEffectType type)
    {
      var info = item.GetInfo();
      if (info.HasData && info.Data.Effects.Any(e => e.Type == type))
      {
        return info.Data.Effects.Where(e => e.Type == type).Sum(e => e.Value);
      }
      return 0;
    }
  }
}
