using System;
using Microsoft.AspNetCore.Mvc.Filters;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Common.Definitions;

namespace SangokuKmy.Filters
{
  public class SecretKeyRequestedFilterAttribute : ActionFilterAttribute
  {
    public override void OnActionExecuting(ActionExecutingContext context)
    {
      base.OnActionExecuting(context);

      string key = context.HttpContext.Request.Headers["SecretKey"];
      if (!string.IsNullOrEmpty(Config.Game.SecretKey))
      {
        if (key != Config.Game.SecretKey)
        {
          ErrorCode.InvalidSecretKeyError.Throw();
        }
      }
    }
  }
}
