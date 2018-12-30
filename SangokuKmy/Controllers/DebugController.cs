﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
using SangokuKmy.Filters;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Common;

namespace SangokuKmy.Controllers
{
#if DEBUG
  [Route("api/v1/debug")]
  [DebugModeOnlyFilter]
  public class DebugController : Controller
  {
    [HttpGet("step/month")]
    public async Task StepNextMonth()
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var debug = await repo.System.GetDebugDataAsync();
        debug.UpdatableLastDateTime = debug.UpdatableLastDateTime.AddSeconds(Config.UpdateTime);
        await repo.SaveChangesAsync();
      }
    }
  }
#endif
}
