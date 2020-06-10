using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SangokuKmy.Common;
using SangokuKmy.Filters;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [ServiceFilter(typeof(SangokuKmyErrorFilterAttribute))]
  [AuthenticationFilter]
  public class AccountController : Controller, IAuthenticationDataReceiver
  {
    public AuthenticationData AuthData { private get; set; }

    [HttpGet("account")]
    public async Task<Account> GetMyAccountAsync()
    {
      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var account = await repo.Account.GetByCharacterIdAsync(chara.Id).GetOrErrorAsync(ErrorCode.AccountNotFoundError);

        return account;
      }
    }

    [HttpPut("account")]
    public async Task<Account> UpdateMyAccountAsync(
      [FromBody] Account data = default)
    {
      if (data == null)
      {
        ErrorCode.LackOfParameterError.Throw();
      }
      if (!string.IsNullOrEmpty(data.AliasId) && (data.AliasId.Length < 4 || data.AliasId.Length > 12))
      {
        ErrorCode.StringLengthError.Throw(new ErrorCode.RangeErrorParameter("aliasId", data.AliasId?.Length ?? 0, 4, 12));
      }
      if (!string.IsNullOrEmpty(data.Password) && (data.Password.Length < 4 || data.Password.Length > 12))
      {
        ErrorCode.StringLengthError.Throw(new ErrorCode.RangeErrorParameter("password", data.Password?.Length ?? 0, 4, 12));
      }
      if (!string.IsNullOrEmpty(data.Name) && data.Name.Length > 12)
      {
        ErrorCode.StringLengthError.Throw(new ErrorCode.RangeErrorParameter("name", data.Name?.Length ?? 0, 1, 12));
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var account = await repo.Account.GetByCharacterIdAsync(chara.Id).GetOrErrorAsync(ErrorCode.AccountNotFoundError);

        if (!string.IsNullOrEmpty(data.AliasId) && data.AliasId != account.AliasId)
        {
          var sameAliasId = await repo.Account.GetByAliasIdAsync(data.AliasId);
          if (sameAliasId.HasData)
          {
            ErrorCode.DuplicateAccountNameOrAliasIdError.Throw();
          }
          account.AliasId = data.AliasId;
        }

        if (!string.IsNullOrEmpty(data.Name) && data.Name != account.Name)
        {
          var sameName = await repo.Account.GetByNameAsync(data.Name);
          if (sameName.HasData)
          {
            ErrorCode.DuplicateAccountNameOrAliasIdError.Throw();
          }
          account.Name = data.Name;
        }

        if (!string.IsNullOrEmpty(data.Password))
        {
          account.SetPassword(data.Password);
        }

        await repo.SaveChangesAsync();

        return account;
      }
    }

    [HttpPost("account/login")]
    public async Task<Account> LoginAsync(
      [FromBody] Account data = default)
    {
      if (data == null)
      {
        ErrorCode.LackOfParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);

        var current = await repo.Account.GetByCharacterIdAsync(chara.Id);
        if (current.HasData)
        {
          ErrorCode.DuplicateAccountOfCharacterError.Throw();
        }

        var account = await repo.Account.GetByAliasIdAsync(data.AliasId).GetOrErrorAsync(ErrorCode.AccountNotFoundError);
        if (!((await repo.Character.GetByIdAsync(account.CharacterId)).Data?.HasRemoved ?? true))
        {
          ErrorCode.DuplicateAccountOfCharacterError.Throw();
        }

        if (!account.TryLogin(data.Password))
        {
          ErrorCode.AccountLoginPasswordIncorrectError.Throw();
        }

        account.CharacterId = chara.Id;
        account.LoginCount++;
        await repo.SaveChangesAsync();

        return account;
      }
    }

    [HttpPost("account")]
    public async Task<Account> CreateAsync(
      [FromBody] Account data = default)
    {
      if (data == null)
      {
        ErrorCode.LackOfParameterError.Throw();
      }
      if (string.IsNullOrEmpty(data.AliasId) || data.AliasId.Length < 4 || data.AliasId.Length > 12)
      {
        ErrorCode.StringLengthError.Throw(new ErrorCode.RangeErrorParameter("aliasId", data.AliasId?.Length ?? 0, 4, 12));
      }
      if (string.IsNullOrEmpty(data.Password) || data.Password.Length < 4 || data.Password.Length > 12)
      {
        ErrorCode.StringLengthError.Throw(new ErrorCode.RangeErrorParameter("password", data.Password?.Length ?? 0, 4, 12));
      }
      if (string.IsNullOrEmpty(data.Name) || data.Name.Length > 12)
      {
        ErrorCode.StringLengthError.Throw(new ErrorCode.RangeErrorParameter("name", data.Name?.Length ?? 0, 1, 12));
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);

        var sameChara = await repo.Account.GetByCharacterIdAsync(chara.Id);
        if (sameChara.HasData)
        {
          ErrorCode.DuplicateAccountOfCharacterError.Throw();
        }

        var sameAliasId = await repo.Account.GetByAliasIdAsync(data.AliasId);
        if (sameAliasId.HasData)
        {
          ErrorCode.DuplicateAccountNameOrAliasIdError.Throw();
        }

        var sameName = await repo.Account.GetByNameAsync(data.Name);
        if (sameName.HasData)
        {
          ErrorCode.DuplicateAccountNameOrAliasIdError.Throw();
        }

        var account = new Account
        {
          AliasId = data.AliasId,
          Name = data.Name,
          CharacterId = chara.Id,
          LoginCount = 1,
        };
        account.SetPassword(data.Password);
        await repo.Account.AddAsync(account);

        await repo.SaveChangesAsync();

        return account;
      }
    }
  }
}
