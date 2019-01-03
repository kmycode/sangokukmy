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
      unchecked
      {
        using (var repo = MainRepository.WithReadAndWrite())
        {
          foreach (var commandGroup in commands.GroupBy(c => c.Type))
          {
            var cmd = Commands.Get(commandGroup.Key).GetOrError(ErrorCode.CommandTypeNotFoundError);
            var sameCommandParamsGroups = commandGroup.GroupBy(g => g.Parameters.Sum(p => p.GetHashCode()), c => c);
            foreach (var sameCommandParamsGroup in sameCommandParamsGroups)
            {
              await cmd.InputAsync(repo, this.AuthData.CharacterId, sameCommandParamsGroup.Select(c => c.GameDateTime), sameCommandParamsGroup.First().Parameters.ToArray());
            }
          }
          await repo.SaveChangesAsync();
        }
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
          // TODO: 諜報データをあたる

          // 諜報データもなければエラー
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
          // TODO: 諜報データをあたる

          // 諜報データもなければエラー
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
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
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
        var scoutedTown = ScoutedTown.From(town);
        scoutedTown.ScoutedDateTime = system.GameDateTime;
        scoutedTown.ScoutedCharacterId = chara.Id;
        scoutedTown.ScoutedCountryId = chara.CountryId;
        scoutedTown.ScoutMethod = ScoutMethod.Manual;

        await repo.ScoutedTown.AddScoutAsync(scoutedTown);
        await repo.SaveChangesAsync();
      }
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
  }
}
