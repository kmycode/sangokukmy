using SangokuKmy.Models.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SangokuKmy.Models.Common.Definitions;
using Nito.AsyncEx;
using System.Transactions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.ComponentModel.DataAnnotations.Schema;

namespace SangokuKmy.Models.Data
{
  /// <summary>
  /// メインとなるリポジトリ
  /// </summary>
  public class MainRepository : IDisposable
  {
    /// <summary>
    /// データベースへアクセスするコンテキスト。アクセスが必要になった場合にのみ生成する
    /// </summary>
    private MainContext Context
    {
      get
      {
        try
        {
          return this._context = this._context ?? new MainContext();
        }
        catch (Exception ex)
        {
          ErrorCode.DatabaseError.Throw(ex);
          return null;
        }
      }
    }
    private MainContext _context;

    /// <summary>
    /// システム情報
    /// </summary>
    public SystemRepository System => this._system = this._system ?? new SystemRepository(this.container);
    private SystemRepository _system;

    /// <summary>
    /// 認証データ
    /// </summary>
    public AuthenticationDataRepository AuthenticationData => this._auth = this._auth ?? new AuthenticationDataRepository(this.container);
    private AuthenticationDataRepository _auth;

    /// <summary>
    /// 武将
    /// </summary>
    public CharacterRepository Character => this._chara = this._chara ?? new CharacterRepository(this.container);
    private CharacterRepository _chara;

    /// <summary>
    /// 武将登録時のホスト
    /// </summary>
    public EntryHostRepository EntryHost => this._entryHost = this._entryHost ?? new EntryHostRepository(this.container);
    private EntryHostRepository _entryHost;

    /// <summary>
    /// 武将コマンド
    /// </summary>
    public CharacterCommandRepository CharacterCommand => this._charaCommand = this._charaCommand ?? new CharacterCommandRepository(this.container);
    private CharacterCommandRepository _charaCommand;

    /// <summary>
    /// 武将兵種
    /// </summary>
    public CharacterSoldierTypeRepository CharacterSoldierType => this._charaSoldierType = this._charaSoldierType ?? new CharacterSoldierTypeRepository(this.container);
    private CharacterSoldierTypeRepository _charaSoldierType;

    /// <summary>
    /// マップログ
    /// </summary>
    public MapLogRepository MapLog => this._maplog = this._maplog ?? new MapLogRepository(this.container);
    private MapLogRepository _maplog;

    /// <summary>
    /// 国
    /// </summary>
    public CountryRepository Country => this._country = this._country ?? new CountryRepository(this.container);
    private CountryRepository _country;

    /// <summary>
    /// AI国
    /// </summary>
    public AiCountryRepository AiCountry => this._aiCountry = this._aiCountry ?? new AiCountryRepository(this.container);
    private AiCountryRepository _aiCountry;

    /// <summary>
    /// AI国
    /// </summary>
    public AiActionHistoryRepository AiActionHistory => this._aiActionHistory = this._aiActionHistory ?? new AiActionHistoryRepository(this.container);
    private AiActionHistoryRepository _aiActionHistory;

    /// <summary>
    /// 外交
    /// </summary>
    public CountryDiplomaciesRepository CountryDiplomacies => this._countryDiplomacies = this._countryDiplomacies ?? new CountryDiplomaciesRepository(this.container);
    private CountryDiplomaciesRepository _countryDiplomacies;

    /// <summary>
    /// 都市
    /// </summary>
    public TownRepository Town => this._town = this._town ?? new TownRepository(this.container);
    private TownRepository _town;

    /// <summary>
    /// 諜報された都市
    /// </summary>
    public ScoutedTownRepository ScoutedTown => this._scoutedTown = this._scoutedTown ?? new ScoutedTownRepository(this.container);
    private ScoutedTownRepository _scoutedTown;

    /// <summary>
    /// 手紙
    /// </summary>
    public ChatMessageRepository ChatMessage => this._chatMessage = this._chatMessage ?? new ChatMessageRepository(this.container);
    private ChatMessageRepository _chatMessage;

    /// <summary>
    /// 部隊
    /// </summary>
    public UnitRepository Unit => this._unit = this._unit ?? new UnitRepository(this.container);
    private UnitRepository _unit;

    /// <summary>
    /// 戦闘ログ
    /// </summary>
    public BattleLogRepository BattleLog => this._battleLog = this._battleLog ?? new BattleLogRepository(this.container);
    private BattleLogRepository _battleLog;

