using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
using SangokuKmy.Filters;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Services;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Controllers
{
  [Route("api/v1/debug")]
  [DebugModeOnlyFilter]
  public class DebugController : Controller
  {
    [HttpGet("step/month")]
    public async Task StepNextMonth()
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var debug = await repo.System.GetDebugDataAsync();
        debug.UpdatableLastDateTime = debug.UpdatableLastDateTime.AddSeconds(Config.UpdateTime);
        await repo.SaveChangesAsync();
      }
    }
    [HttpGet("password/{pass}")]
    public object PasswordHash(
      [FromRoute] string pass)
    {
      var chara = new Character();
      chara.SetPassword(pass);
      return new { password = pass, hash = chara.PasswordHash, };
    }

    [HttpGet("reset/request")]
    public async Task RequestReset()
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        await ResetService.RequestResetAsync(repo);
        await repo.SaveChangesAsync();
      }
    }

    [HttpGet("reset/run")]
    public async Task Reset()
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        await ResetService.ResetAsync(repo);
        await repo.SaveChangesAsync();
      }
    }

    [HttpGet("reset/record")]
    public async Task ResetRecord()
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        //await ResetService.RecordHistoryAsync(repo, await repo.System.GetAsync());
        await repo.SaveChangesAsync();
      }
    }

    [HttpGet("reset/map/{num}")]
    public string CreateNewMap([FromRoute] int num = 7)
    {
      var towns = MapService.CreateMap(num);
      var str = "";
      for (var y = 0; y < 10; y++)
      {
        for (var x = 0; x < 10; x++)
        {
          var town = towns.FirstOrDefault(t => t.X == x && t.Y == y);
          if (town == null)
          {
            str += "|     ";
          }
          else
          {
            str += "|==";
          }
        }
        str += "|\n";
      }
      return str;
    }
  }
}
