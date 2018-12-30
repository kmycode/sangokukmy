using System;
using System.Threading.Tasks;
using SangokuKmy.Models.Data.Entities;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Models.Common.Definitions;
using System.Linq;

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
        var data = (await container.Context.SystemData.FirstOrDefaultAsync());
        if (data == null)
        {
          data = SystemData.Initialized;
          await this.container.Context.AddAsync(data);
          await this.container.Context.SaveChangesAsync();
        }
        return data;
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return null;
      }
    }

    public async Task<SystemDebugData> GetDebugDataAsync()
    {
      try
      {
        var data = (await container.Context.SystemDebugData.FirstOrDefaultAsync()) ?? new SystemDebugData();
        return data;
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return null;
      }
    }
  }
}
