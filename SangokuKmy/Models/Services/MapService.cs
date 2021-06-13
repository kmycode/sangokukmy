using SangokuKmy.Models.Data.Entities;
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
      "張掖", "武威", "朔方", "雲中", "雁門", "桑乾", "代"  , "上谷", "北平", "遼西",
      "金城", "街亭", "安定", "上"  , "晋陽", "邯鄲", "中山", "易"  , "燕国", "任丘",
      "狄道", "漢陽", "北地", "河西", "上党", "鄴" , "常山", "博陵", "渤海", "広陵",
      "隴西", "咸陽", "高陵", "河南", "河内", "陳留", "清河", "平原", "南皮", "北海",
      "沓中", "武都", "長安", "弘農", "雒陽", "許" , "濮陽", "定陶", "任城", "琅琊",
      "綿竹", "梓潼", "上庸", "鄧" , "宛"  , "潁川", "汝南", "武平", "彭城", "下邳",
      "武陽", "成都", "漢中", "襄陽", "新野", "固始", "合肥", "寿春", "建鄴", "江都",
      "朱堤", "江陽", "永安", "夷陵", "江陵", "江夏", "廬江", "虎林", "呉" , "曲阿",
      "永昌", "建寧", "牂牁", "武陵", "長沙", "武昌", "柴桑", "南昌", "富春", "会稽",
      "雲南", "三江", "合浦", "南海", "零陵", "桂陽", "安城", "延平", "建安", "温州",
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
            X = (short)RandomService.Next(0, 10),
            Y = (short)RandomService.Next(0, 10),
          },
        };

        for (var i = 1; i < townCount; i++)
        {
          var pos = GetNewTownPosition(towns, null, true);
          if (pos.X < 0 || pos.Y < 0)
          {
            i--;
            continue;
          }
          if (towns.Any(t => t.Y == pos.Y && t.X == pos.X - 1) &&
            towns.Any(t => t.Y == pos.Y && t.X == pos.X + 1) &&
            towns.Any(t => t.X == pos.X && t.Y == pos.Y - 1) &&
            towns.Any(t => t.X == pos.X && t.Y == pos.Y + 1))
          {
            if (townCount < 80 && RandomService.Next(0, 5) < 4)
            {
              i--;
              continue;
            }
          }

          towns.Add(new Town
          {
            Id = (uint)i + 1,
            X = pos.X,
            Y = pos.Y,
          });
        }

        if (townCount >= 5)
        {
          var errors = 0;
          foreach (var t in towns)
          {
            var arounds = towns.GetOrderedAroundTowns(t).ToArray();
            if (!arounds.Any())
            {
              errors++;
            }
          }
          if (townCount <= 70)
          {
            isVerify = towns.Count(t => towns.GetAroundTowns(t).Count() >= 6) < townCount / 2 &&
              towns.Count(t => towns.GetAroundTowns(t).Count() >= 8) <= townCount / 12 &&
              errors == 0;
          }
          else
          {
            isVerify = true;
          }

          // 少都市数に応じた条件
          if (isVerify && townCount <= 9 && townCount > 5)
          {
            isVerify = !towns.Any(t => towns.GetAroundTowns(t).Count() == townCount - 1);
          }
        }
        else
        {
          isVerify = true;
        }

        if (isVerify)
        {
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

    public static string GetTownName(short x, short y)
    {
      var index = y * 10 + x;
      if (index >= townNames.Length)
      {
        return string.Empty;
      }
      return townNames[index];
    }

    private static void ShiftMap(IEnumerable<Town> towns, short x, short y)
    {
      foreach (var town in towns)
      {
        town.X += x;
        town.Y += y;
      }
    }

    public static (short X, short Y) GetNewTownPosition(IEnumerable<Town> existTowns, Func<Town, bool> subject, bool isHandouts = false)
    {
      var borderTowns = existTowns
        .Where(t => existTowns.GetAroundTowns(t).Count() <
                    ((t.X < 9 && t.X > 0 && t.Y < 9 && t.Y > 0) ?  8  :
                     (t.X < 9 && t.X > 0) ? 5 :
                     (t.Y < 9 && t.Y > 0) ? 5 : 3) - (isHandouts ? 2 : 0));
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
      .Where(t => !isHandouts || existTowns.GetAroundTowns(t.Item1, t.Item2).Count() < 7)
      .Where(t => !existTowns.Any(tt => tt.X == t.Item1 && tt.Y == t.Item2))
      .ToArray();
      if (!townPositions.Any())
      {
        return (-1, -1);
      }

      var k = existTowns.GetAroundTowns(4, 5);
      var townPosition = townPositions[RandomService.Next(0, townPositions.Length)];

      return (townPosition.Item1, townPosition.Item2);
    }

    public static Town CreateTown(TownType typeId)
    {
      var town = new Town
      {
        Type = typeId,
      };
      UpdateTownType(town, typeId);
      town.RicePrice = 1.0f;

      {
        // 都市施設
        var b = new TownBuilding[]
        {
          TownBuilding.MilitaryStation,
          TownBuilding.OpenWall,
          // TownBuilding.RepairWall,
          TownBuilding.TrainIntellect,
          TownBuilding.TrainLeadership,
          TownBuilding.TrainPopularity,
          TownBuilding.TrainStrong,
          TownBuilding.Houses,
          TownBuilding.Casting,
          TownBuilding.TrainingBuilding,
          TownBuilding.Camp,
          TownBuilding.Extension,
          TownBuilding.Sukiya,
          TownBuilding.School,
        };
        var r = RandomService.Next(0, b.Length);
        town.TownBuilding = b[r];
      }
      return town;
    }

    public static void UpdateTownType(Town town, TownType typeId)
    {
      if (typeId == TownType.Any)
      {
        var r = RandomService.Next(0, 9);
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
        else
        {
          typeId = TownType.Agriculture;
        }
      }
      town.Type = typeId;

      var type = typeId == TownType.Agriculture ? TownTypeDefinition.AgricultureType :
                 typeId == TownType.Commercial ? TownTypeDefinition.CommercialType :
                 typeId == TownType.Fortress ? TownTypeDefinition.FortressType :
                 TownTypeDefinition.LargeType;

      town.Agriculture = type.Agriculture;
      town.AgricultureMax = type.AgricultureMax;
      town.Commercial = type.Commercial;
      town.CommercialMax = type.CommercialMax;
      town.Technology = type.Technology;
      town.TechnologyMax = type.TechnologyMax;
      town.Wall = type.Wall;
      town.WallMax = type.WallMax;
      town.PeopleMax = type.PeopleMax;
      town.People = type.People;
      town.Security = (short)type.Security;
      town.Agriculture = Math.Min(town.Agriculture, town.AgricultureMax);
      town.Commercial = Math.Min(town.Commercial, town.CommercialMax);
      town.Technology = Math.Min(town.Technology, town.TechnologyMax);
      town.Wall = Math.Min(town.Wall, town.WallMax);
      town.People = Math.Min(town.People, town.PeopleMax);
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
        public override int Agriculture => 0;

        public override int AgricultureMax => RandomService.Next(3, 6) * 100;

        public override int Commercial => 0;

        public override int CommercialMax => 100;

        public override int Technology => RandomService.Next(0, 4) * 100;

        public override int TechnologyMax => RandomService.Next(7, 10) * 100;

        public override int Wall => RandomService.Next(1, 4) * 100;

        public override int WallMax => RandomService.Next(10, 20) * 100;

        public override int PeopleMax => 40000;

        public override int People => 12000;

        public override int Security => 50;
      }

      private class CommercialTownType : TownTypeDefinition
      {
        public override int Agriculture => 0;

        public override int AgricultureMax => 100;

        public override int Commercial => 0;

        public override int CommercialMax => RandomService.Next(3, 6) * 100;

        public override int Technology => RandomService.Next(0, 4) * 100;

        public override int TechnologyMax => RandomService.Next(7, 10) * 100;

        public override int Wall => RandomService.Next(1, 4) * 100;

        public override int WallMax => RandomService.Next(10, 20) * 100;

        public override int PeopleMax => 40000;

        public override int People => 12000;

        public override int Security => 50;
      }

      private class FortressTownType : TownTypeDefinition
      {
        public override int Agriculture => 0;

        public override int AgricultureMax => RandomService.Next(2, 4) * 100;

        public override int Commercial => 0;

        public override int CommercialMax => RandomService.Next(2, 4) * 100;

        public override int Technology => RandomService.Next(0, 4) * 100;

        public override int TechnologyMax => 1000;

        public override int Wall => RandomService.Next(1, 5) * 100;

        public override int WallMax => RandomService.Next(18, 29) * 100;

        public override int PeopleMax => 30000;

        public override int People => 0;

        public override int Security => 30;
      }

      private class LargeTownType : TownTypeDefinition
      {
        public override int Agriculture => RandomService.Next(1, 3) * 100;

        public override int AgricultureMax => RandomService.Next(3, 6) * 100;

        public override int Commercial => RandomService.Next(1, 3) * 100;

        public override int CommercialMax => RandomService.Next(3, 6) * 100;

        public override int Technology => RandomService.Next(1, 7) * 100;

        public override int TechnologyMax => 1000;

        public override int Wall => 300;

        public override int WallMax => RandomService.Next(16, 25) * 100;

        public override int PeopleMax => 50000;

        public override int People => RandomService.Next(8, 21) * 1000;

        public override int Security => 50;
      }
    }
  }
}
