using System;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Data.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;
namespace SangokuKmy.Models.Data.Repositories
{
  public class AiCountryRepository
  {
    private readonly IRepositoryContainer container;

    public AiCountryRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// 国IDからログを取得する
    /// </summary>
    /// <returns>ログ</returns>
    /// <param name="id">ID</param>
    public async Task<Optional<AiCountryStorategy>> GetStorategyByCountryIdAsync(uint id)
    {
      try
      {
        return await this.container.Context.AiCountryStorategies
          .FirstOrDefaultAsync(d => d.CountryId == id)
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 国IDからログを取得する
    /// </summary>
    /// <returns>ログ</returns>
    /// <param name="id">ID</param>
    public async Task<Optional<AiCountryManagement>> GetManagementByCountryIdAsync(uint id)
    {
      try
      {
        return await this.container.Context.AiCountryManagements
          .FirstOrDefaultAsync(d => d.CountryId == id)
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 追加する
    /// </summary>
    /// <param name="storategy">作戦</param>
    public async Task AddAsync(AiCountryStorategy storategy)
    {
      try
      {
        await this.container.Context.AiCountryStorategies.AddAsync(storategy);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 追加する
    /// </summary>
    /// <param name="storategy">作戦</param>
    public async Task AddAsync(AiCountryManagement management)
    {
      try
      {
        await this.container.Context.AiCountryManagements.AddAsync(management);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 削除する
    /// </summary>
    /// <param name="id">国ID</param>
    public void ResetStorategyByCountryId(uint id)
    {
      try
      {
        this.container.Context.AiCountryStorategies.RemoveRange(this.container.Context.AiCountryStorategies.Where(d => d.CountryId == id));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 内容をすべてリセットする
    /// </summary>
    public async Task ResetAsync()
    {
      try
      {
        await this.container.RemoveAllRowsAsync(typeof(AiCountryStorategy));
        await this.container.RemoveAllRowsAsync(typeof(AiCountryManagement));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
