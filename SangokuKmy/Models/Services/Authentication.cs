using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
    public static AuthenticationData WithToken(string token)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// IDとパスワードから認証を行う
    /// </summary>
    /// <param name="id">ID</param>
    /// <param name="password">パスワード</param>
    /// <returns>認証結果</returns>
    public static AuthenticationData WithIdAndPassword(string id, string password)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// ログイン処理を行い、アクセストークンを新たに発行する
    /// </summary>
    /// <param name="id">ID</param>
    /// <param name="password">パスワード</param>
    /// <returns>認証結果</returns>
    public static AuthenticationData Login(string id, string password)
    {
      throw new NotImplementedException();
    }

    /// <summary>
    /// アクセストークンを新たに作成する
    /// </summary>
    /// <returns>作成されたアクセストークン</returns>
    private static string GenerateAccessToken(string id, string password)
    {
      var hash = new SHA256CryptoServiceProvider()
        .ComputeHash(Encoding.UTF8.GetBytes($"{id} + {password} + Asuka is Kmy *** {DateTime.Now.ToLongTimeString()}"))
        .Select(b => string.Format("{0:X2}", b));
      var hashText = string.Join("", hash);

      return Convert.ToBase64String(Encoding.UTF8.GetBytes(hashText));
    }
  }
}
