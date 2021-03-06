﻿using System;
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
    public short Month
    {
      get => this._month;
      set
      {
        if (value > 12)
        {
          this._month = (short)((value - 1) % 12 + 1);
          this.Year += (short)((value - 1) / 12);
        }
        else
        {
          this._month = value;
        }
      }
    }
    private short _month;

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

    public GameDateTime NextMonth() => this.AddMonth(1);

    public GameDateTime PrevMonth() => this.AddMonth(-1);

    public GameDateTime AddMonth(int add)
    {
      var year = this.Year;
      var month = this.Month + add;
      while (month > 12)
      {
        month -= 12;
        year++;
      }
      while (month < 1)
      {
        month += 12;
        year--;
      }
      return new GameDateTime { Year = year, Month = (short)month, };
    }

    public override string ToString()
    {
      return $"{this.Year}年{this.Month}月";
    }
  }
}
