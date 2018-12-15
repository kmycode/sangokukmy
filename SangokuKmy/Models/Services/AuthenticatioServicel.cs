using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using SangokuKmy.Common;

namespace SangokuKmy.Models.Services
{
  /// <summary>
  /// 認証を行うクラス
  /// </summary>
  public static class AuthenticationService
  {
    /// <summary>
    /// アクセストークンから認証を行う
    /// </summary>
    /// <param name="token">アクセストークン</param>
    /// <returns>認証結果</returns>
    public static async Task<AuthenticationData> WithTokenAsync(MainRepository repo, string token)
    {
      var data = await repo.AuthenticationData
        .FindByTokenAsync(token)
        .GetOrErrorAsync(ErrorCode.LoginTokenIncorrectError);

      return data;
    }

    /// <summary>
    /// IDとパスワードから認証を行う
    /// </summary>
    /// <param name="id">ID</param>
    /// <param name="password">パスワード</param>
    /// <returns>認証結果</returns>
    public static async Task<AuthenticationData> WithIdAndPasswordAsync(MainRepository repo, string id, string password)
    {
      return await LoginAsync(repo, id, password);
    }

    /// <summary>
    /// ログイン処理を行い、アクセストークンを新たに発行する
    /// </summary>
    /// <param name="aliasId">ID</param>
    /// <param name="password">パスワード</param>
    /// <returns>認証結果</returns>
    private static async Task<AuthenticationData> LoginAsync(MainRepository repo, string aliasId, string password)
    {
      if (string.IsNullOrEmpty(aliasId) || string.IsNullOrEmpty(password))
      {
        ErrorCode.LoginParameterMissingError.Throw();
      }

      var chara = (await repo.Character.GetByAliasIdAsync(aliasId))
        .GetOrError(ErrorCode.LoginCharacterNotFoundError);
      if (!chara.TryLogin(password))
      {
        ErrorCode.LoginParameterIncorrectError.Throw();
      }

      var data = new AuthenticationData
      {
        AccessToken = GenerateAccessToken(aliasId, password),
        CharacterId = chara.Id,
        ExpirationTime = DateTime.Now.AddDays(365),
        Scope = Scope.All,
      };

      await repo.AuthenticationData.AddAsync(data);
      return data;
    }

    /// <summary>
    /// アクセストークンを新たに作成する
    /// </summary>
    /// <returns>作成されたアクセストークン</returns>
    private static string GenerateAccessToken(string id, string password)
    {
      var hash = new SHA256CryptoServiceProvider()
        .ComputeHash(Encoding.UTF8.GetBytes($"{id} + {password} + Asuka is Kmy *** {DateTime.Now.ToLongTimeString()}"))
        .Select(b => string.Format("{0:x2}", b));
      var hashText = string.Join(string.Empty, hash);

      return Convert.ToBase64String(Encoding.UTF8.GetBytes(hashText));
    }
  }
}
