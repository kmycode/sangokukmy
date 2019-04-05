using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Services;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Filters;
using Newtonsoft.Json;
using System.Collections;
using SangokuKmy.Models.Commands;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Common;
using SangokuKmy.Models.Common;
using SangokuKmy.Streamings;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [ServiceFilter(typeof(SangokuKmyErrorFilterAttribute))]
  public class SangokuKmyController : Controller, IAuthenticationDataReceiver
  {
    private readonly ILogger _logger;
    public AuthenticationData AuthData { private get; set; }

    public SangokuKmyController(ILogger<SangokuKmyController> logger)
    {
      this._logger = logger;
    }

    [HttpPost("authenticate")]
    [SecretKeyRequestedFilter]
    public async Task<ApiData<AuthenticationData>> AuthenticateAsync
      ([FromBody] AuthenticateParameter param)
    {
      AuthenticationData authData;
      using (var repo = MainRepository.WithReadAndWrite())
      {
        authData = await AuthenticationService.WithIdAndPasswordAsync(repo, param.Id, param.Password);
      }
      return ApiData.From(authData);
    }

    public struct AuthenticateParameter
    {
      [JsonProperty("id")]
      public string Id { get; set; }
      [JsonProperty("password")]
      public string Password { get; set; }
    }

    [AuthenticationFilter]
    [HttpPost("logout")]
    public async Task LogoutAsync()
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        await repo.AuthenticationData.RemoveCharacterAsync(this.AuthData.CharacterId);
        StatusStreaming.Default.Disconnect(chara);
        await OnlineService.SetAsync(chara, OnlineStatus.Offline);
      }
    }

    [AuthenticationFilter]
    [HttpGet("character")]
    public async Task<ApiData<Character>> GetMyCharacterAsync()
    {
      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var icon = (await repo.Character.GetCharacterAllIconsAsync(chara.Id)).GetMainOrFirst().Data;
        chara.MainIcon = icon;
        return ApiData.From(chara);
      }
    }

    [AuthenticationFilter]
    [HttpGet("character/{id}")]
    public async Task<ApiData<CharacterForAnonymous>> GetCharacterAsync(
      [FromRoute] uint id = default)
    {
      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(id).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        return ApiData.From(new CharacterForAnonymous(chara, null, CharacterShareLevel.Anonymous));
      }
    }

    [AuthenticationFilter]
    [HttpGet("commands")]
    public async Task<GetCharacterAllCommandsResponse> GetCharacterAllCommandsAsync(
      [FromQuery] string months = "")
    {
      IEnumerable<CharacterCommand> commands;
      int secondsNextCommand;

      List<GameDateTime> mons = null;
      if (!string.IsNullOrEmpty(months))
      {
        var numTexts = months.Split(',');
        var nums = new List<GameDateTime>();
        foreach (var text in numTexts)
        {
          if (int.TryParse(text, out var value))
          {
            nums.Add(GameDateTime.FromInt(value));
          }
        }
        mons = nums;
      }

      using (var repo = MainRepository.WithRead())
      {
        var system = await repo.System.GetAsync();
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var isCurrentMonthExecuted = chara.LastUpdated >= system.CurrentMonthStartDateTime;
        var firstMonth = isCurrentMonthExecuted ? system.GameDateTime.NextMonth() : system.GameDateTime;

        if (mons != null && mons.Any())
        {
          commands = await repo.CharacterCommand.GetAsync(this.AuthData.CharacterId, mons);
        }
        else
        {
          commands = await repo.CharacterCommand.GetAllAsync(this.AuthData.CharacterId, firstMonth);
        }

        // 次のコマンドが実行されるまでの秒数
        secondsNextCommand = (int)(chara.LastUpdated.AddSeconds(Config.UpdateTime) - DateTime.Now).TotalSeconds;
      }
      return new GetCharacterAllCommandsResponse
      {
        Commands = commands,
        SecondsNextCommand = secondsNextCommand,
      };
    }

    public struct GetCharacterAllCommandsResponse
    {
      [JsonProperty("commands")]
      public IEnumerable<CharacterCommand> Commands { get; set; }
      [JsonProperty("secondsNextCommand")]
      public int SecondsNextCommand { get; set; }
    }

    [AuthenticationFilter]
    [HttpPut("commands")]
    public async Task SetCharacterCommandsAsync(
      [FromBody] IReadOnlyList<CharacterCommand> commands)
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        foreach (var commandGroup in commands.GroupBy(c => c.Type))
        {
          var cmd = Commands.Get(commandGroup.Key).GetOrError(ErrorCode.CommandTypeNotFoundError);
          var sameCommandParamsGroups = commandGroup.GroupBy(g => g.Parameters.Any() ? g.Parameters.Select(p => p.GetHashCode()).Aggregate((p1, p2) => p1 ^ p2) : 0, c => c);
          foreach (var sameCommandParamsGroup in sameCommandParamsGroups)
          {
            await cmd.InputAsync(repo, this.AuthData.CharacterId, sameCommandParamsGroup.Select(c => c.GameDateTime), sameCommandParamsGroup.First().Parameters.ToArray());
          }
        }
        await repo.SaveChangesAsync();
      }
    }

    [AuthenticationFilter]
    [HttpGet("icons")]
    public async Task<ApiArrayData<CharacterIcon>> GetCharacterAllIconsAsync()
    {
      IEnumerable<CharacterIcon> icons;
      using (var repo = MainRepository.WithRead())
      {
        icons = await repo.Character.GetCharacterAllIconsAsync(this.AuthData.CharacterId);
      }
      return ApiData.From(icons);
    }

    [AuthenticationFilter]
    [HttpDelete("icons/{id}")]
    public async Task DeleteCharacterIconAsync(
      [FromRoute] uint id = 0)
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var icon = await repo.Character.GetCharacterIconByIdAsync(id).Or(ErrorCode.CharacterIconNotFoundError);
        if (icon.CharacterId != this.AuthData.CharacterId)
        {
          ErrorCode.NotPermissionError.Throw();
        }
        if (icon.IsMain)
        {
          ErrorCode.InvalidOperationError.Throw();
        }
        icon.IsAvailable = false;
        await repo.SaveChangesAsync();
      }
    }

    [AuthenticationFilter]
    [HttpPut("icons/{id}/main")]
    public async Task SetMainIconAsync(
      [FromRoute] uint id = 0)
    {
      Character chara;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var icons = await repo.Character.GetCharacterAllIconsAsync(this.AuthData.CharacterId);
        var isHit = false;
        foreach (var icon in icons)
        {
          if (icon.Id != id)
          {
            icon.IsMain = false;
          }
          else
          {
            icon.IsMain = true;
            isHit = true;
          }
        }
        if (!isHit)
        {
          ErrorCode.CharacterIconNotFoundError.Throw();
        }
        await repo.SaveChangesAsync();
      }

      await OnlineService.SetAsync(chara, OnlineStatus.Active);
    }

    [AuthenticationFilter]
    [HttpPost("icons")]
    public async Task<CharacterIcon> AddNewCharacterIconAsync(
      [FromForm(Name = "type")] string typeIdText,
      [FromForm] string fileName = null,
      [FromForm] List<IFormFile> files = null)
    {
      if (!int.TryParse(typeIdText, out int typeId))
      {
        ErrorCode.InvalidParameterError.Throw();
      }

      var type = (CharacterIconType)typeId;
      string ext = null;
      var icon = new CharacterIcon
      {
        Type = type,
        IsAvailable = true,
        CharacterId = this.AuthData.CharacterId,
        FileName = fileName,
      };
      if (type == CharacterIconType.Default || type == CharacterIconType.Gravatar)
      {
        if (string.IsNullOrEmpty(fileName))
        {
          ErrorCode.LackOfParameterError.Throw();
        }
      }
      else if (type == CharacterIconType.Uploaded)
      {
        if (files == null || !files.Any())
        {
          ErrorCode.LackOfParameterError.Throw();
        }
        if (files.Count > 1 || files.Any(f => f.Length <= 0) || files.Any(f => f.Length > 1_000_000)
           || string.IsNullOrWhiteSpace(files[0].FileName) || !files[0].FileName.Contains('.'))
        {
          ErrorCode.InvalidParameterError.Throw();
        }
        ext = Path.GetExtension(files[0].FileName).ToLower();
        if (ext != ".jpg" && ext != ".jpeg" && ext != ".png" && ext != ".gif")
        {
          ErrorCode.InvalidParameterError.Throw();
        }
      }
      else
      {
        ErrorCode.InvalidParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        await repo.Character.AddCharacterIconAsync(icon);
        await repo.SaveChangesAsync();

        if (type == CharacterIconType.Uploaded)
        {
          var tmpFileName = Config.Game.UploadedIconDirectory + $"{icon.Id}_tmp{ext}";
          var saveFileName = Config.Game.UploadedIconDirectory + $"{icon.Id}.png";
          try
          {
            using (var stream = new FileStream(tmpFileName, FileMode.Create))
            {
              await files[0].CopyToAsync(stream);
            }
            using (Image<Rgba32> image = Image.Load(tmpFileName))
            {
              var cropX = 0;
              var cropY = 0;
              var cropSize = 0;
              if (image.Width < image.Height)
              {
                cropY = (image.Height - image.Width) / 2;
                cropSize = image.Width;
              }
              else if (image.Height < image.Width)
              {
                cropX = (image.Width - image.Height) / 2;
                cropSize = image.Height;
              }

              image.Mutate(x =>
              {
                if (cropSize > 0)
                {
                  x.Crop(new Rectangle(cropX, cropY, cropSize, cropSize));
                }
                x.Resize(128, 128);
              });

              using (var stream = new FileStream(saveFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
              {
                var encoder = new SixLabors.ImageSharp.Formats.Png.PngEncoder();
                image.Save(stream, encoder);
              }
            }

            icon.FileName = $"{icon.Id}.png";
            await repo.SaveChangesAsync();
          }
          catch
          {
            icon.IsAvailable = false;
            await repo.SaveChangesAsync();
          }
          finally
          {
            try
            {
              System.IO.File.Delete(tmpFileName);
            }
            catch (Exception ex)
            {
              this._logger.LogError(ex, "アップロードされた一時ファイルの削除に失敗");
            }
          }
        }
      }

      return icon;
    }

    [HttpGet("characters")]
    public async Task<ApiArrayData<CharacterForAnonymous>> GetAllCharactersAsync()
    {
      IEnumerable<CharacterForAnonymous> charas;
      using (var repo = MainRepository.WithRead())
      {
        charas = (await repo.Character.GetAllAliveWithIconAsync())
          .Select(c => new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.AllCharacterList));
      }
      return ApiData.From(charas);
    }

    [AuthenticationFilter]
    [HttpGet("character/log")]
    public async Task<ApiArrayData<CharacterLog>> GetCharacterLogAsync(
      [FromQuery] uint since = default,
      [FromQuery] int count = default)
    {
      IEnumerable<CharacterLog> logs;
      using (var repo = MainRepository.WithRead())
      {
        logs = await repo.Character.GetCharacterLogsAsync(this.AuthData.CharacterId, since, count);
      }
      return ApiData.From(logs);
    }

    [HttpGet("countries")]
    public async Task<ApiArrayData<CountryForAnonymous>> GetAllCountriesAsync()
    {
      IEnumerable<CountryForAnonymous> countries;
      using (var repo = MainRepository.WithRead())
      {
        countries = (await repo.Country.GetAllForAnonymousAsync());
      }
      return ApiData.From(countries);
    }

    [AuthenticationFilter]
    [HttpGet("town/{townId}/characters")]
    public async Task<ApiArrayData<CharacterForAnonymous>> GetTownCharactersAsync(
      [FromRoute] uint townId)
    {
      IEnumerable<CharacterForAnonymous> charas;
      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var town = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.TownNotFoundError);

        if (town.CountryId != chara.CountryId && town.Id != chara.TownId)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        charas = (await repo.Town.GetCharactersWithIconAsync(townId))
          .Select(c => new CharacterForAnonymous(c.Character, c.Icon, null, c.Commands, c.CustomSoldierType.Data, c.Character.CountryId == chara.CountryId ? CharacterShareLevel.SameTownAndSameCountry : CharacterShareLevel.SameTown));
      }
      return ApiData.From(charas);
    }

    [AuthenticationFilter]
    [HttpGet("town/{townId}/defenders")]
    public async Task<ApiArrayData<CharacterForAnonymous>> GetTownDefendersAsync(
      [FromRoute] uint townId)
    {
      IEnumerable<CharacterForAnonymous> charas;
      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var town = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.TownNotFoundError);

        if (town.CountryId != chara.CountryId && town.Id != chara.TownId)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        charas = (await repo.Town.GetDefendersAsync(townId))
          .Select(c => new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.SameTown));
      }
      return ApiData.From(charas);
    }

    [AuthenticationFilter]
    [HttpPost("town/scout")]
    public async Task ScoutTownAsync()
    {
      ScoutedTown scoutedTown, savedScoutedTown;
      Character chara;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var town = await repo.Town.GetByIdAsync(chara.TownId).GetOrErrorAsync(ErrorCode.TownNotFoundError);
        var country = await repo.Country.GetByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.InternalDataNotFoundError, new { type = "country", value = chara.CountryId, });

        if (town.CountryId == chara.CountryId)
        {
          ErrorCode.MeaninglessOperationError.Throw();
        }
        if (country.HasOverthrown)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var system = await repo.System.GetAsync();
        scoutedTown = ScoutedTown.From(town);
        scoutedTown.ScoutedDateTime = system.GameDateTime;
        scoutedTown.ScoutedCharacterId = chara.Id;
        scoutedTown.ScoutedCountryId = chara.CountryId;
        scoutedTown.ScoutMethod = ScoutMethod.Manual;

        await repo.ScoutedTown.AddScoutAsync(scoutedTown);
        await repo.SaveChangesAsync();
        
        savedScoutedTown = (await repo.ScoutedTown.GetByTownIdAsync(town.Id, chara.CountryId))
          .GetOrError(ErrorCode.InternalError, new { type = "already-scouted-town", value = scoutedTown.Id, });
      }

      await StatusStreaming.Default.SendCountryAsync(ApiData.From(savedScoutedTown), chara.CountryId);
    }

    [AuthenticationFilter]
    [HttpGet("country/{countryId}/characters")]
    public async Task<ApiArrayData<CharacterForAnonymous>> GetCountryCharactersAsync(
      [FromRoute] uint countryId)
    {
      IEnumerable<CharacterForAnonymous> charas;
      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        charas = (await repo.Country.GetCharactersAsync(countryId))
          .GroupJoin(await repo.Reinforcement.GetByCountryIdAsync(countryId), c => c.Character.Id, r => r.CharacterId, (c, rs) => new { CharacterData = c, Reinforcements = rs })
          .Select(c => new CharacterForAnonymous(c.CharacterData.Character, c.CharacterData.Icon, c.Reinforcements.FirstOrDefault(), c.CharacterData.Commands, chara.CountryId == c.CharacterData.Character.CountryId ? CharacterShareLevel.SameCountry : CharacterShareLevel.Anonymous));
      }
      return ApiData.From(charas);
    }

    [AuthenticationFilter]
    [HttpPut("country/posts")]
    public async Task SetCountryPostAsync(
      [FromBody] CountryPost param)
    {
      CountryPost post;
      using (var repo = MainRepository.WithReadAndWrite())
      {
        if (this.AuthData.CharacterId == param.CharacterId || param.Type == CountryPostType.Monarch)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var self = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var posts = await repo.Country.GetPostsAsync(self.CountryId);
        var myPost = posts.FirstOrDefault(p => p.CharacterId == self.Id);
        var targetPost = posts.FirstOrDefault(p => p.CharacterId == param.CharacterId);
        if (myPost == null || !myPost.Type.CanAppoint() || targetPost?.Type == CountryPostType.Monarch)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var target = await repo.Character.GetByIdAsync(param.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        if (target.CountryId != self.CountryId)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        post = new CountryPost
        {
          CountryId = self.CountryId,
          CharacterId = target.Id,
          Type = param.Type,
          Character = new CharacterForAnonymous(target, null, CharacterShareLevel.Anonymous),
        };
        await repo.Country.SetPostAsync(post);

        await repo.SaveChangesAsync();
      }
      await StatusStreaming.Default.SendAllAsync(ApiData.From(post));
    }

    [AuthenticationFilter]
    [HttpPut("country/messages")]
    public async Task SetCountryMessageAsync(
      [FromBody] CountryMessage param)
    {
      CountryMessage message;

      if (param.Type == CountryMessageType.Solicitation && param.Message?.Length > 200)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("message", param.Message.Length, 1, 200));
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var country = await repo.Country.GetAliveByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var posts = await repo.Country.GetPostsAsync(chara.CountryId);
        var myPosts = posts.Where(p => p.CharacterId == chara.Id);
        if (!myPosts.CanCountrySetting())
        {
          ErrorCode.NotPermissionError.Throw();
        }
        if (param.Type != CountryMessageType.Commanders && !myPosts.CanCountrySettingExceptForCommands())
        {
          ErrorCode.NotPermissionError.Throw();
        }

        CharacterIcon icon;
        if (param.WriterIconId > 0)
        {
          icon = await repo.Character.GetCharacterIconByIdAsync(param.WriterIconId);
          if (icon == null)
          {
            ErrorCode.CharacterIconNotFoundError.Throw();
          }
        }
        else
        {
          icon = (await repo.Character.GetCharacterAllIconsAsync(chara.Id)).GetMainOrFirst().GetOrError(ErrorCode.CharacterIconNotFoundError);
        }

        var old = await repo.Country.GetMessageAsync(chara.CountryId, param.Type);
        if (old.HasData)
        {
          message = old.Data;
        }
        else
        {
          message = new CountryMessage
          {
            CountryId = chara.CountryId,
            Type = param.Type,
          };
          await repo.Country.SetMessageAsync(message);
        }

        message.Message = param.Message;
        message.WriterPost = myPosts.GetTopmostPost().Data.Type;
        message.WriterCharacterId = chara.Id;
        message.WriterCharacterName = chara.Name;
        message.WriterIconId = icon.Id;
        message.WriterIcon = icon;

        await repo.SaveChangesAsync();
      }

      await StatusStreaming.Default.SendCountryAsync(ApiData.From(message), message.CountryId);
      if (message.Type == CountryMessageType.Solicitation)
      {
        await AnonymousStreaming.Default.SendAllAsync(ApiData.From(message));
      }
    }

    [AuthenticationFilter]
    [HttpPut("country/{targetId}/alliance")]
    public async Task SetCountryAllianceAsync(
      [FromRoute] uint targetId,
      [FromBody] CountryAlliance param)
    {
      CountryAlliance alliance;
      MapLog mapLog = null;

      if (param.Status != CountryAllianceStatus.Available &&
          param.Status != CountryAllianceStatus.ChangeRequesting &&
          param.Status != CountryAllianceStatus.Dismissed &&
          param.Status != CountryAllianceStatus.InBreaking &&
          param.Status != CountryAllianceStatus.Requesting &&
          param.Status != CountryAllianceStatus.None)
      {
        ErrorCode.InvalidParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var self = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var posts = await repo.Country.GetPostsAsync(self.CountryId);
        var myPost = posts.FirstOrDefault(p => p.CharacterId == self.Id);
        if (myPost == null || !myPost.Type.CanDiplomacy())
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var target = await repo.Country.GetAliveByIdAsync(targetId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        
        var old = await repo.CountryDiplomacies.GetCountryAllianceAsync(self.CountryId, targetId);
        var war = await repo.CountryDiplomacies.GetCountryWarAsync(self.CountryId, targetId);
        if (old.HasData &&
          old.Data.Status != CountryAllianceStatus.Broken &&
          old.Data.Status != CountryAllianceStatus.Dismissed &&
          old.Data.Status != CountryAllianceStatus.None)
        {
          var o = old.Data;
          
          if ((param.Status == CountryAllianceStatus.Available || param.Status == CountryAllianceStatus.ChangeRequesting) &&
                o.Status == CountryAllianceStatus.Requesting)
          {
            if (self.CountryId == o.RequestedCountryId)
            {
              // 自分で自分の要求を承認しようとした
              ErrorCode.NotPermissionError.Throw();
            }
          }
          else if (param.Status == CountryAllianceStatus.InBreaking &&
                    (o.Status != CountryAllianceStatus.Available && o.Status != CountryAllianceStatus.ChangeRequesting))
          {
            // 結んでいないのに破棄はエラー
            ErrorCode.NotPermissionError.Throw();
          }
          else if ((param.Status == CountryAllianceStatus.None || param.Status == CountryAllianceStatus.Requesting) &&
                    (o.Status == CountryAllianceStatus.Available || o.Status == CountryAllianceStatus.ChangeRequesting))
          {
            // 結んでいるものを一瞬でなかったことにするのはエラー
            ErrorCode.NotPermissionError.Throw();
          }
          else if (param.Status == o.Status && param.Status == CountryAllianceStatus.Available)
          {
            // 再承認はできない
            ErrorCode.MeaninglessOperationError.Throw();
          }

          if (param.Status == CountryAllianceStatus.Available)
          {
            param.BreakingDelay = o.BreakingDelay;
            param.IsPublic = o.IsPublic;
          }
        }
        else
        {
          if (param.Status != CountryAllianceStatus.Requesting)
          {
            // 同盟を結んでいない場合、同盟の要求以外にできることはない
            ErrorCode.NotPermissionError.Throw();
          }

          war.Some((w) =>
          {
            if (w.Status == CountryWarStatus.Available ||
                w.Status == CountryWarStatus.InReady ||
                w.Status == CountryWarStatus.StopRequesting)
            {
              // 戦争中
              ErrorCode.NotPermissionError.Throw();
            }
          });
        }

        alliance = new CountryAlliance
        {
          RequestedCountryId = self.CountryId,
          InsistedCountryId = targetId,
          BreakingDelay = param.BreakingDelay,
          IsPublic = param.IsPublic,
          Status = param.Status,
          NewBreakingDelay = param.NewBreakingDelay,
        };
        await repo.CountryDiplomacies.SetAllianceAsync(alliance);

        // 同盟関係を周りに通知
        if (alliance.IsPublic && old.HasData)
        {
          if (old.Data.Status == CountryAllianceStatus.Requesting &&
              alliance.Status == CountryAllianceStatus.Available)
          {
            var country1 = await repo.Country.GetByIdAsync(alliance.RequestedCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
            var country2 = await repo.Country.GetByIdAsync(alliance.InsistedCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
            mapLog = new MapLog
            {
              ApiGameDateTime = (await repo.System.GetAsync()).GameDateTime,
              Date = DateTime.Now,
              EventType = EventType.AllianceStart,
              IsImportant = true,
              Message = "<country>" + country1.Name + "</country> と <country>" + country2.Name + "</country> は、同盟を締結しました",
            };
            await repo.MapLog.AddAsync(mapLog);
          }
        }

        await repo.SaveChangesAsync();
      }

      // 同盟関係を周りに通知
      if (alliance.IsPublic)
      {
        await StatusStreaming.Default.SendAllAsync(ApiData.From(alliance));
        if (mapLog != null)
        {
          await StatusStreaming.Default.SendAllAsync(ApiData.From(mapLog));
          await AnonymousStreaming.Default.SendAllAsync(ApiData.From(mapLog));
        }
      }
      else
      {
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(alliance), alliance.RequestedCountryId);
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(alliance), alliance.InsistedCountryId);
      }
    }

    [AuthenticationFilter]
    [HttpPut("country/{targetId}/war")]
    public async Task SetCountryWarAsync(
      [FromRoute] uint targetId,
      [FromBody] CountryWar param)
    {
      CountryWar war;
      Optional<CountryAlliance> alliance;
      MapLog mapLog = null;

      if (param.Status != CountryWarStatus.InReady /* &&
          param.Status != CountryWarStatus.StopRequesting &&
          param.Status != CountryWarStatus.Stoped */ )
      {
        ErrorCode.InvalidParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var system = await repo.System.GetAsync();
        var self = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var posts = await repo.Country.GetPostsAsync(self.CountryId);
        var myPost = posts.FirstOrDefault(p => p.CharacterId == self.Id);
        if (myPost == null || !myPost.Type.CanDiplomacy())
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var target = await repo.Country.GetAliveByIdAsync(targetId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);

        var old = await repo.CountryDiplomacies.GetCountryWarAsync(self.CountryId, targetId);
        alliance = await repo.CountryDiplomacies.GetCountryAllianceAsync(self.CountryId, targetId);
        old.Some((o) =>
        {
          if ((o.Status == CountryWarStatus.InReady || o.Status == CountryWarStatus.Available) &&
              param.Status == CountryWarStatus.InReady)
          {
            // 重複して宣戦布告はできない
            ErrorCode.MeaninglessOperationError.Throw();
          }
          else if (o.Status == CountryWarStatus.StopRequesting && param.Status == CountryWarStatus.Stoped &&
                   o.RequestedCountryId == self.CountryId)
          {
            // 自分の停戦要求を自分で承認できない
            ErrorCode.NotPermissionError.Throw();
          }
          else if (o.Status == CountryWarStatus.Stoped && param.Status == CountryWarStatus.StopRequesting)
          {
            // 一度決まった停戦を撤回できない
            ErrorCode.NotPermissionError.Throw();
          }

          if (o.Status == CountryWarStatus.Stoped && param.Status == CountryWarStatus.InReady)
          {
            if (param.StartGameDate.ToInt() < system.IntGameDateTime + 12 * 12 + 1)
            {
              // 開戦が早すぎる
              ErrorCode.InvalidParameterError.Throw();
            }
            else if (param.StartGameDate.ToInt() > system.IntGameDateTime + 12 * 48)
            {
              // 開戦が遅すぎる
              ErrorCode.InvalidParameterError.Throw();
            }
          }
          else
          {
            param.RequestedCountryId = o.RequestedCountryId;
            param.InsistedCountryId = o.InsistedCountryId;
            param.StartGameDate = o.StartGameDate;
          }
        });
        old.None(() =>
        {
          if (param.Status == CountryWarStatus.StopRequesting || param.Status == CountryWarStatus.Stoped)
          {
            // 存在しない戦争を停戦にはできない
            ErrorCode.NotPermissionError.Throw();
          }
          else if (param.StartGameDate.ToInt() < system.IntGameDateTime + 12 * 12 + 1)
          {
            // 開戦が早すぎる
            ErrorCode.InvalidParameterError.Throw();
          }
          else if (param.StartGameDate.ToInt() > system.IntGameDateTime + 12 * 48)
          {
            // 開戦が遅すぎる
            ErrorCode.InvalidParameterError.Throw();
          }

          alliance.Some((a) =>
          {
            if (a.Status == CountryAllianceStatus.Available ||
                a.Status == CountryAllianceStatus.ChangeRequesting ||
                a.Status == CountryAllianceStatus.InBreaking)
            {
              // 同盟が有効中
              ErrorCode.NotPermissionError.Throw();
            }
            if (a.Status == CountryAllianceStatus.Requesting)
            {
              // 自動で同盟申請を却下する
              a.Status = CountryAllianceStatus.Broken;
            }
          });
        });

        war = new CountryWar
        {
          RequestedCountryId = param.RequestedCountryId,
          InsistedCountryId = param.InsistedCountryId,
          StartGameDate = param.StartGameDate,
          Status = param.Status,
          RequestedStopCountryId = param.RequestedStopCountryId,
        };
        await repo.CountryDiplomacies.SetWarAsync(war);

        // 戦争を周りに通知
        var country1 = await repo.Country.GetAliveByIdAsync(war.RequestedCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var country2 = await repo.Country.GetAliveByIdAsync(war.InsistedCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        mapLog = new MapLog
        {
          ApiGameDateTime = (await repo.System.GetAsync()).GameDateTime,
          Date = DateTime.Now,
          EventType = EventType.WarInReady,
          IsImportant = true,
          Message = "<country>" + country1.Name + "</country> は、<date>" + war.StartGameDate.ToString() + "</date> より <country>" + country2.Name + "</country> へ侵攻します",
        };
        await repo.MapLog.AddAsync(mapLog);

        await repo.SaveChangesAsync();
      }

      await StatusStreaming.Default.SendAllAsync(ApiData.From(war));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(mapLog));
      await AnonymousStreaming.Default.SendAllAsync(ApiData.From(mapLog));

      if (alliance.HasData)
      {
        var a = alliance.Data;
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(a), a.RequestedCountryId);
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(a), a.InsistedCountryId);
      }
    }

    [AuthenticationFilter]
    [HttpPut("town/{townId}/war")]
    public async Task SetTownWarAsync(
      [FromRoute] uint townId)
    {
      TownWar war;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var system = await repo.System.GetAsync();
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var posts = await repo.Country.GetPostsAsync(chara.CountryId);
        var myPost = posts.FirstOrDefault(p => p.CharacterId == chara.Id);
        if (myPost == null || !myPost.Type.CanDiplomacy())
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var country = await repo.Country.GetAliveByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var targetTown = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.TownNotFoundError);
        var targetCountry = await repo.Country.GetAliveByIdAsync(targetTown.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);

        if (await repo.Town.CountByCountryIdAsync(targetCountry.Id) <= 1)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var alliance = await repo.CountryDiplomacies.GetCountryAllianceAsync(country.Id, targetCountry.Id);
        var countryWar = await repo.CountryDiplomacies.GetCountryWarAsync(country.Id, targetCountry.Id);
        if (alliance.HasData)
        {
          if (alliance.Data.Status == CountryAllianceStatus.Available ||
              alliance.Data.Status == CountryAllianceStatus.InBreaking ||
              alliance.Data.Status == CountryAllianceStatus.ChangeRequesting ||
              alliance.Data.Status == CountryAllianceStatus.Requesting)
          {
            ErrorCode.NotPermissionError.Throw();
          }
        }
        if (countryWar.HasData)
        {
          if (countryWar.Data.Status == CountryWarStatus.Available ||
              countryWar.Data.Status == CountryWarStatus.StopRequesting)
          {
            ErrorCode.MeaninglessOperationError.Throw();
          }
          if (countryWar.Data.Status == CountryWarStatus.InReady)
          {
            if (system.IntGameDateTime > countryWar.Data.IntStartGameDate - 6)
            {
              ErrorCode.NotPermissionError.Throw();
            }
          }
        }

        var olds = (await repo.CountryDiplomacies.GetAllTownWarsAsync())
          .Where(o => o.RequestedCountryId == country.Id);
        if (olds.Any(o => o.Status == TownWarStatus.Available || o.Status == TownWarStatus.InReady))
        {
          // 重複して宣戦布告はできない
          ErrorCode.MeaninglessOperationError.Throw();
        }
        else if (olds.Any(o => o.Status == TownWarStatus.Terminated &&
                               system.IntGameDateTime - o.IntGameDate < 12 * 10))
        {
          ErrorCode.NotPermissionError.Throw();
        }

        war = new TownWar
        {
          RequestedCountryId = country.Id,
          InsistedCountryId = targetCountry.Id,
          IntGameDate = system.IntGameDateTime + 1,
          TownId = townId,
          Status = TownWarStatus.InReady,
        };
        await repo.CountryDiplomacies.SetTownWarAsync(war);

        await repo.SaveChangesAsync();
      }

      await StatusStreaming.Default.SendCountryAsync(ApiData.From(war), war.RequestedCountryId);
    }

    [AuthenticationFilter]
    [HttpGet("units")]
    public async Task<IEnumerable<Unit>> GetCountryUnitsAsync()
    {
      using (var repo = MainRepository.WithRead())
      {
        var character = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        var country = await repo.Country.GetAliveByIdAsync(character.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        return await repo.Unit.GetByCountryIdAsync(country.Id);
      }
    }

    [AuthenticationFilter]
    [HttpGet("units/{id}")]
    public async Task<Unit> GetUnitAsync(
      [FromRoute] uint id = default)
    {
      using (var repo = MainRepository.WithRead())
      {
        var character = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        var unit = await repo.Unit.GetByIdAsync(id).GetOrErrorAsync(ErrorCode.UnitNotFoundError);
        if (unit.CountryId != character.CountryId)
        {
          ErrorCode.NotPermissionError.Throw();
        }
        return unit;
      }
    }

    [AuthenticationFilter]
    [HttpPost("unit")]
    public async Task CreateUnitAsync(
      [FromBody] Unit param)
    {
      if (param == null)
      {
        ErrorCode.LackOfParameterError.Throw();
      }

      if (string.IsNullOrEmpty(param.Name) || param.Name.Length > 24)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("name", param.Name.Length, 1, 24));
      }

      if (param.Message?.Length > 240)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("message", param.Message.Length, 1, 240));
      }

      var unit = new Unit
      {
        Name = param.Name,
        IsLimited = false,
        Message = param.Message,
      };
      if (string.IsNullOrEmpty(unit.Name))
      {
        ErrorCode.LackOfNameParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var character = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        var country = await repo.Country.GetAliveByIdAsync(character.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);

        var old = await repo.Unit.GetByMemberIdAsync(character.Id);
        if (old.Member.HasData)
        {
          var oldMember = old.Member.Data;
          if (oldMember.Post == UnitMemberPostType.Leader)
          {
            // 隊長は２つ以上の部隊を持てない
            ErrorCode.InvalidOperationError.Throw();
          }
        }

        unit.CountryId = character.CountryId;
        await repo.Unit.AddAsync(unit);
        await repo.SaveChangesAsync();

        await repo.Unit.SetMemberAsync(new UnitMember
        {
          CharacterId = character.Id,
          Post = UnitMemberPostType.Leader,
          UnitId = unit.Id,
        });
        await repo.SaveChangesAsync();
      }
    }

    [AuthenticationFilter]
    [HttpPost("unit/{id}/join")]
    public async Task SetUnitMemberAsync(
      [FromRoute] uint id)
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var character = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        var country = await repo.Country.GetAliveByIdAsync(character.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var unit = await repo.Unit.GetByIdAsync(id).GetOrErrorAsync(ErrorCode.UnitNotFoundError);

        if (unit.CountryId != character.CountryId)
        {
          // 違う国の部隊には入れない
          ErrorCode.NotPermissionError.Throw();
        }
        if (unit.IsLimited)
        {
          // 入隊制限がかかっている
          ErrorCode.UnitJoinLimitedError.Throw();
        }
        
        var old = await repo.Unit.GetByMemberIdAsync(character.Id);
        if (old.Member.HasData)
        {
          var oldMember = old.Member.Data;
          if (oldMember.Post == UnitMemberPostType.Leader)
          {
            // 隊長は部隊から脱げられない
            ErrorCode.InvalidOperationError.Throw();
          }
          if (oldMember.UnitId == unit.Id)
          {
            ErrorCode.MeaninglessOperationError.Throw();
          }
        }

        await repo.Unit.SetMemberAsync(new UnitMember
        {
          CharacterId = character.Id,
          Post = UnitMemberPostType.Normal,
          UnitId = unit.Id,
        });
        await repo.SaveChangesAsync();
      }
    }

    [AuthenticationFilter]
    [HttpPost("unit/leave")]
    public async Task LeaveUnitMemberAsync()
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var old = await repo.Unit.GetByMemberIdAsync(this.AuthData.CharacterId);
        if (old.Member.HasData)
        {
          var oldMember = old.Member.Data;
          if (oldMember.Post == UnitMemberPostType.Leader)
          {
            // 隊長は部隊から脱げられない
            ErrorCode.InvalidOperationError.Throw();
          }
        }
        else
        {
          ErrorCode.UnitNotFoundError.Throw();
        }

        UnitService.Leave(repo, this.AuthData.CharacterId);
        await repo.SaveChangesAsync();
      }
    }

    [AuthenticationFilter]
    [HttpPut("unit/{id}")]
    public async Task UpdateUnitAsync(
      [FromRoute] uint id,
      [FromBody] Unit unit)
    {
      if (unit == null)
      {
        ErrorCode.LackOfParameterError.Throw();
      }

      if (string.IsNullOrEmpty(unit.Name) || unit.Name.Length > 24)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("name", unit.Name.Length, 1, 24));
      }

      if (unit.Message?.Length > 240)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("message", unit.Message.Length, 1, 240));
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var member = await repo.Unit.GetByMemberIdAsync(this.AuthData.CharacterId);
        var oldUnit = member.Unit.Data;
        if (member.Unit.HasData && member.Member.HasData)
        {
          var unitMember = member.Member.Data;
          if (unitMember.Post != UnitMemberPostType.Leader)
          {
            ErrorCode.NotPermissionError.Throw();
          }
          if (oldUnit.Id != id)
          {
            ErrorCode.NotPermissionError.Throw();
          }
        }
        else
        {
          ErrorCode.UnitNotFoundError.Throw();
        }

        oldUnit.Message = unit.Message;
        oldUnit.Name = unit.Name;
        oldUnit.IsLimited = unit.IsLimited;
        await repo.SaveChangesAsync();
      }
    }

    [AuthenticationFilter]
    [HttpDelete("unit/{id}")]
    public async Task RemoveUnitAsync(
      [FromRoute] uint id)
    {
      IEnumerable<UnitMember> members = null;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var member = await repo.Unit.GetByMemberIdAsync(this.AuthData.CharacterId);
        if (member.Unit.HasData && member.Member.HasData)
        {
          var unitMember = member.Member.Data;
          var unit = member.Unit.Data;
          if (unitMember.Post != UnitMemberPostType.Leader)
          {
            ErrorCode.NotPermissionError.Throw();
          }
          if (unit.Id != id)
          {
            ErrorCode.NotPermissionError.Throw();
          }
        }
        else
        {
          ErrorCode.UnitNotFoundError.Throw();
        }

        await UnitService.RemoveAsync(repo, id);
        await repo.SaveChangesAsync();
      }
    }

    [HttpGet("battle/{id}")]
    public async Task<BattleLog> GetBattleLogAsync(
      [FromRoute] uint id)
    {
      using (var repo = MainRepository.WithRead())
      {
        var log = await repo.BattleLog.GetWithLinesByIdAsync(id).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        if (log.AttackerCache.SoldierType == SoldierType.Custom)
        {
          var type = await repo.CharacterSoldierType.GetByIdAsync(log.AttackerCache.CharacterSoldierTypeId);
          log.AttackerCache.CharacterSoldierType = type.Data;
        }
        if (log.DefenderCache.SoldierType == SoldierType.Custom)
        {
          var type = await repo.CharacterSoldierType.GetByIdAsync(log.DefenderCache.CharacterSoldierTypeId);
          log.DefenderCache.CharacterSoldierType = type.Data;
        }
        return log;
      }
    }

    [HttpGet("maplog")]
    public async Task<IEnumerable<MapLog>> GetMapLogsAsync(
      [FromQuery] uint since = uint.MaxValue,
      [FromQuery] int count = 50)
    {
      using (var repo = MainRepository.WithRead())
      {
        var logs = await repo.MapLog.GetRangeAsync(since, count);
        return logs;
      }
    }
  }
}
