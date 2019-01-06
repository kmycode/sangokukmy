﻿using System;
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

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [SangokuKmyErrorFilter]
  public class SangokuKmyController : Controller, IAuthenticationDataReceiver
  {
    public AuthenticationData AuthData { private get; set; }

    [HttpPost("authenticate")]
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
    [HttpGet("commands")]
    public async Task<GetCharacterAllCommandsResponse> GetCharacterAllCommandsAsync()
    {
      IEnumerable<CharacterCommand> commands;
      int secondsNextCommand;
      using (var repo = MainRepository.WithRead())
      {
        var system = await repo.System.GetAsync();
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var isCurrentMonthExecuted = chara.LastUpdated >= system.CurrentMonthStartDateTime;
        var firstMonth = isCurrentMonthExecuted ? system.GameDateTime.NextMonth() : system.GameDateTime;
        commands = await repo.CharacterCommand.GetAllAsync(this.AuthData.CharacterId, firstMonth);

        // 次のコマンドが実行されるまでの秒数
        secondsNextCommand = (int)((isCurrentMonthExecuted ? chara.LastUpdated.AddSeconds(Config.UpdateTime) : chara.LastUpdated) - DateTime.Now).TotalSeconds;
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

        charas = (await repo.Town.GetCharactersAsync(townId))
          .Select(c => new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.SameTown));
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
        charas = (await repo.Country.GetCharactersAsync(countryId))
          .Select(c => new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.Anonymous));
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
    [HttpPut("country/{targetId}/alliance")]
    public async Task SetCountryAllianceAsync(
      [FromRoute] uint targetId,
      [FromBody] CountryAlliance param)
    {
      CountryAlliance alliance;
      MapLog mapLog = null;

      if ((short)param.Status < 0)
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

        var target = await repo.Country.GetByIdAsync(targetId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);

        var old = await repo.CountryDiplomacies.GetCountryAlliancesAsync(self.CountryId, targetId);
        old.Some((o) =>
        {
          if (o.Status != CountryAllianceStatus.Broken)
          {
            if ((param.Status == CountryAllianceStatus.Available || param.Status == CountryAllianceStatus.ChangeRequesting) &&
                 o.Status == CountryAllianceStatus.Requesting)
            {
              if (self.CountryId == o.RequestedCountryId)
              {
                // 自分で自分の要求を承認しようとした
                ErrorCode.NotPermissionError.Throw();
              }
            }
            else if (param.Status == CountryAllianceStatus.Broken)
            {
              // 破棄はシステムで自動処理するので、ユーザが設定しようとしたらエラー（設定できるのは破棄猶予）
              ErrorCode.NotPermissionError.Throw();
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
          }
          else
          {
            if (param.Status != CountryAllianceStatus.Requesting)
            {
              // 同盟を結んでいない場合、同盟の要求以外にできることはない
              ErrorCode.NotPermissionError.Throw();
            }
          }
        });
        old.None(() =>
        {
          if (param.Status != CountryAllianceStatus.Requesting)
          {
            // 同盟を結んでいない場合、同盟の要求以外にできることはない
            ErrorCode.NotPermissionError.Throw();
          }
        });

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
        if (alliance.IsPublic)
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

        await repo.SaveChangesAsync();
      }

      // 同盟関係を周りに通知
      if (alliance.IsPublic)
      {
        await StatusStreaming.Default.SendAllAsync(ApiData.From(alliance));
        await StatusStreaming.Default.SendAllAsync(ApiData.From(mapLog));
      }
      else
      {
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(alliance), alliance.RequestedCountryId);
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(alliance), alliance.InsistedCountryId);
      }
    }
  }
}
