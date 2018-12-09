using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace SangokuKmy.Models.Services
{
  public static class CharacterService
  {
    /// <summary>
    /// パスワードからハッシュを生成する
    /// </summary>
    /// <returns>生成されたハッシュ</returns>
    /// <param name="password">パスワード</param>
    public static string GeneratePasswordHash(string password)
    {
      var hash = new SHA256CryptoServiceProvider()
        .ComputeHash(Encoding.UTF8.GetBytes($"{password} hello, for you"))
        .Select(b => string.Format("{0:x2}", b));
      var hashText = string.Join(string.Empty, hash);

      return hashText;
    }
  }
}
