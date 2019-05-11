using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SangokuKmy.Models.Services
{
  public static class RandomService
  {
    private static readonly Random rand = new Random(DateTime.Now.Millisecond);

    public static int Next(int min, int max)
    {
      return rand.Next(min, max);
    }

    public static int Next(int min)
    {
      return rand.Next(min);
    }

    public static int Next()
    {
      return rand.Next();
    }

    public static double NextDouble()
    {
      return rand.NextDouble();
    }

    public static T Next<T>(T[] array)
    {
      if (array.Length <= 0)
      {
        return default;
      }
      return array[Next(0, array.Length)];
    }

    public static T Next<T>(IEnumerable<T> enumerable)
    {
      if (!enumerable.Any())
      {
        return default;
      }
      return enumerable.ElementAt(Next(0, enumerable.Count()));
    }
  }
}
