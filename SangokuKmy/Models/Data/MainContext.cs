using Microsoft.EntityFrameworkCore;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data
{
  /// <summary>
  /// メインコンテキスト
  /// </summary>
  public class MainContext : DbContext
  {
    /// <summary>
    /// 認証データ
    /// </summary>
    public DbSet<AuthenticationData> AuthenticationData { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
      if (!string.IsNullOrEmpty(Config.Database.MySqlConnectionString))
      {
        optionsBuilder.UseMySql(Config.Database.MySqlConnectionString);
      }
    }
  }
}
