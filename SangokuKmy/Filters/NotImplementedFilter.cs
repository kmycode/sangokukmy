using System;
using Microsoft.AspNetCore.Mvc.Filters;
using SangokuKmy.Models.Common.Definitions;

namespace SangokuKmy.Filters
{
  public class NotImplementedFilter : ActionFilterAttribute
  {
    public override void OnActionExecuting(ActionExecutingContext context)
    {
      base.OnActionExecuting(context);
      ErrorCode.NotImplementedError.Throw();
    }
  }
}
