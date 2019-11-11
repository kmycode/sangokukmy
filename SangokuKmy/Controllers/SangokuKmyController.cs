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
        await repo.AuthenticationData.RemoveTokenAsync(this.AuthData.AccessToken);
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
      SystemData system;
      Character chara;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        system = await repo.System.GetAsync();

        try
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
        catch (SangokuKmyException ex)
        {
          if (ex.ErrorCode.Code == ErrorCode.LackOfTownTechnologyForSoldier.Code)
          {
            repo.ErrorWithCustomCode(ex);
          }
        }
        catch (Exception ex)
        {
          repo.Error(ex);
        }
      }

      var streamingCommands = commands.Where(c => c.IntGameDateTime <= (system.GameDateTime.Year >= Config.UpdateStartYear ? system.IntGameDateTime : new GameDateTime { Year = Config.UpdateStartYear, Month = 1, }.ToInt()) + 5).ToArray();
      if (streamingCommands.Any())
      {
        await StatusStreaming.Default.SendCountryAsync(streamingCommands.Select(s => ApiData.From(s)), chara.CountryId);
      }
    }

    [AuthenticationFilter]
    [HttpPut("commands/comments")]
    public async Task SetCommandCommentsAsync(
      [FromBody] IReadOnlyList<CommandMessage> comments)
    {
      if (!comments.Any())
      {
        ErrorCode.LackOfParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var system = await repo.System.GetAsync();
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var posts = await repo.Country.GetPostsAsync(chara.CountryId);
        var myPost = posts.FirstOrDefault(p => p.CharacterId == chara.Id);
        if (myPost == null || !myPost.Type.CanCommandComment())
        {
          ErrorCode.NotPermissionError.Throw();
        }

        foreach (var comment in comments)
        {
          comment.CountryId = chara.CountryId;
        }

        repo.CharacterCommand.RemoveOldMessages(system.GameDateTime);
        await repo.CharacterCommand.SetMessagesAsync(comments);
        await repo.SaveChangesAsync();
      }

      foreach (var comment in comments)
      {
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(comment), comment.CountryId);
      }
      await StatusStreaming.Default.SendCountryAsync(ApiData.From(new ApiSignal
      {
        Type = SignalType.CommandCommentUpdated,
      }), comments[0].CountryId);
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
      CharacterIcon mainIcon = null;

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
            mainIcon = icon;
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
      if (mainIcon != null)
      {
        var ac = new CharacterForAnonymous(chara, mainIcon, CharacterShareLevel.Anonymous);
        await StatusStreaming.Default.SendAllAsync(ApiData.From(ac));
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(chara), chara.Id);
      }
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

    [AuthenticationFilter]
    [HttpPut("character/message")]
    public async Task SetMessageAsync(
      [FromBody] Character param)
    {
      Character chara;
      if (param.Message == null)
      {
        ErrorCode.LackOfParameterError.Throw();
      }
      if (param.Message.Length > 128)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("message", param.Message.Length, 0, 128));
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        chara.Message = param.Message;
        await repo.SaveChangesAsync();
      }

      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(chara), chara.Id);
    }

    [HttpPut("formations")]
    [AuthenticationFilter]
    public async Task SetFormationAsync(
      [FromBody] Formation param)
    {
      Character chara;
      CharacterLog log;
      var info = FormationTypeInfoes.Get(param.Type).GetOrError(ErrorCode.InvalidParameterError);
      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        if (chara.FormationType == param.Type)
        {
          ErrorCode.MeaninglessOperationError.Throw();
        }
        if (chara.FormationPoint < 50)
        {
          ErrorCode.InvalidOperationError.Throw();
        }

        var formations = await repo.Character.GetFormationsAsync(chara.Id);
        if (param.Type != FormationType.Normal && !formations.Any(f => f.Type == param.Type))
        {
          ErrorCode.InvalidOperationError.Throw();
        }

        chara.FormationPoint -= 50;
        chara.FormationType = param.Type;

        log = new CharacterLog
        {
          DateTime = DateTime.Now,
          GameDateTime = (await repo.System.GetAsync()).GameDateTime,
          CharacterId = chara.Id,
          Message = $"陣形ポイント <num>50</num> を消費して、陣形を {info.Name} に変更しました",
        };
        await repo.Character.AddCharacterLogAsync(log);

        await repo.SaveChangesAsync();
      }
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(chara), chara.Id);
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(log), chara.Id);
    }

    [HttpPost("items")]
    [AuthenticationFilter]
    public async Task SetItemAsync(
      [FromBody] CharacterItem param)
    {
      Character chara;
      CharacterItem item;
      var info = CharacterItemInfoes.Get(param.Type).GetOrError(ErrorCode.InvalidParameterError);

      if (param.Status != CharacterItemStatus.CharacterHold && param.Status != CharacterItemStatus.TownOnSale)
      {
        ErrorCode.InvalidParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var items = await repo.Character.GetItemsAsync(chara.Id);
        item = items
          .OrderBy(i => i.IntLastStatusChangedGameDate)
          .FirstOrDefault(i => i.Status == CharacterItemStatus.CharacterPending && i.Type == param.Type && (param.Id == default || i.Id == param.Id));
        if (item == null)
        {
          ErrorCode.MeaninglessOperationError.Throw();
        }

        if (param.Status == CharacterItemStatus.CharacterHold)
        {
          var skills = await repo.Character.GetSkillsAsync(chara.Id);
          if (!info.IsResource && CharacterService.CountLimitedItems(items) >= CharacterService.GetItemMax(skills))
          {
            ErrorCode.NotMoreItemsError.Throw();
          }

          await ItemService.SetCharacterAsync(repo, item, chara);
        }
        else if (param.Status == CharacterItemStatus.TownOnSale)
        {
          await ItemService.ReleaseCharacterAsync(repo, item, chara);
        }

        await repo.SaveChangesAsync();
      }
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(chara), chara.Id);
      // await StatusStreaming.Default.SendCharacterAsync(ApiData.From(item), chara.Id);
    }

    [HttpPost("items/all")]
    [AuthenticationFilter]
    public async Task ReceiveAllItemsAsync()
    {
      Character chara;
      List<CharacterItem> changedItems = new List<CharacterItem>();

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var items = await repo.Character.GetItemsAsync(chara.Id);
        var targets = items
          .OrderBy(i => i.IntLastStatusChangedGameDate)
          .Where(i => i.Status == CharacterItemStatus.CharacterPending);

        var skills = await repo.Character.GetSkillsAsync(chara.Id);
        var itemMax = CharacterService.GetItemMax(skills);
        var itemCount = CharacterService.CountLimitedItems(items);

        foreach (var target in targets)
        {
          var infoOptional = target.GetInfo();
          if (!infoOptional.HasData)
          {
            continue;
          }
          var info = infoOptional.Data;

          if (!info.IsResource && itemMax <= itemCount)
          {
            continue;
          }

          await ItemService.SetCharacterAsync(repo, target, chara);
          changedItems.Add(target);
          if (!info.IsResource)
          {
            itemCount++;
          }
        }

        await repo.SaveChangesAsync();
      }
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(chara), chara.Id);
      // await StatusStreaming.Default.SendCharacterAsync(ApiData.From(item), chara.Id);
    }

    [HttpPost("skills")]
    [AuthenticationFilter]
    public async Task AddSkillAsync(
      [FromBody] CharacterSkill param)
    {
      Character chara;
      CharacterSkill skill;
      var info = CharacterSkillInfoes.Get(param.Type).GetOrError(ErrorCode.InvalidParameterError);
      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var skills = await repo.Character.GetSkillsAsync(chara.Id);
        if (skills.Any(s => s.Type == info.Type && s.Status == CharacterSkillStatus.Available))
        {
          ErrorCode.MeaninglessOperationError.Throw();
        }
        if (chara.SkillPoint < info.RequestedPoint)
        {
          ErrorCode.InvalidOperationError.Throw();
        }
        if (info.SubjectAppear != null && !info.SubjectAppear(skills))
        {
          ErrorCode.InvalidOperationError.Throw();
        }

        chara.SkillPoint -= info.RequestedPoint;
        skill = new CharacterSkill
        {
          CharacterId = chara.Id,
          Type = param.Type,
          Status = CharacterSkillStatus.Available,
        };
        await SkillService.SetCharacterAndSaveAsync(repo, skill, chara);
      }
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
          .Where(c => c.Character.CountryId == chara.CountryId || c.Character.AiType != CharacterAiType.SecretaryScouter)
          .Select(c => new CharacterForAnonymous(c.Character, c.Icon, null, c.Character.AiType == CharacterAiType.Human ? c.Commands : null, c.Character.CountryId == chara.CountryId ? CharacterShareLevel.SameTownAndSameCountry : CharacterShareLevel.SameTown));
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
    [HttpGet("town/{townId}/items")]
    public async Task<ApiArrayData<CharacterItem>> GetTownItemsAsync(
      [FromRoute] uint townId)
    {
      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var town = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.TownNotFoundError);

        if (town.CountryId != chara.CountryId && town.Id != chara.TownId)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        return ApiData.From(await repo.Town.GetItemsAsync(townId));
      }
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
        charas = (await repo.Country.GetCharactersWithIconsAndCommandsAsync(countryId))
          .GroupJoin(await repo.Reinforcement.GetByCountryIdAsync(countryId), c => c.Character.Id, r => r.CharacterId, (c, rs) => new { CharacterData = c, Reinforcements = rs })
          .Select(c => new CharacterForAnonymous(c.CharacterData.Character, c.CharacterData.Icon, c.Reinforcements.FirstOrDefault(f => f.Status != ReinforcementStatus.Returned), c.CharacterData.Character.AiType == CharacterAiType.Human ? c.CharacterData.Commands : null, chara.CountryId == c.CharacterData.Character.CountryId ? CharacterShareLevel.SameCountry : CharacterShareLevel.Anonymous));
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
      ChatMessage chat = null;

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
        if (param.Type == CountryMessageType.Unified && !myPosts.CanCountryUnifiedMessage())
        {
          ErrorCode.NotPermissionError.Throw();
        }

        if (param.Type == CountryMessageType.Unified)
        {
          var system = await repo.System.GetAsync();
          if (!system.IsWaitingReset)
          {
            ErrorCode.InvalidOperationError.Throw();
          }
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

        if (message.Type == CountryMessageType.Commanders)
        {
          chat = await ChatService.PostChatMessageAsync(repo, new ChatMessage
          {
            Message = $"[r][s]【指令更新】[-s][-r]\n\n{message.Message}",
          }, chara, ChatMessageType.SelfCountry, chara.CountryId);
        }

        await repo.SaveChangesAsync();
      }

      if (message.Type == CountryMessageType.Commanders || message.Type == CountryMessageType.Solicitation)
      {
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(message), message.CountryId);
      }
      if (message.Type == CountryMessageType.Unified)
      {
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(message), this.AuthData.CharacterId);
      }
      if (message.Type == CountryMessageType.Solicitation)
      {
        await AnonymousStreaming.Default.SendAllAsync(ApiData.From(message));
      }

      if (chat != null && message.Type == CountryMessageType.Commanders)
      {
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(chat), message.CountryId);
      }
    }

    [HttpGet("battle/{id}")]
    public async Task<BattleLog> GetBattleLogAsync(
      [FromRoute] uint id)
    {
      using (var repo = MainRepository.WithRead())
      {
        var log = await repo.BattleLog.GetWithLinesByIdAsync(id).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
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
