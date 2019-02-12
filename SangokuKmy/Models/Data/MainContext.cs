using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Debug;
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
    /// システムのデバッグデータ
    /// </summary>
    public DbSet<SystemDebugData> SystemDebugData { get; set; }

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
    /// 武将のコマンドのパラメータ
    /// </summary>
    public DbSet<CharacterCommandParameter> CharacterCommandParameters { get; set; }

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
    /// 同盟
    /// </summary>
    public DbSet<CountryAlliance> CountryAlliances { get; set; }

    /// <summary>
    /// 戦争
    /// </summary>
    public DbSet<CountryWar> CountryWars { get; set; }

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
    /// 諜報された都市データ
    /// </summary>
    public DbSet<ScoutedTown> ScoutedTowns { get; set; }

    /// <summary>
    /// 諜報された都市の武将データ
    /// </summary>
    public DbSet<ScoutedCharacter> ScoutedCharacters { get; set; }

    /// <summary>
    /// 諜報された都市の守備データ
    /// </summary>
    public DbSet<ScoutedDefender> ScoutedDefenders { get; set; }

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

    /// <summary>
    /// ログに使う武将のキャッシュ
    /// </summary>
    public DbSet<LogCharacterCache> CharacterCaches { get; set; }

    /// <summary>
    /// 戦闘ログ
    /// </summary>
    public DbSet<BattleLog> BattleLogs { get; set; }

    /// <summary>
    /// 戦闘ログの一行
    /// </summary>
    public DbSet<BattleLogLine> BattleLogLines { get; set; }

    /// <summary>
    /// スレッド型の掲示板アイテム
    /// </summary>
    public DbSet<ThreadBbsItem> ThreadBbsItems { get; set; }

    private static readonly LoggerFactory LoggerFactory = new LoggerFactory(new[] {
        new DebugLoggerProvider((category, level)
            => category == DbLoggerCategory.Database.Command.Name && level == LogLevel.Information)
    });

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
