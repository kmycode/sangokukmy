using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Common;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Data.Repositories
{
  public class AccountRepository
  {
    private readonly IRepositoryContainer container;

    public AccountRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    public async Task<Optional<Account>> GetByIdAsync(uint id)
    {
      try
      {
        return await this.container.Context.Accounts
          .FindAsync(id)
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task<Optional<Account>> GetByAliasIdAsync(string id)
    {
      try
      {
        return await this.container.Context.Accounts
          .FirstOrDefaultAsync(a => a.AliasId == id)
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task<Optional<Account>> GetByNameAsync(string name)
    {
      try
      {
        return await this.container.Context.Accounts
          .FirstOrDefaultAsync(a => a.Name == name)
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task<Optional<Account>> GetByCharacterIdAsync(uint charaId)
    {
      try
      {
        return await this.container.Context.Accounts.FirstOrDefaultAsync(a => a.CharacterId == charaId).ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task AddAsync(Account account)
    {
      try
      {
        await this.container.Context.Accounts
          .AddAsync(account);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
