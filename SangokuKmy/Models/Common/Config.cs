﻿using System;
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

      public static string GcmServerKey { get; set; }
    }

    public static class Admin
    {
      public static string Name { get; set; }

      public static string AliasId { get; set; }

      public static string Password { get; set; }

      public static string GravatarMailAddressMD5 { get; set; }
    }

    public static class Game
    {
      public static string SecretKey { get; set; }

      public static string UploadedIconDirectory { get; set; }

      public static string HistoricalUploadedIconDirectory { get; set; }

      public static bool IsAllowMonarchReinforcement { get; set; } = false;

      public static bool IsThief { get; set; } = false;

      public static bool IsGenerateAdminCharacter { get; set; } = false;
    }

    public static int UpdateTime { get; } = 600;

    public static short StartYear { get; } = 0;

    public static short StartMonth { get; } = 1;

    public static short UpdateStartYear { get; } = 12;

    public static short BuyTownStartYear { get; } = 48;

    /// <summary>
    /// 階級の数（新規登録直後に与えられる階級も含まれるので注意）
    /// </summary>
    public static short LankCount { get; } = 26;

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
    public static int CountryJoinMaxOnLimited { get; } = 5;

    public static int CountryColorMax { get; } = 18;

    public static int DeleteTurns { get; } = 500;

    public static int RiceBuyMax { get; } = 15000;

    public static int TownBuildingMax { get; } = 2000;

    public static int PaySafeMax { get; } = 10_0000;

    public static int CountrySafeMax { get; } = 100_0000;

    public static int SecretaryCost { get; } = 2000;

    public static int ScouterMax { get; } = 2;

    public static int ScouterCost { get; } = 2000;

    public static float SoldierPeopleCost { get; } = 5.0f;

    public static float RicePriceBase { get; } = 1000000.0f;
  }
}
