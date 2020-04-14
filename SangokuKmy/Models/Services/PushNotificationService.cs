using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PushSharp.Apple;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Services
{
  public static class PushNotificationService
  {
    public static async Task TestSend(MainRepository repo, SystemData d, ILogger logger)
    {
      try
      {
        var push = new ApnsServiceBroker(new ApnsConfiguration(
          ApnsConfiguration.ApnsServerEnvironment.Sandbox,
          "/home/sangokukmy/push_notification_development.p12",
          "test"));
        push.OnNotificationFailed += (sender, e) =>
        {

        };
        push.Start();

        var keys = await repo.PushNotificationKey.GetAllAsync();
        foreach (var key in keys)
        {
          push.QueueNotification(new ApnsNotification
          {
            DeviceToken = key.Key,
            Payload = JObject.Parse(@"{""aps"":{""alert"":{""title"":""リセット"",""body"":""ゲームはリセットされ、第 " + d.Period + @" が始まりました。新規登録しましょう。""},""badge"":7}}"),
          });
        }

        push.Stop();
      }
      catch (Exception ex)
      {
        logger.LogError(ex, "プッシュ通知で例外が発生しました");
      }
    }
  }
}
