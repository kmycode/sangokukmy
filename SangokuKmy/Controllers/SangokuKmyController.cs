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

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [SangokuKmyErrorFilter]
  public class SangokuKmyController : Controller
  {
    [HttpPost("authenticate")]
    public ApiData<AuthenticationData> Authenticate([FromBody] AuthenticateParameter param)
    {
      AuthenticationData authData;
      using (var repo = new MainRepository())
      {
        authData = AuthenticationService.WithIdAndPassword(repo, param.Id, param.Password);
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
  }
}
