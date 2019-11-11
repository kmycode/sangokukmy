using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("character_caches")]
  public class LogCharacterCache
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 武将のID
    /// </summary>
    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    /// <summary>
    /// 当時メインだったアイコンのID
    /// </summary>
    [Column("icon_id")]
    [JsonProperty("iconId")]
    public uint IconId { get; set; }

    [NotMapped]
    [JsonProperty("mainIcon")]
    public CharacterIcon MainIcon { get; set; }

    /// <summary>
    /// 名前
    /// </summary>
    [Column("name", TypeName = "varchar(64)")]
    [JsonProperty("name")]
    public string Name { get; set; }

    /// <summary>
    /// 国のID
    /// </summary>
    [Column("country_id")]
    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    /// <summary>
    /// 武力
    /// </summary>
    [Column("strong")]
    [JsonProperty("strong")]
    public short Strong { get; set; }

    /// <summary>
    /// 知力
    /// </summary>
    [Column("intellect")]
    [JsonProperty("intellect")]
    public short Intellect { get; set; }

    /// <summary>
    /// 統率力
    /// </summary>
    [Column("leadership")]
    [JsonProperty("leadership")]
    public short Leadership { get; set; }

    /// <summary>
    /// 人望
    /// </summary>
    [Column("popularity")]
    [JsonProperty("popularity")]
    public short Popularity { get; set; }

    /// <summary>
    /// 兵士の種類
    /// </summary>
    [Column("soldier_type")]
    [JsonIgnore]
    public SoldierType SoldierType { get; set; }

    /// <summary>
    /// 兵士の種類（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("soldierType")]
    public short ApiSoldierType
    {
      get => (short)this.SoldierType;
      set => this.SoldierType = (SoldierType)value;
    }

    [Column("character_soldier_type_id")]
    [JsonProperty("characterSoldierTypeId")]
    public uint CharacterSoldierTypeId { get; set; }

    [Column("formation_type")]
    [JsonIgnore]
    public FormationType FormationType { get; set; }

    [NotMapped]
    [JsonProperty("formationType")]
    public short ApiFormationType
    {
      get => (short)this.FormationType;
      set => this.FormationType = (FormationType)value;
    }

    [Column("formation_level")]
    [JsonProperty("formationLevel")]
    public short FormationLevel { get; set; }

    /// <summary>
    /// 出身
    /// </summary>
    [Column("from")]
    [JsonIgnore]
    public CharacterFrom From { get; set; }

    /// <summary>
    /// 出身（API用）
    /// </summary>
    [NotMapped]
    [JsonProperty("from")]
    public short ApiFrom
    {
      get => (short)this.From;
      set => this.From = (CharacterFrom)value;
    }

    /// <summary>
    /// 兵士数
    /// </summary>
    [Column("soldier_number")]
    [JsonProperty("soldierNumber")]
    public int SoldierNumber { get; set; }

    /// <summary>
    /// 訓練値
    /// </summary>
    [Column("proficiency")]
    [JsonProperty("proficiency")]
    public short Proficiency { get; set; }
  }

  public static class LogCharacterCacheExtensions
  {
    public static LogCharacterCache ToLogCache(this Character c, CharacterIcon icon, Formation formation)
    {
      return new LogCharacterCache
      {
        CharacterId = c.Id,
        CountryId = c.CountryId,
        Name = c.Name,
        Strong = c.Strong,
        Intellect = c.Intellect,
        Leadership = c.Leadership,
        Popularity = c.Popularity,
        Proficiency = c.Proficiency,
        SoldierType = c.SoldierType,
        CharacterSoldierTypeId = c.CharacterSoldierTypeId,
        SoldierNumber = c.SoldierNumber,
        FormationType = c.FormationType,
        FormationLevel = formation.Level,
        IconId = icon.Id,
        From = c.From,
      };
    }
  }
}
