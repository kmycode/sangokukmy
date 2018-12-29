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
    public async Task<ApiArrayData<CharacterCommand>> GetCharacterAllCommandsAsync()
    {
      IEnumerable<CharacterCommand> commands;
      using (var repo = MainRepository.WithRead())
      {
        commands = await repo.CharacterCommand.GetAllAsync(this.AuthData.CharacterId);
      }
      return ApiData.From(commands);
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
          var cmd = Commands.Get(commandGroup.Key);
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
}
