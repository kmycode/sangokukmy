using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SangokuKmy.Models.Common.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Data.ApiEntities;
using Microsoft.Extensions.Logging;

namespace SangokuKmy.Filters
{
  /// <summary>
  /// 処理中にエラーがスローされたときに、出力に反映するフィルタ
  /// </summary>
  public class SangokuKmyErrorFilterAttribute: ExceptionFilterAttribute
  {
    private ILogger logger;

    public SangokuKmyErrorFilterAttribute(ILogger<SangokuKmyErrorFilterAttribute> logger)
    {
      this.logger = logger;
    }

    public override void OnException(ExceptionContext context)
    {
      base.OnException(context);

      if (!(context.Exception is SangokuKmyException exception))
      {
        exception = new SangokuKmyException(new Exception(), ErrorCode.InternalError);
      }

      this.logger.LogError(exception, $"{context.HttpContext.Request.Method} [{(context.HttpContext.Request.Path.HasValue ? context.HttpContext.Request.Path.Value : "no-path")}]");

      try
      {
        context.HttpContext.Response.StatusCode = exception.StatusCode;
        context.Result = new JsonResult(ApiData.From(new ApiError { Code = exception.ErrorCode.Code, Data = exception.AdditionalData }));
      }
      catch
      {
        context.HttpContext.Response.StatusCode = 500;
        context.Result = new JsonResult(ApiData.From(new ApiError { Code = -1, Data = default(object) }));
      }

      context.ExceptionHandled = true;
    }
  }
}
