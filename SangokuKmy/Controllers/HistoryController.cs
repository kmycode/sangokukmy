using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SangokuKmy.Common;
using SangokuKmy.Filters;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [ServiceFilter(typeof(SangokuKmyErrorFilterAttribute))]
  public class HistoryController : Controller
  {
    [HttpGet("histories")]
    public async Task<IEnumerable<History>> GetHistoriesAsync()
    {
      using (var repo = MainRepository.WithRead())
      {
        var histories = await repo.History.GetAllAsync();
        return histories;
      }
    }

    [HttpGet("histories/{id}")]
    public async Task<History> GetHistoryAsync(
      [FromRoute] uint id = 0)
    {
      using (var repo = MainRepository.WithRead())
      {
        var history = await repo.History.GetAsync(id).GetOrErrorAsync(ErrorCode.NodeNotFoundError);
        return history;
      }
    }
  }
}
