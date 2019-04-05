﻿using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Services
{
  public static class MapService
  {
    private static readonly string[] townNames = new string[]
    {
      "酒泉", "西郡", "    ", "    ", "    ", "朔方", "云中", "代"  , "上谷", "北平",
      "西涼", "狄道", "富平", "石城", "上郡", "河西", "巨鹿", "太原", "幽州", "渤海",
      "西平", "金城", "安定", "北地", "朝歌", "上党", "邯鄲", "平原", "臨済", "北海",
      "    ", "天水", "高陵", "河南", "河内", "許昌", "濮陽", "魯"  , "小沛", "琅琊",
      "    ", "槐里", "長安", "弘農", "雒陽", "陳留", "武平", "任城", "下邳", "利城",
      "武都", "下弁", "漢中", "宛"  , "上庸", "潁川", "夏口", "沛"  , "徐州", "    ",
      "    ", "梓潼", "西城", "新野", "襄陽", "汝南", "合肥", "淮南", "寿春", "江都",
      "永昌", "成都", "永安", "武陵", "長沙", "江夏", "武昌", "柴桑", "建鄴", "呉"  ,
      "建寧", "武陽", "    ", "零陵", "桂陽", "安城", "戸陵", "豫章", "虎林", "会稽",
      "雲南", "朱堤", "    ", "郁林", "南城", "始光", "    ", "    ", "建安", "温州",
    };

    public static IReadOnlyList<InitialTown> CreateMap(int townCount)
    {
      if (townCount < 1)
      {
        return Enumerable.Empty<InitialTown>().ToArray();
      }

      IList<Town> towns = null;
      var isVerify = false;

      while (!isVerify)
      {
        towns = new List<Town>
        {
          new Town
          {
            Id = 1,
            X = (short)(RandomService.Next(0, 2) + 4),
            Y = (short)(RandomService.Next(0, 2) + 4),
          },
        };

        for (var i = 1; i < townCount; i++)
        {
          var pos = GetNewTownPosition(towns, null);
          towns.Add(new Town
          {
            Id = (uint)i + 1,
            X = pos.X,
            Y = pos.Y,
          });
        }

        if (townCount >= 5)
        {
          var hit = 0;
          var hit2 = 0;
          foreach (var t in towns)
          {
            var arounds = towns.GetAroundTowns(t);
            var c = arounds.Count(at => arounds.Any(att => att.IsNextToTown(at)));
            if (c == arounds.Count())
            {
              hit++;
            }
            else if (c <= 2)
            {
              hit2++;
            }
          }
          //isVerify = count >= townCount - townCount / 2 && count <= townCount - townCount / 6;
          isVerify = hit == Math.Max(4, townCount - townCount * 3 / 5) && hit2 < townCount - townCount * 4 / 5;
        }
        else
        {
          isVerify = true;
        }

        if (isVerify)
        {
          var xMax = towns.Max(t => t.X);
          var yMax = towns.Max(t => t.Y);
          var xMin = towns.Min(t => t.X);
          var yMin = towns.Min(t => t.Y);
          var centerX = (xMax - xMin) / 2 + xMin;
          var centerY = (yMax - yMin) / 2 + yMin;
          if (centerX < 5)
          {
            ShiftMap(towns, (short)Math.Min(4 - centerX, 9 - xMax), 0);
          }
          else if (centerX > 5)
          {
            ShiftMap(towns, (short)-Math.Min(centerX - 5, xMin), 0);
          }
          if (centerY < 5)
          {
            ShiftMap(towns, (short)Math.Min(4 - centerY, 9 - yMax), 0);
          }
          else if (centerY > 5)
          {
            ShiftMap(towns, (short)-Math.Min(centerY - 5, yMin), 0);
          }

          isVerify = towns.All(t => !string.IsNullOrWhiteSpace(GetTownName(t.X, t.Y)));
        }
      }

      return towns.Select(t => new InitialTown
      {
        X = t.X,
        Y = t.Y,
        Name = GetTownName(t.X, t.Y),
        Type = TownType.Any,
      }).ToArray();
    }

    private static string GetTownName(short x, short y)
    {
      return townNames[y * 10 + x];
    }

    private static void ShiftMap(IEnumerable<Town> towns, short x, short y)
    {
      foreach (var town in towns)
      {
        town.X += x;
        town.Y += y;
      }
    }

    public static (short X, short Y) GetNewTownPosition(IEnumerable<Town> existTowns, Func<Town, bool> subject)
    {
      var borderTowns = existTowns
        .Where(t => existTowns.GetAroundTowns(t).Count() <
                    ((t.X < 9 && t.X > 0 && t.Y < 9 && t.Y > 0) ?  8 :
                     (t.X < 9 && t.X > 0) ? 5 :
                     (t.Y < 9 && t.Y > 0) ? 5 : 3));
      if (subject != null)
      {
        borderTowns = borderTowns.Where(subject);
      }
      if (!borderTowns.Any())
      {
        return (-1, -1);
      }

      var nearTown = borderTowns.ElementAt(RandomService.Next(0, borderTowns.Count()));
      var townPositions = new Tuple<short, short>[]
      {
        new Tuple<short, short>((short)(nearTown.X - 1), (short)(nearTown.Y - 1)),
        new Tuple<short, short>((short)(nearTown.X + 0), (short)(nearTown.Y - 1)),
        new Tuple<short, short>((short)(nearTown.X + 1), (short)(nearTown.Y - 1)),
        new Tuple<short, short>((short)(nearTown.X - 1), (short)(nearTown.Y + 0)),
        new Tuple<short, short>((short)(nearTown.X + 1), (short)(nearTown.Y + 0)),
        new Tuple<short, short>((short)(nearTown.X - 1), (short)(nearTown.Y + 1)),
        new Tuple<short, short>((short)(nearTown.X + 0), (short)(nearTown.Y + 1)),
        new Tuple<short, short>((short)(nearTown.X + 1), (short)(nearTown.Y + 1)),
      }
      .Where(t => t.Item1 >= 0 && t.Item1 < 10 && t.Item2 >= 0 && t.Item2 < 10)
      .Where(t => !existTowns.Any(tt => tt.X == t.Item1 && tt.Y == t.Item2))
      .ToArray();
      var townPosition = townPositions[RandomService.Next(0, townPositions.Length)];

      return (townPosition.Item1, townPosition.Item2);
    }

    public static Town CreateTown(TownType typeId)
    {
      if (typeId == TownType.Any)
      {
        var r = RandomService.Next(0, 10);
        if (r <= 2)
        {
          typeId = TownType.Agriculture;
        }
        else if (r <= 6)
        {
          typeId = TownType.Commercial;
        }
        else if (r <= 8)
        {
          typeId = TownType.Fortress;
        }
        else if (r == 9)
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
      town.Agriculture = Math.Min(town.Agriculture, town.AgricultureMax);
      town.Commercial = Math.Min(town.Commercial, town.CommercialMax);
      town.Technology = Math.Min(town.Technology, town.TechnologyMax);
      town.Wall = Math.Min(town.Wall, town.WallMax);
      town.People = Math.Min(town.People, town.PeopleMax);
      {
        // 都市施設
        var b = new TownBuilding[]
        {
          TownBuilding.MilitaryStation,
          TownBuilding.OpenWall,
          TownBuilding.RepairWall,
          TownBuilding.TrainIntellect,
          TownBuilding.TrainLeadership,
          TownBuilding.TrainPopularity,
          TownBuilding.TrainStrong,
        };
        var r = RandomService.Next(0, b.Length);
        town.TownBuilding = b[r];
      }
      return town;
    }

    private abstract class TownTypeDefinition
    {
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
        public override int Agriculture => RandomService.Next(4, 9) * 100;

        public override int AgricultureMax => RandomService.Next(12, 19) * 100;

        public override int Commercial => RandomService.Next(1, 3) * 100;

        public override int CommercialMax => RandomService.Next(3, 7) * 100;

        public override int Technology => RandomService.Next(0, 4) * 100;

        public override int TechnologyMax => 900;

        public override int Wall => RandomService.Next(1, 6) * 100;

        public override int WallMax => RandomService.Next(18, 25) * 100;

        public override int PeopleMax => 40000;

        public override int People => 8000;

        public override int Security => 80;
      }

      private class CommercialTownType : TownTypeDefinition
      {
        public override int Agriculture => RandomService.Next(1, 3) * 100;

        public override int AgricultureMax => RandomService.Next(3, 7) * 100;

        public override int Commercial => RandomService.Next(4, 9) * 100;

        public override int CommercialMax => RandomService.Next(12, 19) * 100;

        public override int Technology => RandomService.Next(0, 4) * 100;

        public override int TechnologyMax => 900;

        public override int Wall => RandomService.Next(1, 6) * 100;

        public override int WallMax => RandomService.Next(18, 25) * 100;

        public override int PeopleMax => 40000;

        public override int People => 14000;

        public override int Security => 50;
      }

      private class FortressTownType : TownTypeDefinition
      {
        public override int Agriculture => RandomService.Next(0, 3) * 100;

        public override int AgricultureMax => RandomService.Next(0, 5) * 100;

        public override int Commercial => RandomService.Next(0, 3) * 100;

        public override int CommercialMax => RandomService.Next(0, 5) * 100;

        public override int Technology => RandomService.Next(1, 5) * 100;

        public override int TechnologyMax => 999;

        public override int Wall => RandomService.Next(4, 13) * 100;

        public override int WallMax => RandomService.Next(26, 49) * 100;

        public override int PeopleMax => 40000;

        public override int People => 4000;

        public override int Security => 70;
      }

      private class LargeTownType : TownTypeDefinition
      {
        public override int Agriculture => RandomService.Next(1, 3) * 100;

        public override int AgricultureMax => RandomService.Next(4, 11) * 100;

        public override int Commercial => RandomService.Next(1, 3) * 100;

        public override int CommercialMax => RandomService.Next(4, 11) * 100;

        public override int Technology => RandomService.Next(1, 7) * 100;

        public override int TechnologyMax => 900;

        public override int Wall => RandomService.Next(8, 15) * 100;

        public override int WallMax => RandomService.Next(12, 21) * 100;

        public override int PeopleMax => 50000;

        public override int People => 20000;

        public override int Security => 35;
      }
    }
  }
}