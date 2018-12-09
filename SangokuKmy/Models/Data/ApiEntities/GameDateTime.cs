using System;
using Newtonsoft.Json;
namespace SangokuKmy.Models.Data.ApiEntities
{
  public struct GameDateTime
  {
    /// <summary>
    /// 年
    /// </summary>
    [JsonProperty("year")]
    public short Year { get; set; }

    /// <summary>
    /// 月
    /// </summary>
    [JsonProperty("month")]
    public short Month { get; set; }

    public static GameDateTime FromInt(int value)
    {
      var month = value % 12;
      return new GameDateTime
      {
        Year = (short)((value - month) / 12),
        Month = (short)(month + 1),
      };
    }

    public int ToInt()
    {
      return this.Year * 12 + this.Month - 1;
    }
  }
}
