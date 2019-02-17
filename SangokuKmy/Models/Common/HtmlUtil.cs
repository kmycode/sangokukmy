using System;
namespace SangokuKmy.Models.Common
{
  public static class HtmlUtil
  {
    /// <summary>
    /// 文字列をエスケープする
    /// </summary>
    /// <returns>エスケープされた文字列</returns>
    /// <param name="text">エスケープする文字列</param>
    public static string Escape(string text)
    {
      return text
        .Replace("&", "&amp;")
        .Replace("<", "&lt;")
        .Replace(">", "&gt;");
    }

    /// <summary>
    /// 文字列のエスケープを解除する
    /// </summary>
    /// <returns>復元された文字列</returns>
    /// <param name="text">エスケープされた文字列</param>
    public static string Unescape(string text)
    {
      return text
        .Replace("&amp;", "&")
        .Replace("&lt;", "<")
        .Replace("&gt;", ">");
    }
  }
}
