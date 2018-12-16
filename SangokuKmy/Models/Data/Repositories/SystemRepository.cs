using System;
using System.Threading.Tasks;
using SangokuKmy.Models.Data.Entities;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Models.Common.Definitions;

namespace SangokuKmy.Models.Data.Repositories
{
  public class SystemRepository
  {
    private readonly IRepositoryContainer container;

    public SystemRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    public async Task<SystemData> GetAsync()
    {
      try
      {
        return (await this.container.Context.SystemData.FirstOrDefaultAsync()) ?? new SystemData();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return null;
      }
    }
  }
}
