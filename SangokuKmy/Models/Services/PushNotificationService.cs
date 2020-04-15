using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using PushSharp.Apple;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Services
{
  public static class PushNotificationService
  {
    public static ILogger Logger { get; set; }

    public static Task SendAllAsync(MainRepository repo, string title, string message)
      => SendAsync(repo, title, message, k => true);

    public static Task SendCharacterAsync(MainRepository repo, string title, string message, uint charaId)
      => SendAsync(repo, title, message, k => k.CharacterId == charaId);

    public static Task SendCharactersAsync(MainRepository repo, string title, string message, IEnumerable<uint> charaIds)
      => SendAsync(repo, title, message, k => charaIds.Contains(k.CharacterId));

    public static async Task SendCountryAsync(MainRepository repo, string title, string message, uint countryId)
    {
      var charas = await repo.Country.GetCharactersAsync(countryId);
      await SendCharactersAsync(repo, title, message, charas.Select(c => c.Id));
    }

    private static async Task SendAsync(MainRepository repo, string title, string message, Predicate<PushNotificationKey> predicate)
    {
      try
      {
        var push = new ApnsServiceBroker(new ApnsConfiguration(
          ApnsConfiguration.ApnsServerEnvironment.Production,
          "/home/sangokukmy/push_notification_product.p12",
          "test"));
        push.OnNotificationFailed += (sender, e) =>
        {
          Logger.LogError(e, "プッシュ通知送信時にエラーが発生しました");
        };
        push.Start();

        var keys = await repo.PushNotificationKey.GetAllAsync();
        foreach (var key in keys.Where(k => k.Platform == PushNotificationPlatform.iOS && predicate(k)))
        {
          push.QueueNotification(new ApnsNotification
          {
            DeviceToken = key.Key,
            Payload = JObject.Parse(@"{""aps"":{""alert"":{""title"":""" + title + @""",""body"":""" + message + @"""},""badge"":1}}"),
          });
        }

        push.Stop();
      }
      catch (Exception ex)
      {
        Logger?.LogError(ex, "プッシュ通知で例外が発生しました");
      }
    }
  }
}
