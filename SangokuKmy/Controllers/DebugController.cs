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
using PushSharp.Apple;
using Newtonsoft.Json.Linq;

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

    [HttpGet("step/month/{m}")]
    public async Task StepNextDay(int m)
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var debug = await repo.System.GetDebugDataAsync();
        debug.UpdatableLastDateTime = debug.UpdatableLastDateTime.AddSeconds(Config.UpdateTime * m);
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
        var countries = await repo.Country.GetAllAsync();
        await ResetService.RequestResetAsync(repo, countries.First(c => !c.HasOverthrown).Id);
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
      num = Math.Min(30, Math.Max(1, num));

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

    [HttpGet("notify")]
    public async Task Notification()
    {
      var push = new ApnsServiceBroker(new ApnsConfiguration(
        ApnsConfiguration.ApnsServerEnvironment.Production,
        "/Users/kmy/Develop/_iOSCerts/push_notification_product.p12",
        "test"));
      push.OnNotificationFailed += (sender, e) =>
      {
        var s = e.ToString();
      };
      push.Start();

      using (var repo = MainRepository.WithRead())
      {
        var keys = await repo.PushNotificationKey.GetAllAsync();
        foreach (var key in keys)
        {
          push.QueueNotification(new ApnsNotification
          {
            DeviceToken = key.Key,
            Payload = JObject.Parse(@"{""aps"":{""alert"":{""title"":""放置削除ターンが進んでいます"",""body"":""コマンドを入力しないと、あなたの武将データは約 2 日後に削除されます""},""badge"":7}}"),
          });
        }
      }

      push.Stop();
    }

    [HttpGet("notify2")]
    public async Task Notification2()
    {
      using (var repo = MainRepository.WithRead())
      {
        var admins = await repo.Character.GetAdministratorsAsync();
        await PushNotificationService.SendCharactersAsync(repo, "通知テスト", "これは通知のテストです", admins.Select(c => c.Id));
      }
    }
  }
}
