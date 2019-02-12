using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using System.Collections.Generic;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("thread_bbs_item")]
  public class ThreadBbsItem
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 掲示板の種類
    /// </summary>
    [Column("type")]
    [JsonIgnore]
    public BbsType Type { get; set; }

    /// <summary>
    /// 掲示板の種類（API出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("type")]
    public short ApiType
    {
      get => (short)this.Type;
      set => this.Type = (BbsType)value;
    }

    /// <summary>
    /// 親アイテムのID。ゼロなら、最上位のアイテム（スレッド）
    /// </summary>
    [Column("parent_id")]
    [JsonProperty("parentId")]
    public uint ParentId { get; set; }

    /// <summary>
    /// 子の書き込み一覧（API用）
    /// </summary>
    [NotMapped]
    [JsonProperty("children")]
    public IEnumerable<ThreadBbsItem> Children { get; set; }

    /// <summary>
    /// 書き込み者の国ID
    /// </summary>
    [Column("country_id")]
    [JsonProperty("countryId")]
    public uint CountryId { get; set; }

    /// <summary>
    /// 書き込み者のID
    /// </summary>
    [Column("character_id")]
    [JsonProperty("characterId")]
    public uint CharacterId { get; set; }

    [NotMapped]
    [JsonProperty("character")]
    public CharacterForAnonymous Character { get; set; }

    /// <summary>
    /// 使用する武将アイコンのID
    /// </summary>
    [Column("character_icon_id")]
    [JsonProperty("characterIconId")]
    public uint CharacterIconId { get; set; }

    /// <summary>
    /// 使用する武将アイコン
    /// </summary>
    [NotMapped]
    [JsonProperty("characterIcon")]
    public CharacterIcon CharacterIcon { get; set; }

    /// <summary>
    /// タイトル
    /// </summary>
    [Column("title")]
    [JsonProperty("title")]
    public string Title { get; set; }

    /// <summary>
    /// 文章
    /// </summary>
    [Column("text")]
    [JsonProperty("text")]
    public string Text { get; set; }

    /// <summary>
    /// 書き込み日付
    /// </summary>
    [Column("written")]
    [JsonIgnore]
    public DateTime Written { get; set; }

    /// <summary>
    /// 書き込み日付（JSON出力用）
    /// </summary>
    [NotMapped]
    [JsonProperty("written")]
    public ApiDateTime ApiWritten
    {
      get => ApiDateTime.FromDateTime(this.Written);
      set => this.Written = value.ToDateTime();
    }

    /// <summary>
    /// ストリーミングで配信されたとき、この書き込みを削除するか
    /// </summary>
    [NotMapped]
    [JsonProperty("isRemove")]
    public bool IsRemove { get; set; }
  }

  public enum BbsType : short
  {
    /// <summary>
    /// 会議室
    /// </summary>
    CountryBbs = 1,
  }
}
