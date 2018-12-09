using System;
using Newtonsoft.Json;

namespace SangokuKmy.Models.Data.ApiEntities
{
  public struct ApiDateTime
  {
    [JsonProperty("year")]
    public short Year { get; set; }

    [JsonProperty("month")]
    public short Month { get; set; }

    [JsonProperty("day")]
    public short Day { get; set; }

    [JsonProperty("hours")]
    public short Hours { get; set; }

    [JsonProperty("minutes")]
    public short Minutes { get; set; }

    [JsonProperty("seconds")]
    public short Seconds { get; set; }

    public static ApiDateTime FromDateTime(DateTime dt)
    {
      return new ApiDateTime
      {
        Year = (short)dt.Year,
        Month = (short)dt.Month,
        Day = (short)dt.Day,
        Hours = (short)dt.Hour,
        Minutes = (short)dt.Minute,
        Seconds = (short)dt.Second,
      };
    }

    public DateTime ToDateTime()
    {
      return new DateTime(this.Year, this.Month, this.Day, this.Hours, this.Minutes, this.Seconds);
    }
  }
}
