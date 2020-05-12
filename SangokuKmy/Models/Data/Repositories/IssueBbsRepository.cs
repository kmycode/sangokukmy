using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Common;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Data.Repositories
{
  public class IssueBbsRepository
  {
    private readonly IRepositoryContainer container;

    public IssueBbsRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    public async Task<Optional<IssueBbsItem>> GetByIdAsync(uint id)
    {
      try
      {
        var item = await this.container.Context.IssueBbsItems
          .Where(ii => ii.Id == id)
          .Join(this.container.Context.Accounts, ii => ii.AccountId, a => a.Id, (ii, a) => new { Item = ii, Account = a, })
          .ToArrayAsync();
        if (!item.Any())
        {
          return default;
        }

        var i = item.First();
        i.Item.AccountName = i.Account.Name;
        return i.Item.ToOptional();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task<IReadOnlyList<IssueBbsItem>> GetRepliesAsync(uint parentId)
    {
      try
      {
        return (await this.container.Context.IssueBbsItems
          .Where(i => i.ParentId == parentId)
          .Join(this.container.Context.Accounts, i => i.AccountId, a => a.Id, (i, a) => new { Item = i, Account = a, })
          .OrderBy(i => i.Item.Written)
          .ToArrayAsync())
          .Select(i =>
          {
            i.Item.AccountName = i.Account.Name;
            return i.Item;
          })
          .ToArray();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task<IReadOnlyList<IssueBbsItem>> GetPageThreadsAsync(int page, int count, short period = -1, short betaVersion = 0, IssueStatus status = IssueStatus.Undefined)
    {
      try
      {
        Expression<Func<IssueBbsItem, bool>> predicate;
        if (period >= 0 && status == IssueStatus.Undefined)
        {
          predicate = item => item.Period == period && item.BetaVersion == betaVersion;
        }
        else if (period < 0 && status != IssueStatus.Undefined)
        {
          predicate = item => item.Status == status;
        }
        else if (period >= 0 && status != IssueStatus.Undefined)
        {
          predicate = item => item.Period == period && item.BetaVersion == betaVersion && item.Status == status;
        }
        else
        {
          predicate = item => true;
        }

        return (await this.container.Context.IssueBbsItems
          .Where(i => i.ParentId == 0)
          .Where(predicate)
          .Join(this.container.Context.Accounts, i => i.AccountId, a => a.Id, (i, a) => new { Issue = i, Account = a, })
          .Join(this.container.Context.Accounts, i => i.Issue.LastWriterAccountId, a => a.Id, (i, a) => new { i.Issue, i.Account, LastWriterAccount = a, })
          .OrderByDescending(i => i.Issue.LastModified)
          .Skip(page * count)
          .Take(count)
          .ToArrayAsync())
          .Select(i =>
          {
            i.Issue.AccountName = i.Account.Name;
            i.Issue.LastWriterAccountName = i.LastWriterAccount.Name;
            return i.Issue;
          })
          .ToArray();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task AddAsync(IssueBbsItem item)
    {
      try
      {
        await this.container.Context.IssueBbsItems.AddAsync(item);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
