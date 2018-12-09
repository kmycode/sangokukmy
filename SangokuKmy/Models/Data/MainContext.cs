using Microsoft.EntityFrameworkCore;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data
{
  /// <summary>
  /// メインコンテキスト
  /// </summary>
  public class MainContext : DbContext
  {
    /// <summary>
    /// システムデータ
    /// </summary>
    public DbSet<SystemData> SystemData { get; set; }

    /// <summary>
    /// 認証データ
    /// </summary>
    public DbSet<AuthenticationData> AuthenticationData { get; set; }

    /// <summary>
    /// デフォルトのアイコンデータ
    /// </summary>
    public DbSet<DefaultIconData> DefaultIconData { get; set; }

    /// <summary>
    /// 武将データ
    /// </summary>
    public DbSet<Character> Characters { get; set; }

    /// <summary>
    /// 武将のアイコンデータ
    /// </summary>
    public DbSet<CharacterIcon> CharacterIcons { get; set; }

    /// <summary>
    /// 武将のコマンド
    /// </summary>
    public DbSet<CharacterCommand> CharacterCommands { get; set; }

    /// <summary>
    /// 武将のログ
    /// </summary>
    public DbSet<CharacterLog> CharacterLogs { get; set; }

    /// <summary>
    /// 武将の更新ログ
    /// </summary>
    public DbSet<CharacterUpdateLog> CharacterUpdateLogs { get; set; }

    /// <summary>
    /// 国データ
    /// </summary>
    public DbSet<Country> Countries { get; set; }

    /// <summary>
    /// 国のメッセージ一覧
    /// </summary>
    public DbSet<CountryMessage> CountryMessages { get; set; }

    /// <summary>
    /// 国の役職一覧
    /// </summary>
    public DbSet<CountryPost> CountryPosts { get; set; }

    /// <summary>
    /// マップログ
    /// </summary>
    public DbSet<MapLog> MapLogs { get; set; }

    /// <summary>
    /// チャットのメッセージ
    /// </summary>
    public DbSet<ChatMessage> ChatMessages { get; set; }

    /// <summary>
    /// 都市
    /// </summary>
    public DbSet<Town> Towns { get; set; }

    /// <summary>
    /// 都市の守備武将
    /// </summary>
    public DbSet<TownDefender> TownDefenders { get; set; }

    /// <summary>
    /// 部隊
    /// </summary>
    public DbSet<Unit> Units { get; set; }

    /// <summary>
    /// 部隊のメンバー
    /// </summary>
    public DbSet<UnitMember> UnitMembers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (!string.IsNullOrEmpty(Config.Database.MySqlConnectionString))
      {
        optionsBuilder.UseMySql(Config.Database.MySqlConnectionString);
      }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      // マスタデータの登録
      modelBuilder.Entity<DefaultIconData>().HasData(
        Enumerable
          .Range(0, 99)
          .Select(num => new DefaultIconData { Id = (uint)(num + 1), FileName = num + ".gif", })
          .ToArray());
    }
  }
}
