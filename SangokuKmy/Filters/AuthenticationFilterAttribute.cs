using Microsoft.AspNetCore.Mvc.Filters;
using SangokuKmy.Models.Common.Definitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Controllers;
using SangokuKmy.Common;

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
        string authentication = context.HttpContext.Request.Headers["Authorization"];
        if (!string.IsNullOrEmpty(authentication))
        {
          var authData = authentication.Split(' ');
          if (authData.Length == 2 && authData[0] == "Bearer")
          {
            token = authData[1];
          }
        }
      }
      if (string.IsNullOrEmpty(token))
      {
        ErrorCode.LoginTokenEmptyError.Throw();
      }

      // アクセストークンを保存済のデータと照合する
      using (var repo = MainRepository.WithRead())
      {
        AuthenticationData authData = null;
        Task.Run(async () => {
          authData = (await repo.AuthenticationData
            .FindByTokenAsync(token))
            .GetOrError(ErrorCode.LoginTokenIncorrectError);
        }).Wait();

        // 認証データをコントローラに設定
        if (context.Controller is IAuthenticationDataReceiver receiver)
        {
          receiver.AuthData = authData;
        }
      }
    }
  }
}
