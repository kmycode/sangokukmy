using System;
using Microsoft.AspNetCore.Mvc.Filters;
using SangokuKmy.Models.Data;
using System.Threading.Tasks;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Common.Definitions;
using Microsoft.Extensions.Primitives;

namespace SangokuKmy.Filters
{
  public class DebugModeOnlyFilterAttribute : ActionFilterAttribute
  {
    public bool IsDebugCommand { get; set; } = true;

    public override void OnActionExecuting(ActionExecutingContext context)
    {
      base.OnActionExecuting(context);

      using (var repo = MainRepository.WithRead())
      {
        SystemData system = null;
        SystemDebugData debug = null;
        Task.Run(async () =>
        {
          system = await repo.System.GetAsync();
          debug = await repo.System.GetDebugDataAsync();
        }).Wait();

        context.HttpContext.Request.Query.TryGetValue("p", out StringValues password);

        if (!system.IsDebug ||
          (this.IsDebugCommand && !debug.CanUseDebugCommands) ||
          (this.IsDebugCommand && !string.IsNullOrEmpty(debug.DebugPassword) && password.ToString() != debug.DebugPassword))
        {
          ErrorCode.DebugModeOnlyError.Throw();
        }
      }
    }
  }
}
