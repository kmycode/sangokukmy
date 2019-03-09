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
    private static readonly Random rand = new Random(DateTime.Now.Millisecond);

    public static async Task ResetAsync(MainRepository repo)
    {
      await repo.AuthenticationData.ResetAsync();
      await repo.BattleLog.ResetAsync();
      await repo.CharacterCommand.ResetAsync();
      await repo.CharacterSoldierType.ResetAsync();
      await repo.Character.ResetAsync();
      await repo.EntryHost.ResetAsync();
      await repo.ChatMessage.ResetAsync();
      await repo.CountryDiplomacies.ResetAsync();
      await repo.Country.ResetAsync();
      await repo.MapLog.ResetAsync();
      await repo.ScoutedTown.ResetAsync();
      await repo.ThreadBbs.ResetAsync();
      await repo.Town.ResetAsync();
      await repo.Unit.ResetAsync();
      await repo.Reinforcement.ResetAsync();

      await ResetTownsAsync(repo);

      var now = DateTime.Now;
      var system = await repo.System.GetAsync();
      system.GameDateTime = new GameDateTime
      {
        Year = Config.StartYear,
        Month = Config.StartMonth,
      };
      system.CurrentMonthStartDateTime = new DateTime(now.Year, now.Month, now.Day, 21, 0, 0, 0);
      system.IsWaitingReset = false;
      system.IntResetGameDateTime = 0;
      system.TerroristCount = 0;
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

      await RecordHistoryAsync(repo, system);
    }

    private static async Task RecordHistoryAsync(MainRepository repo, SystemData system)
    {
      await repo.SaveChangesAsync();
      
      var countries = await repo.Country.GetAllAsync();
      var characters = await repo.Character.GetAllAliveWithIconAsync();

      var unifiedCountry = countries.FirstOrDefault(c => !c.HasOverthrown);
      CountryMessage unifiedCountryMessage = null;
      if (unifiedCountry != null)
      {
        var messageOptional = await repo.Country.GetMessageAsync(unifiedCountry.Id, CountryMessageType.Unified);
        messageOptional.Some((message) => unifiedCountryMessage = message);
      }

      var history = new History
      {
        Period = system.Period,
        BetaVersion = system.BetaVersion,
        UnifiedDateTime = DateTime.Now,
        UnifiedCountryMessage = unifiedCountryMessage?.Message ?? string.Empty,
        Characters = characters.Select(c =>
        {
          var chara = HistoricalCharacter.FromCharacter(c.Character);
          chara.Icon = HistoricalCharacterIcon.FromCharacterIcon(c.Icon);
          return chara;
        }).ToArray(),
        Countries = countries.Select(c =>
        {
          var country = HistoricalCountry.FromCountry(c);
          return country;
        }).ToArray(),
      };
      await repo.History.RecordAndSaveAsync(history);
    }

    private static async Task ResetTownsAsync(MainRepository repo)
    {
      var initialTowns = await repo.Town.GetAllInitialTownsAsync();
      var towns = new List<Town>();
      foreach (var itown in initialTowns)
      {
        var typeId = itown.Type;
        var town = CreateTown(typeId);
        town.Name = itown.Name;
        town.X = itown.X;
        town.Y = itown.Y;
        towns.Add(town);
      }

      await repo.Town.AddTownsAsync(towns);
    }

    public static Town CreateTown(TownType typeId)
    {
      if (typeId == TownType.Any)
      {
        var r = rand.Next(0, 9);
        if (r <= 2)
        {
          typeId = TownType.Agriculture;
        }
        else if (r <= 5)
        {
          typeId = TownType.Commercial;
        }
        else if (r <= 7)
        {
          typeId = TownType.Fortress;
        }
        else if (r == 8)
        {
          typeId = TownType.Large;
        }
        else
        {
          typeId = TownType.Agriculture;
        }
      }
      var type = typeId == TownType.Agriculture ? TownTypeDefinition.AgricultureType :
                 typeId == TownType.Commercial ? TownTypeDefinition.CommercialType :
                 typeId == TownType.Fortress ? TownTypeDefinition.FortressType :
                 TownTypeDefinition.LargeType;
      var town = new Town
      {
        Type = typeId,
        Agriculture = type.Agriculture,
        AgricultureMax = type.AgricultureMax,
        Commercial = type.Commercial,
        CommercialMax = type.CommercialMax,
        Technology = type.Technology,
        TechnologyMax = type.TechnologyMax,
        Wall = type.Wall,
        WallMax = type.WallMax,
        PeopleMax = type.PeopleMax,
        People = type.People,
        Security = (short)type.Security,
      };
      town.WallGuard = town.Wall;
      town.WallGuardMax = town.WallMax;
      {
        // 都市施設
        var b = new TownBuilding[]
        {
          TownBuilding.Economy,
          TownBuilding.MilitaryStation,
          TownBuilding.OpenWall,
          TownBuilding.RepairWall,
          TownBuilding.SaveWall,
          TownBuilding.TrainIntellect,
          TownBuilding.TrainLeadership,
          TownBuilding.TrainPopularity,
          TownBuilding.TrainStrong,
        };
        var r = rand.Next(0, b.Length);
        town.TownBuilding = b[r];
      }
      {
        // 国家施設
        var b = new CountryBuilding[]
        {
          CountryBuilding.CountrySafe,
          CountryBuilding.Secretary,
          CountryBuilding.SoldierLaboratory,
          CountryBuilding.Spy,
        };
        var r = rand.Next(0, b.Length);
        town.CountryBuilding = b[r];
      }
      return town;
    }

    private abstract class TownTypeDefinition
    {
      protected static readonly Random rand = new Random(DateTime.Now.Millisecond);

      public static TownTypeDefinition AgricultureType { get; } = new AgricultureTownType();
      public static TownTypeDefinition CommercialType { get; } = new CommercialTownType();
      public static TownTypeDefinition FortressType { get; } = new FortressTownType();
      public static TownTypeDefinition LargeType { get; } = new LargeTownType();

      public abstract int Agriculture { get; }

      public abstract int AgricultureMax { get; }

      public abstract int Commercial { get; }

      public abstract int CommercialMax { get; }

      public abstract int Technology { get; }

      public abstract int TechnologyMax { get; }

      public abstract int Wall { get; }

      public abstract int WallMax { get; }

      public abstract int PeopleMax { get; }

      public abstract int People { get; }

      public abstract int Security { get; }

      private class AgricultureTownType : TownTypeDefinition
      {
        public override int Agriculture => rand.Next(4, 9) * 100;

        public override int AgricultureMax => rand.Next(12, 19) * 100;

        public override int Commercial => rand.Next(1, 3) * 100;

        public override int CommercialMax => rand.Next(3, 7) * 100;

        public override int Technology => rand.Next(0, 4) * 100;

        public override int TechnologyMax => 900;

        public override int Wall => rand.Next(1, 6) * 100;

        public override int WallMax => rand.Next(18, 25) * 100;

        public override int PeopleMax => 40000;

        public override int People => 8000;

        public override int Security => 80;
      }

      private class CommercialTownType : TownTypeDefinition
      {
        public override int Agriculture => rand.Next(1, 3) * 100;

        public override int AgricultureMax => rand.Next(3, 7) * 100;

        public override int Commercial => rand.Next(4, 9) * 100;

        public override int CommercialMax => rand.Next(12, 19) * 100;

        public override int Technology => rand.Next(0, 4) * 100;

        public override int TechnologyMax => 900;

        public override int Wall => rand.Next(1, 6) * 100;

        public override int WallMax => rand.Next(18, 25) * 100;

        public override int PeopleMax => 40000;

        public override int People => 14000;

        public override int Security => 50;
      }

      private class FortressTownType : TownTypeDefinition
      {
        public override int Agriculture => rand.Next(0, 3) * 100;

        public override int AgricultureMax => rand.Next(0, 5) * 100;

        public override int Commercial => rand.Next(0, 3) * 100;

        public override int CommercialMax => rand.Next(0, 5) * 100;

        public override int Technology => rand.Next(1, 5) * 100;

        public override int TechnologyMax => 999;

        public override int Wall => rand.Next(4, 13) * 100;

        public override int WallMax => rand.Next(26, 49) * 100;

        public override int PeopleMax => 30000;

        public override int People => 4000;

        public override int Security => 70;
      }

      private class LargeTownType : TownTypeDefinition
      {
        public override int Agriculture => rand.Next(1, 3) * 100;

        public override int AgricultureMax => rand.Next(4, 11) * 100;

        public override int Commercial => rand.Next(1, 3) * 100;

        public override int CommercialMax => rand.Next(4, 11) * 100;

        public override int Technology => rand.Next(1, 7) * 100;

        public override int TechnologyMax => 900;

        public override int Wall => rand.Next(8, 15) * 100;

        public override int WallMax => rand.Next(12, 21) * 100;

        public override int PeopleMax => 50000;

        public override int People => 20000;

        public override int Security => 35;
      }
    }
  }
}
