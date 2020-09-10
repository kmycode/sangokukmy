using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualBasic.CompilerServices;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Common.Definitions
{
  /// <summary>
  /// アクセストークンのスコープ
  /// </summary>
  [Flags]
  public enum Scope : uint
  {
    All = 0b111_1111_1111_1111_1111_1111_1111_1111,
  }

  /// <summary>
  /// エラーコード
  /// </summary>
  public readonly struct ErrorCode
  {
    /// <summary>
    /// サーバへの接続に失敗したエラー（クライアント側でのみ使用する）
    /// </summary>
    public static ErrorCode ServerConnectionFailedError { get; } = new ErrorCode(500, -1);

    /// <summary>
    /// 原因不明の内部エラー
    /// </summary>
    public static ErrorCode InternalError { get; } = new ErrorCode(500, 1);

    /// <summary>
    /// データベース接続のエラー
    /// </summary>
    public static ErrorCode DatabaseError { get; } = new ErrorCode(503, 2);

    /// <summary>
    /// データベースタイムアウトのエラー
    /// </summary>
    public static ErrorCode DatabaseTimeoutError { get; } = new ErrorCode(503, 3);

    /// <summary>
    /// ロック失敗のエラー
    /// </summary>
    public static ErrorCode LockFailedError { get; } = new ErrorCode(409, 4);

    /// <summary>
    /// ログインしている武将が見つからないエラー
    /// </summary>
    public static ErrorCode LoginCharacterNotFoundError { get; } = new ErrorCode(401, 5);

    /// <summary>
    /// ログイン時のパラメータが足りないエラー
    /// </summary>
    public static ErrorCode LoginParameterMissingError { get; } = new ErrorCode(400, 6);

    /// <summary>
    /// ログイン時のパラメータが間違っているエラー
    /// </summary>
    public static ErrorCode LoginParameterIncorrectError { get; } = new ErrorCode(401, 7);

    /// <summary>
    /// ログイントークンが間違っているエラー
    /// </summary>
    public static ErrorCode LoginTokenIncorrectError { get; } = new ErrorCode(401, 8);

    /// <summary>
    /// アクセストークンが空であるエラー
    /// </summary>
    public static ErrorCode LoginTokenEmptyError { get; } = new ErrorCode(401, 9);

    /// <summary>
    /// 内部データが見つからないエラー
    /// </summary>
    public static ErrorCode InternalDataNotFoundError { get; } = new ErrorCode(500, 10);

    /// <summary>
    /// 指定した種類のコマンドは見つからないエラー
    /// </summary>
    public static ErrorCode CommandTypeNotFoundError { get; } = new ErrorCode(400, 11);

    /// <summary>
    /// 操作が他の人とかぶった時に出るエラー
    /// </summary>
    public static ErrorCode OperationConflictError { get; } = new ErrorCode(409, 12);

    /// <summary>
    /// デバッグモードでしか呼べないAPIを、通常時に呼び出した時に出るエラー
    /// </summary>
    public static ErrorCode DebugModeOnlyError { get; } = new ErrorCode(403, 13);

    /// <summary>
    /// 武将のアイコンが見つからないエラー
    /// </summary>
    public static ErrorCode CharacterIconNotFoundError { get; } = new ErrorCode(404, 14);

    /// <summary>
    /// 徴兵コマンドを入力しようとしたが、都市技術が足りないエラー
    /// </summary>
    public static ErrorCode LackOfTownTechnologyForSoldier { get; } = new ErrorCode(400, 15);

    /// <summary>
    /// コマンドのパラメータが足りないエラー
    /// </summary>
    public static ErrorCode LackOfCommandParameter { get; } = new ErrorCode(400, 16);

    /// <summary>
    /// コマンドパラメータが不正であるエラー
    /// </summary>
    public static ErrorCode InvalidCommandParameter { get; } = new ErrorCode(400, 17);

    /// <summary>
    /// そのデータにアクセスする権限がない一般的なエラー
    /// </summary>
    public static ErrorCode NotPermissionError { get; } = new ErrorCode(403, 18);

    /// <summary>
    /// 都市が見つからないエラー
    /// </summary>
    public static ErrorCode TownNotFoundError { get; } = new ErrorCode(404, 19);

    /// <summary>
    /// 意味のない、しなくてもいい操作をしようとしたときのエラー
    /// </summary>
    public static ErrorCode MeaninglessOperationError { get; } = new ErrorCode(403, 20);

    /// <summary>
    /// 武将が見つからないエラー
    /// </summary>
    public static ErrorCode CharacterNotFoundError { get; } = new ErrorCode(404, 21);

    /// <summary>
    /// 国が見つからないエラー
    /// </summary>
    public static ErrorCode CountryNotFoundError { get; } = new ErrorCode(404, 22);

    /// <summary>
    /// APIの引数が不正なエラー
    /// </summary>
    public static ErrorCode InvalidParameterError { get; } = new ErrorCode(400, 23);

    /// <summary>
    /// APIの引数が足りないエラー
    /// </summary>
    public static ErrorCode LackOfParameterError { get; } = new ErrorCode(400, 24);

    /// <summary>
    /// APIの引数のうち、名前が足りないエラー
    /// </summary>
    public static ErrorCode LackOfNameParameterError { get; } = new ErrorCode(400, 25);

    /// <summary>
    /// 不正な操作をしようとしたエラー
    /// </summary>
    public static ErrorCode InvalidOperationError { get; } = new ErrorCode(400, 26);

    /// <summary>
    /// 部隊が見つからないエラー
    /// </summary>
    public static ErrorCode UnitNotFoundError { get; } = new ErrorCode(404, 27);

    /// <summary>
    /// 部隊に制限がかかっているエラー
    /// </summary>
    public static ErrorCode UnitJoinLimitedError { get; } = new ErrorCode(403, 28);

    /// <summary>
    /// ツリー階層の親が見つからないなどのエラー
    /// </summary>
    public static ErrorCode ParentNodeNotFoundError { get; } = new ErrorCode(404, 29);

    /// <summary>
    /// ツリー階層で、トップ階層のものでないエラー
    /// </summary>
    public static ErrorCode NotTopNodeError { get; } = new ErrorCode(400, 30);

    /// <summary>
    /// ツリー階層のアイテムが見つからないエラー
    /// </summary>
    public static ErrorCode NodeNotFoundError { get; } = new ErrorCode(404, 31);

    /// <summary>
    /// 数値の範囲エラー
    /// </summary>
    public static ErrorCode NumberRangeError { get; } = new ErrorCode(400, 32);

    /// <summary>
    /// 文字列の長さエラー
    /// </summary>
    public static ErrorCode StringLengthError { get; } = new ErrorCode(400, 33);

    /// <summary>
    /// そこでは建国できないエラー
    /// </summary>
    public static ErrorCode CantPublisAtSuchTownhError { get; } = new ErrorCode(403, 34);

    /// <summary>
    /// そこでは仕官できないエラー
    /// </summary>
    public static ErrorCode CantJoinAtSuchTownhError { get; } = new ErrorCode(403, 35);

    /// <summary>
    /// その国には仕官できないエラー
    /// </summary>
    public static ErrorCode CantJoinAtSuchCountryhError { get; } = new ErrorCode(403, 36);

    /// <summary>
    /// すでに同じ情報が存在するエラー
    /// </summary>
    public static ErrorCode DuplicateCharacterNameOrAliasIdError { get; } = new ErrorCode(403, 37);

    /// <summary>
    /// すでに同じ情報が存在するエラー
    /// </summary>
    public static ErrorCode DuplicateCountryNameOrColorError { get; } = new ErrorCode(403, 38);

    /// <summary>
    /// IPアドレスが正常に取得できないエラー
    /// </summary>
    public static ErrorCode InvalidIpAddressError { get; } = new ErrorCode(400, 39);

    /// <summary>
    /// 重複登録と認定されたエラー
    /// </summary>
    public static ErrorCode DuplicateEntryError { get; } = new ErrorCode(403, 40);

    /// <summary>
    /// 招待コードが必要エラー
    /// </summary>
    public static ErrorCode InvitationCodeRequestedError { get; } = new ErrorCode(403, 41);

    /// <summary>
    /// 兵種が見つからないエラー
    /// </summary>
    public static ErrorCode SoldierTypeNotFoundError { get; } = new ErrorCode(404, 42);

    /// <summary>
    /// シークレットキーが間違っているエラー
    /// </summary>
    public static ErrorCode InvalidSecretKeyError { get; } = new ErrorCode(401, 43);

    /// <summary>
    /// 未実装エラー
    /// </summary>
    public static ErrorCode NotImplementedError { get; } = new ErrorCode(501, 44);

    /// <summary>
    /// アイテム所持数が最大に達しているエラー
    /// </summary>
    public static ErrorCode NotMoreItemsError { get; } = new ErrorCode(403, 45);

    /// <summary>
    /// 技能がないエラー
    /// </summary>
    public static ErrorCode NotSkillError { get; } = new ErrorCode(403, 46);

    /// <summary>
    /// 都市特化が対応してないエラー
    /// </summary>
    public static ErrorCode NotTownTypeError { get; } = new ErrorCode(403, 47);

    /// <summary>
    /// 建築物が対応してないエラー
    /// </summary>
    public static ErrorCode LackOfTownSubBuildingForSoldier { get; } = new ErrorCode(403, 48);

    /// <summary>
    /// 未実装エラー
    /// </summary>
    public static ErrorCode NotSupportedError { get; } = new ErrorCode(501, 49);

    /// <summary>
    /// ユーザの制限された行動エラー
    /// </summary>
    public static ErrorCode BlockedActionError { get; } = new ErrorCode(403, 50);

    /// <summary>
    /// アカウントが見つからないエラー
    /// </summary>
    public static ErrorCode AccountNotFoundError { get; } = new ErrorCode(404, 51);

    /// <summary>
    /// アカウントのパスワードが違うエラー
    /// </summary>
    public static ErrorCode AccountLoginPasswordIncorrectError { get; } = new ErrorCode(403, 52);

    /// <summary>
    /// すでに同じ情報が存在するエラー
    /// </summary>
    public static ErrorCode DuplicateAccountNameOrAliasIdError { get; } = new ErrorCode(403, 53);

    /// <summary>
    /// 同じ武将にすでにアカウントが作成されているエラー
    /// </summary>
    public static ErrorCode DuplicateAccountOfCharacterError { get; } = new ErrorCode(403, 54);

    /// <summary>
    /// 画像のアップロードに失敗したエラー
    /// </summary>
    public static ErrorCode UploadImageFailedError { get; } = new ErrorCode(400, 55);

    /// <summary>
    /// 現在のルールセットで制限されているエラー
    /// </summary>
    public static ErrorCode RuleSetError { get; } = new ErrorCode(400, 56);

    /// <summary>
    /// 現在の年月が条件に達していないエラー
    /// </summary>
    public static ErrorCode TooEarlyError { get; } = new ErrorCode(400, 57);

    /// <summary>
    /// 宗教に起因するエラー
    /// </summary>
    public static ErrorCode ReligionError { get; } = new ErrorCode(400, 58);

    public class RangeErrorParameter
    {
      [JsonProperty("name")]
      public string Name { get; }
      [JsonProperty("current")]
      public int CurrentLength { get; }
      [JsonProperty("max")]
      public int MaxLength { get; }
      [JsonProperty("min")]
      public int Minlength { get; }
      public RangeErrorParameter(string name, int current, int min, int max)
      {
        this.Name = name;
        this.CurrentLength = current;
        this.MaxLength = max;
        this.Minlength = min;
      }
    }

    /// <summary>
    /// エラーコード
    /// </summary>
    public int Code { get; }

    /// <summary>
    /// HTTPステータスコード
    /// </summary>
    public int StatusCode { get; }

    public ErrorCode(int status, int code)
    {
      this.Code = code;
      this.StatusCode = status;
    }

    public void Throw()
    {
      throw new SangokuKmyException(this);
    }

    public void Throw(object data)
    {
      throw new SangokuKmyException(this.StatusCode, this, data);
    }

    public void Throw(Exception original)
    {
      throw new SangokuKmyException(original, this);
    }

    public void Throw(Exception original, object data)
    {
      throw new SangokuKmyException(original, this.StatusCode, this, data);
    }

    public void Throw(Exception original, int statusCode)
    {
      throw new SangokuKmyException(original, statusCode, this);
    }

    public void Throw(Exception original, int statusCode, object data)
    {
      throw new SangokuKmyException(original, statusCode, this, data);
    }

    public override bool Equals(object obj)
    {
      if (obj is ErrorCode code)
      {
        return this.Code == code.Code;
      }
      return false;
    }

    public override int GetHashCode()
    {
      return this.Code.GetHashCode();
    }
  }

  public static class ErrorCodeExtensions {
    /// <summary>
    /// 例外がそのエラーコードを持っているか確認する
    /// </summary>
    /// <returns>例外のエラーコードが一致するか</returns>
    /// <param name="ex">例外</param>
    /// <param name="code">エラーコード</param>
    public static bool Is(this SangokuKmyException ex, ErrorCode code) {
      return ex.ErrorCode.Code == code.Code;
    }
  }
}
