using System;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Commands
{
  public abstract class Command
  {
    /// <summary>
    /// コマンドのID
    /// </summary>
    public abstract CharacterCommandType Type { get; }

    /// <summary>
    /// コマンドを入力する。例外を出すこともある
    /// </summary>
    /// <param name="repo">リポジトリ</param>
    /// <param name="characterId">武将ID</param>
    /// <param name="gameDates">入力する年月</param>
    /// <param name="options">コマンドパラメータ</param>
    public abstract Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options);

    /// <summary>
    /// コマンドを実行する。例外は出さない
    /// </summary>
    /// <param name="repo">リポジトリ</param>
    /// <param name="character">武将</param>
    /// <param name="options">コマンドパラメータ</param>
    /// <param name="game">ゲームオブジェクト</param>
    public abstract Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game);
  }
}

public class CommandSystemData
{
  public Func<string, Task> CharacterLogAsync { get; set; }

  public Func<uint, string, Task> CharacterLogByIdAsync { get; set; }

  public Func<EventType, string, bool, Task> MapLogAsync { get; set; }

  public GameDateTime GameDateTime { get; set; }
}
