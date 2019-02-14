using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Common
{
  public static class Config
  {
    public static class Database
    {
      public static string MySqlConnectionString { get; set; }
    }

    public static int UpdateTime { get; } = 600;

    public static short StartYear { get; } = 0;

    public static short StartMonth { get; } = 1;

    public static short UpdateStartYear { get; } = 24;

    /// <summary>
    /// 階級の数（新規登録直後に与えられる階級も含まれるので注意）
    /// </summary>
    public static short LankCount { get; } = 21;

    /// <summary>
    /// 次の階級までに必要な階級値
    /// </summary>
    public static int NextLank { get; } = 800;

    /// <summary>
    /// 建国後の戦闘禁止期間
    /// </summary>
    public static int CountryBattleStopDuring { get; } = 144 * 2;

    /// <summary>
    /// 仕官制限中の最大仕官数
    /// </summary>
    public static int CountryJoinMaxOnLimited { get; } = 6;

    public static int CountryColorMax { get; } = 8;

    public static int DeleteTurns { get; } = 500;
  }
}
