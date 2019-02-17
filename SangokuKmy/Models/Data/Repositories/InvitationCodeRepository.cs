using System;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace SangokuKmy.Models.Data.Repositories
{
  public class InvitationCodeRepository
  {
    private readonly IRepositoryContainer container;

    public InvitationCodeRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// 招待コードをコードから取得する
    /// </summary>
    /// <returns>コード</returns>
    /// <param name="code">コードの文字列</param>
    public async Task<Optional<InvitationCode>> GetByCodeAsync(string code)
    {
      try
      {
        return await this.container.Context.InvitationCodes.FirstOrDefaultAsync(ic => ic.Code == code).ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }
  }
}
