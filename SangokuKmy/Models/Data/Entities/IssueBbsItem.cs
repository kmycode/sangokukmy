using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("issue_bbs_items")]
  public class IssueBbsItem
  {
    [Key]
    [Column("id")]
    [JsonProperty("id")]
    public uint Id { get; set; }

    /// <summary>
    /// 親記事ID
    /// </summary>
    [Column("parent_id")]
    [JsonProperty("parentId")]
    public uint ParentId { get; set; }

    /// <summary>
    /// 書き込み者のアカウントID
    /// </summary>
    [Column("account_id")]
    [JsonProperty("accountId")]
    public uint AccountId { get; set; }

    /// <summary>
    /// 書き込み者のアカウント名
    /// </summary>
    [NotMapped]
    [JsonProperty("accountName")]
    public string AccountName { get; set; }

    /// <summary>
    /// 記事のタイトル
    /// </summary>
    [Column("title", TypeName = "varchar(120)")]
    [JsonProperty("title")]
    public string Title { get; set; }

    /// <summary>
    /// テキスト
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
    /// 最終更新
    /// </summary>
    [Column("last_modified")]
    [JsonIgnore]
    public DateTime LastModified { get; set; }

    /// <summary>
    /// 最終更新
    /// </summary>
    [NotMapped]
    [JsonProperty("lastModified")]
    public ApiDateTime ApiLastModified
    {
      get => ApiDateTime.FromDateTime(this.LastModified);
      set => this.LastModified = value.ToDateTime();
    }

    /// <summary>
    /// 状態
    /// </summary>
    [Column("status")]
    [JsonIgnore]
    public IssueStatus Status { get; set; }

    [NotMapped]
    [JsonProperty("status")]
    public short ApiStatus
    {
      get => (short)this.Status;
      set => this.Status = (IssueStatus)value;
    }

    /// <summary>
    /// 重要度
    /// </summary>
    [Column("priority")]
    [JsonIgnore]
    public IssuePriority Priority { get; set; }

    [NotMapped]
    [JsonProperty("priority")]
    public short ApiPriority
    {
      get => (short)this.Priority;
      set => this.Priority = (IssuePriority)value;
    }

    /// <summary>
    /// 分類
    /// </summary>
    [Column("category")]
    [JsonIgnore]
    public IssueCategory Category { get; set; }

    [NotMapped]
    [JsonProperty("category")]
    public short ApiCategory
    {
      get => (short)this.Category;
      set => this.Category = (IssueCategory)value;
    }
  }

  public enum IssueStatus : short
  {
    Undefined = 0,
    New = 1,
    Discussing = 2,
    InReady = 3,
    Waiting = 4,
    Processing = 5,
    Completed = 6,
    Rejected = 7,
    Duplicate = 8,
    Composite = 9,
    Invalid = 10,
    Wontfix = 11,
    Pending = 12,
  }

  public enum IssuePriority : short
  {
    Undefined = 0,
    New = 1,
    Low = 2,
    Normal = 3,
    High = 4,
  }

  public enum IssueCategory : short
  {
    Undefined = 0,
    New = 1,
    Enhancement = 2,
    Bug = 3,
    Rule = 4,
    Other = 5,
  }
}
