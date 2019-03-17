using System;
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
  }
}
