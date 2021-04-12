using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace SangokuKmy.Models.Services
{
  public static class ChatService
  {
    private static bool isUploadingImage = false;

    public static async Task<ChatMessage> PostChatMessageAsync(MainRepository repo, ChatMessage param, Character chara, string ip, ChatMessageType type, uint typeData = default, uint typeData2 = default)
    {
      ChatMessage message;

      var reinforcement = await repo.Reinforcement.GetByCharacterIdAsync(chara.Id);

      message = new ChatMessage
      {
        CharacterId = chara.Id,
        Character = new CharacterChatData(chara, reinforcement.FirstOrDefault(r => r.Status == ReinforcementStatus.Active)),
        CharacterCountryId = chara.CountryId,
        Posted = DateTime.Now,
        Message = param.Message.TrimEnd(),
        IpAddress = ip,
        Type = type,
        TypeData = typeData,
        TypeData2 = typeData2,
        ImageBase64 = param.ImageBase64,
      };
      if (param.CharacterIconId > 0)
      {
        message.CharacterIconId = param.CharacterIconId;
        message.CharacterIcon = await repo.Character.GetCharacterIconByIdAsync(message.CharacterIconId);
      }
      else
      {
        var icons = await repo.Character.GetCharacterAllIconsAsync(chara.Id);
        var icon = icons.FirstOrDefault(i => i.IsMain) ?? icons.FirstOrDefault();
        if (icon == null)
        {
          ErrorCode.CharacterIconNotFoundError.Throw();
        }
        message.CharacterIconId = icon.Id;
        message.CharacterIcon = icon;
      }

      if (message.Type == ChatMessageType.Private || message.Type == ChatMessageType.Promotion || message.Type == ChatMessageType.PromotionRefused || message.Type == ChatMessageType.PromotionAccepted || message.Type == ChatMessageType.PromotionDenied)
      {
        var receiver = await repo.Character.GetByIdAsync(message.TypeData2).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        message.ReceiverName = receiver.Name;
      }
      else if (message.Type == ChatMessageType.OtherCountry)
      {
        var receiver = await repo.Country.GetByIdAsync(message.TypeData2).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        message.ReceiverName = receiver.Name;
      }

      await repo.ChatMessage.AddMessageAsync(message);
      await repo.SaveChangesAsync();

      if (!string.IsNullOrEmpty(message.ImageBase64))
      {
        try
        {
          await Task.Run(async () => await UploadImageAsync(message));
        }
        catch (Exception ex)
        {
          repo.ChatMessage.RemoveMessage(message);
          await repo.SaveChangesAsync();
          ErrorCode.UploadImageFailedError.Throw(ex);
        }
      }

      return message;
    }

    public static async Task DenyCountryPromotions(MainRepository repo, Country country)
    {
      var promotions = (await repo.ChatMessage.GetPromotionMessagesAsync(country.Id)).Where(p => p.Type == ChatMessageType.Promotion);
      foreach (var p in promotions)
      {
        p.Type = ChatMessageType.PromotionDenied;
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(p), p.TypeData);
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(p), p.TypeData2);
      }
    }

    private static async Task UploadImageAsync(ChatMessage message)
    {
      if (isUploadingImage || message.ImageBase64.Length > 10_000_000)
      {
        throw new Exception();
      }
      isUploadingImage = true;

      byte[] binary = null;
      if (message.ImageBase64.StartsWith("http://") || message.ImageBase64.StartsWith("https://"))
      {
        using (var http = new HttpClient())
        {
          binary = await http.GetByteArrayAsync(message.ImageBase64);
        }
      }
      else if (message.ImageBase64.StartsWith("data:image"))
      {
        // data:image/png;base64,iVB
        var strs = new string[] { "data:image/png;base64,", "data:image/jpg;base64,", "data:image/jpeg;base64,", "data:image/gif;base64,", "data:image/bmp;base64,", };
        foreach (var str in strs)
        {
          if (message.ImageBase64.StartsWith(str))
          {
            message.ImageBase64 = message.ImageBase64.Substring(str.Length);
            binary = Convert.FromBase64String(message.ImageBase64);
            break;
          }
        }
      }
      else
      {
        binary = Convert.FromBase64String(message.ImageBase64);
      }

      var imageKey = RandomService.Next(1, 65535);
      var saveFileName = Config.Game.UploadedIconDirectory + $"c_{message.Id}_{imageKey}.png";
      try
      {
        using (Image<Rgba32> image = binary != null ? Image.Load(binary) : Image.Load(message.ImageBase64))
        {
          var width = image.Width;
          var height = image.Height;
          var isResize = false;

          if (width > 1200)
          {
            height = (int)(height * ((float)1200 / width));
            width = 1200;
            isResize = true;
          }
          if (height > 1200)
          {
            width = (int)(width * ((float)1200 / height));
            height = 1200;
            isResize = true;
          }

          if (isResize)
          {
            image.Mutate(x =>
            {
              x.Resize(width, height);
            });
          }

          using (var stream = new FileStream(saveFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
          {
            var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder();
            image.Save(stream, encoder);
          }
        }

        message.ImageKey = imageKey;
        Task.Run(() =>
        {
          Task.Delay(10_000).Wait();
          isUploadingImage = false;
        });
      }
      catch (Exception ex)
      {
        message.ImageKey = 0;
        Task.Run(() =>
        {
          Task.Delay(10_000).Wait();
          isUploadingImage = false;
        });
        throw ex;
      }
    }
  }
}