    /// <summary>
    /// スレッドフロート型の掲示板
    /// </summary>
    public ThreadBbsRepository ThreadBbs => this._threadBbs = this._threadBbs ?? new ThreadBbsRepository(this.container);
    private ThreadBbsRepository _threadBbs;

    /// <summary>
    /// 招待コード
    /// </summary>
    public InvitationCodeRepository InvitationCode => this._invitationCode = this._invitationCode ?? new InvitationCodeRepository(this.container);
    private InvitationCodeRepository _invitationCode;

    /// <summary>
    /// 統一記録
    /// </summary>
    public HistoryRepository History => this._history = this._history ?? new HistoryRepository(this.container);
    private HistoryRepository _history;

    /// <summary>
    /// 援軍
    /// </summary>
    public ReinforcementRepository Reinforcement => this._reinforcement = this._reinforcement ?? new ReinforcementRepository(this.container);
    private ReinforcementRepository _reinforcement;

    /// <summary>
    /// 読み込みロックをかけた状態のリポジトリを入手する
    /// </summary>
    /// <returns>リポジトリ</returns>
    public static MainRepository WithRead()
    {
      var repo = new MainRepository();
      repo.ReadLock();
      return repo;
    }

    /// <summary>
    /// 書き込みロックをかけた状態のリポジトリを入手する
    /// </summary>
    /// <returns>リポジトリ</returns>
    public static MainRepository WithReadAndWrite()
    {
      var repo = new MainRepository();
      repo.WriteLock();
      return repo;
    }

    private readonly IRepositoryContainer container;
    private IDbContextTransaction transaction;
    private bool isWriteMode = false;
    private bool isError = false;

    private MainRepository()
    {
      this.container = new Container(this);
    }

    public void Dispose()
    {
      if (this.isWriteMode)
      {
        if (!this.isError)
        {
          this.transaction?.Commit();
        }
        else
        {
          this.transaction?.Rollback();
        }
      }
      this.transaction?.Dispose();
      this.Context?.Dispose();
    }

    /// <summary>
    /// 変更をDBに保存する
    /// </summary>
    public async Task SaveChangesAsync() => await this.Context.SaveChangesAsync();

    /// <summary>
    /// 読み込み限定でロックをかける
    /// </summary>
    /// <returns>ロック解除オブジェクト</returns>
    private void ReadLock()
    {
      this.WriteLock();
      this.isWriteMode = false;
    }

    /// <summary>
    /// 読み込みと書き込みが可能なロックをかける
    /// </summary>
    /// <returns>ロック解除オブジェクト</returns>
    private void WriteLock()
    {
      try
      {
        this.transaction = this.Context.Database.BeginTransaction();
        this.isWriteMode = true;
      }
      catch (Exception ex)
      {
        ErrorCode.LockFailedError.Throw(ex);
      }
    }

    public void Error(Exception ex)
    {
      this.isError = true;
      ErrorCode.DatabaseError.Throw(ex);

      // usingを使っているのであれば、このあとDisposeメソッドが呼び出される
    }

    private class Container : IRepositoryContainer
    {
      private readonly MainRepository repo;

      public MainContext Context => this.repo.Context;

      public Container(MainRepository repo)
      {
        this.repo = repo;
      }

      public void Error(Exception ex) => this.repo.Error(ex);

      public async Task RemoveAllRowsAsync(Type type)
      {
        var attribute = type.GetCustomAttributes(typeof(TableAttribute), true).Cast<TableAttribute>().FirstOrDefault();
        if (attribute != null)
        {
          var tableName = attribute.Name;

          // TRUNCATEはAUTO_INCREMENTがリセットされるのであんま使いたくない
          // FormattableStringにしたらなぜかエラーになるので、通常の文字列連結で使う
#pragma warning disable EF1000 // #warning ディレクティブ
          await this.Context.Database.ExecuteSqlCommandAsync("DELETE FROM `" + tableName + "`");
#pragma warning restore EF1000 // #warning ディレクティブ
        }
        else
        {
          throw new InvalidOperationException();
        }
      }
    }
  }

  /// <summary>
  /// リポジトリのコンテナ。リポジトリクラスをnewするときに渡すデータ
  /// </summary>
  public interface IRepositoryContainer
  {
    /// <summary>
    /// データベースへ直接アクセスするコンテキスト
    /// </summary>
    MainContext Context { get; }

    void Error(Exception ex);

    Task RemoveAllRowsAsync(Type type);
  }
}
