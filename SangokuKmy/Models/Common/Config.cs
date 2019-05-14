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

    public static class Game
    {
      public static string SecretKey { get; set; }

      public static string UploadedIconDirectory { get; set; }

      public static string HistoricalUploadedIconDirectory { get; set; }

      public static bool IsAllowMonarchReinforcement { get; set; } = false;

      public static bool IsThief { get; set; } = false;
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
    public static int NextLank { get; } = 1200;

    /// <summary>
    /// 建国後の戦闘禁止期間
    /// </summary>
    public static int CountryBattleStopDuring { get; } = 144 * 2;

    /// <summary>
    /// 仕官制限中の最大仕官数
    /// </summary>
    public static int CountryJoinMaxOnLimited { get; } = 4;

    public static int CountryColorMax { get; } = 11;

    public static int DeleteTurns { get; } = 500;

    public static int RiceBuyMax { get; } = 20000;

    public static int TownBuildingMax { get; } = 2000;

    public static int PaySafeMax { get; } = 10_0000;

    public static int CountrySafeMax { get; } = 100_0000;

    public static int SecretaryCost { get; } = 2000;

    public static int ScouterMax { get; } = 2;

    public static int ScouterCost { get; } = 2000;

    public static float SoldierPeopleCost { get; } = 5.45f;

    public static float RicePriceBase { get; } = 1000000.0f;
  }
}
