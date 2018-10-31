using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SangokuKmy.Models.Common.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Filters
{
    /// <summary>
    /// 処理中にエラーがスローされたときに、出力に反映するフィルタ
    /// </summary>
    public class SangokuKmyErrorFilterAttribute: ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {
            base.OnException(context);

            if (!(context.Exception is SangokuKmyException exception))
            {
                exception = new SangokuKmyException(ErrorCode.InternalError);
            }

            try
            {
                context.HttpContext.Response.StatusCode = exception.StatusCode;
                context.Result = new JsonResult(new { code = exception.ErrorCode.Code, data = exception.Data });
            }
            catch
            {
                context.HttpContext.Response.StatusCode = 500;
                context.Result = new JsonResult(new { code = -1, data = default(object) });
            }

            context.ExceptionHandled = true;
        }
    }
}
