using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Services
{
  public static class ResetService
  {
    public static async Task ResetAsync(MainRepository repo)
    {
      await repo.AuthenticationData.ResetAsync();
      await repo.BattleLog.ResetAsync();
      await repo.CharacterCommand.ResetAsync();
      await repo.Character.ResetAsync();
      await repo.ChatMessage.ResetAsync();
      await repo.CountryDiplomacies.ResetAsync();
      await repo.Country.ResetAsync();
      await repo.MapLog.ResetAsync();
      await repo.ScoutedTown.ResetAsync();
      await repo.ThreadBbs.ResetAsync();
      await repo.Town.ResetAsync();
      await repo.Unit.ResetAsync();

      await ResetTownsAsync(repo);

      var system = await repo.System.GetAsync();
      system.GameDateTime = new GameDateTime
      {
        Year = Config.StartYear,
        Month = Config.StartMonth,
      };
      system.CurrentMonthStartDateTime = DateTime.Now;
      system.IsWaitingReset = false;
      system.IntResetGameDateTime = 0;
      if (system.IsNextPeriodBeta)
      {
        system.BetaVersion++;
      }
      else
      {
        system.BetaVersion = 0;
        system.Period++;
      }

      await repo.MapLog.AddAsync(new MapLog
      {
        EventType = EventType.Reset,
        Date = DateTime.Now,
        ApiGameDateTime = system.GameDateTime,
        IsImportant = false,
        Message = "ゲームプログラムを開始しました",
      });
      await repo.SaveChangesAsync();

      await StatusStreaming.Default.SendAllAsync(ApiData.From(new ApiSignal
      {
        Type = SignalType.Reseted,
      }));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new ApiSignal
      {
        Type = SignalType.Reseted,
      }));
    }

    public static async Task RequestResetAsync(MainRepository repo)
    {
      var system = await repo.System.GetAsync();
      system.IsWaitingReset = true;

      var currentMonth = system.CurrentMonthStartDateTime;
      var todayResetHour = new DateTime(currentMonth.Year, currentMonth.Month, currentMonth.Day, 21, 0, 0, 0);
      var resetHour = todayResetHour.AddDays(currentMonth.Hour < 21 ? 2 : 3);
      var sinceResetTime = resetHour - currentMonth;
      var resetTurn = (int)Math.Round(sinceResetTime.TotalMinutes / 10.0f);
      system.ResetGameDateTime = GameDateTime.FromInt(system.GameDateTime.ToInt() + resetTurn);
    }

    private static async Task ResetTownsAsync(MainRepository repo)
    {
      var rand = new Random(DateTime.Now.Millisecond);
      var initialTowns = await repo.Town.GetAllInitialTownsAsync();
      var towns = new List<Town>();
      foreach (var itown in initialTowns)
      {
        var typeId = itown.Type;
        if (typeId == TownType.Any)
        {
          var r = rand.Next(1, 4);
          if (r == 1)
          {
            typeId = TownType.Agriculture;
          }
          else if (r == 2)
          {
            typeId = TownType.Commercial;
          }
          else
          {
            typeId = TownType.Fortress;
          }
        }
        var type = typeId == TownType.Agriculture ? TownTypeDefinition.Agriculture :
                   typeId == TownType.Commercial ? TownTypeDefinition.Commercial :
                   typeId == TownType.Fortress ? TownTypeDefinition.Fortress :
                   TownTypeDefinition.Large;
        var town = new Town
        {
          Name = itown.Name,
          X = itown.X,
          Y = itown.Y,
          Type = typeId,
          AgricultureMax = type.AgricultureMax,
          CommercialMax = type.CommercialMax,
          TechnologyMax = type.TechnologyMax,
          WallMax = type.WallMax,
          WallGuardMax = type.WallGuardMax,
          PeopleMax = type.PeopleMax,
          People = type.People,
          Security = (short)type.Security,
        };
        towns.Add(town);
      }

      await repo.Town.AddTownsAsync(towns);
    }

    private abstract class TownTypeDefinition
    {
      protected static readonly Random rand = new Random(DateTime.Now.Millisecond);

      public static TownTypeDefinition Agriculture { get; } = new AgricultureTownType();
      public static TownTypeDefinition Commercial { get; } = new CommercialTownType();
      public static TownTypeDefinition Fortress { get; } = new FortressTownType();
      public static TownTypeDefinition Large { get; } = new LargeTownType();

      public abstract int AgricultureMax { get; }

      public abstract int CommercialMax { get; }

      public abstract int TechnologyMax { get; }

      public abstract int WallMax { get; }

      public abstract int WallGuardMax { get; }

      public abstract int PeopleMax { get; }

      public abstract int People { get; }

      public abstract int Security { get; }

      private class AgricultureTownType : TownTypeDefinition
      {
        public override int AgricultureMax => rand.Next(10, 16) * 100;

        public override int CommercialMax => rand.Next(3, 7) * 100;

        public override int TechnologyMax => 901;

        public override int WallMax => rand.Next(10, 17) * 100;

        public override int WallGuardMax => rand.Next(4, 11) * 100;

        public override int PeopleMax => 50000;

        public override int People => 8000;

        public override int Security => 80;
      }

      private class CommercialTownType : TownTypeDefinition
      {
        public override int AgricultureMax => rand.Next(10, 16) * 100;

        public override int CommercialMax => rand.Next(3, 7) * 100;

        public override int TechnologyMax => 901;

        public override int WallMax => rand.Next(10, 17) * 100;

        public override int WallGuardMax => rand.Next(4, 11) * 100;

        public override int PeopleMax => 50000;

        public override int People => 8000;

        public override int Security => 80;
      }

      private class FortressTownType : TownTypeDefinition
      {
        public override int AgricultureMax => rand.Next(10, 16) * 100;

        public override int CommercialMax => rand.Next(3, 7) * 100;

        public override int TechnologyMax => 901;

        public override int WallMax => rand.Next(10, 17) * 100;

        public override int WallGuardMax => rand.Next(4, 11) * 100;

        public override int PeopleMax => 50000;

        public override int People => 8000;

        public override int Security => 80;
      }

      private class LargeTownType : TownTypeDefinition
      {
        public override int AgricultureMax => rand.Next(10, 16) * 100;

        public override int CommercialMax => rand.Next(3, 7) * 100;

        public override int TechnologyMax => 901;

        public override int WallMax => rand.Next(10, 17) * 100;

        public override int WallGuardMax => rand.Next(4, 11) * 100;

        public override int PeopleMax => 50000;

        public override int People => 8000;

        public override int Security => 80;
      }
    }
  }
}
