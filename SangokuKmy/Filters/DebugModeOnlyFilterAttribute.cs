using System;
using Microsoft.AspNetCore.Mvc.Filters;
using SangokuKmy.Models.Data;
using System.Threading.Tasks;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Common.Definitions;

namespace SangokuKmy.Filters
{
  public class DebugModeOnlyFilterAttribute : ActionFilterAttribute
  {
    public override void OnActionExecuting(ActionExecutingContext context)
    {
      base.OnActionExecuting(context);

      using (var repo = MainRepository.WithRead())
      {
        SystemData system = null;
        Task.Run(async () => system = await repo.System.GetAsync()).Wait();
        if (!system.IsDebug)
        {
          ErrorCode.DebugModeOnlyError.Throw();
        }
      }
    }
  }
}
