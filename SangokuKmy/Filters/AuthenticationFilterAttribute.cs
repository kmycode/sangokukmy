using Microsoft.AspNetCore.Mvc.Filters;
using SangokuKmy.Models.Common.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Filters
{
  public class AuthenticationFilterAttribute : ActionFilterAttribute
  {
    /// <summary>
    /// トークンに要求されるスコープ
    /// </summary>
    public Scope Scope { get; set; } = Scope.All;

    public AuthenticationFilterAttribute()
    {
      this.Order = 1;
    }

    public override void OnActionExecuting(ActionExecutingContext context)
    {
      base.OnActionExecuting(context);

      // 参考:https://qiita.com/uasi/items/cfb60588daa18c2ec6f5

      // アクセストークン解析
      var token = string.Empty;
      {
        var authentication = context.HttpContext.Request.Headers["Authorization"];
        if (!string.IsNullOrEmpty(authentication))
        {
          var authData = ((string)authentication).Split(' ');
          if (authData.Length == 2 && authData[0] == "Bearer")
          {
            token = authData[1];
          }
        }
      }
      if (string.IsNullOrEmpty(token))
      {

      }

      // アクセストークンの中身を調べる
    }
  }
}
