using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SangokuKmy.Common;
using SangokuKmy.Filters;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [ServiceFilter(typeof(SangokuKmyErrorFilterAttribute))]
  [AuthenticationFilter]
  public class IssueBbsItemController : Controller, IAuthenticationDataReceiver
  {
    public AuthenticationData AuthData { private get; set; }

    [HttpPost("issue")]
    public async Task<IssueBbsItem> PostAsync(
      [FromBody] IssueBbsItem param)
    {
      IssueBbsItem message;
      Character chara;

      if (string.IsNullOrWhiteSpace(param.Text))
      {
        ErrorCode.LackOfParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var account = await repo.Account.GetByCharacterIdAsync(chara.Id).GetOrErrorAsync(ErrorCode.AccountNotFoundError);
        IssueBbsItem parent = null;

        if (param.ParentId != 0)
        {
          parent = await repo.IssueBbs.GetByIdAsync(param.ParentId).GetOrErrorAsync(ErrorCode.ParentNodeNotFoundError);
          if (parent.ParentId != 0)
          {
            ErrorCode.NotTopNodeError.Throw();
          }
          if (!string.IsNullOrEmpty(param.Title))
          {
            ErrorCode.LackOfParameterError.Throw();
          }
        }
        else
        {
          if (string.IsNullOrEmpty(param.Title))
          {
            ErrorCode.InvalidParameterError.Throw();
          }
          if (param.Title.Length > 40)
          {
            ErrorCode.StringLengthError.Throw(new ErrorCode.RangeErrorParameter("title", param.Title.Length, 1, 40));
          }
        }
        message = new IssueBbsItem
        {
          AccountId = account.Id,
          LastWriterAccountId = account.Id,
          ParentId = param.ParentId,
          Title = param.Title,
          Text = param.Text,
          Written = DateTime.Now,
          LastModified = DateTime.Now,
          Status = IssueStatus.New,
          Category = IssueCategory.New,
          Priority = IssuePriority.Normal,
        };
        await repo.IssueBbs.AddAsync(message);

        if (parent != null)
        {
          parent.LastModified = DateTime.Now;
          parent.LastWriterAccountId = account.Id;
        }

        await repo.SaveChangesAsync();

        message.AccountName = account.Name;
        message.LastWriterAccountName = account.Name;
      }

      await StatusStreaming.Default.SendAllAsync(ApiData.From(message));
      return message;
    }

    [HttpGet("issue")]
    public async Task<IReadOnlyList<IssueBbsItem>> GetThreadsAsync()
    {
      return await this.GetPageAsync(0);
    }

    [HttpGet("issue/page/{page}")]
    public async Task<IReadOnlyList<IssueBbsItem>> GetPageAsync(
      [FromRoute] int page = default)
    {
      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        return await repo.IssueBbs.GetPageThreadsAsync(page, 20);
      }
    }

    [HttpGet("issue/{id}")]
    public async Task<IReadOnlyList<IssueBbsItem>> GetThreadAsync(
      [FromRoute] uint id = default)
    {
      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var parent = await repo.IssueBbs.GetByIdAsync(id).GetOrErrorAsync(ErrorCode.NodeNotFoundError);
        var replies = await repo.IssueBbs.GetRepliesAsync(id);
        return replies.Prepend(parent).ToArray();
      }
    }

    [HttpPatch("issue")]
    public async Task UpdateItemStatusAsync(
      [FromBody] IssueBbsItem param)
    {
      IssueBbsItem message;
      Character chara;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        if (chara.AiType != CharacterAiType.Administrator)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        message = await repo.IssueBbs.GetByIdAsync(param.Id).GetOrErrorAsync(ErrorCode.NodeNotFoundError);

        if (param.Status != IssueStatus.Undefined)
        {
          message.Status = param.Status;
        }
        if (param.Priority != IssuePriority.Undefined)
        {
          message.Priority = param.Priority;
        }
        if (param.Category != IssueCategory.Undefined)
        {
          message.Category = param.Category;
        }

        await repo.SaveChangesAsync();
      }

      await StatusStreaming.Default.SendAllAsync(ApiData.From(message));
    }
  }
}
