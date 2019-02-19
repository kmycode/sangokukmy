using System;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Data.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Repositories
{
  public class HistoryRepository
  {
    private readonly IRepositoryContainer container;

    public HistoryRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    public async Task RecordAndSaveAsync(History history)
    {
      try
      {
        await this.RemoveAsync(history.Period, history.BetaVersion);

        await this.container.Context.Histories.AddAsync(history);
        await this.container.Context.SaveChangesAsync();

        foreach (var chara in history.Characters)
        {
          chara.HistoryId = history.Id;
        }
        await this.container.Context.HistoricalCharacters.AddRangeAsync(history.Characters);
        await this.container.Context.SaveChangesAsync();

        foreach (var chara in history.Characters)
        {
          chara.Icon.CharacterId = chara.Id;
        }
        await this.container.Context.HistoricalCharacterIcons.AddRangeAsync(history.Characters.Select(c => c.Icon));

        foreach (var country in history.Countries)
        {
          country.HistoryId = history.Id;
        }
        await this.container.Context.HistoricalCountries.AddRangeAsync(history.Countries);
        await this.container.Context.SaveChangesAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    private async Task RemoveAsync(int period, int betaVersion)
    {
      try
      {
        var historyOptional = await this.GetAsync(period, betaVersion);
        if (historyOptional.HasData)
        {
          var history = historyOptional.Data;
          this.container.Context.HistoricalCharacterIcons.RemoveRange(history.Characters.Select(c => c.Icon));
          this.container.Context.HistoricalCharacters.RemoveRange(history.Characters);
          this.container.Context.HistoricalCountries.RemoveRange(history.Countries);
          this.container.Context.Histories.Remove(history);
        }
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public async Task<Optional<History>> GetAsync(int period, int betaVersion)
    {
      try
      {
        var history = await this.container.Context.Histories.FirstOrDefaultAsync(h => h.Period == period && h.BetaVersion == betaVersion);
        if (history != null)
        {
          var charaData = await this.container.Context.HistoricalCharacters
            .Where(c => c.HistoryId == history.Id)
            .Join(this.container.Context.HistoricalCharacterIcons, c => c.Id, i => i.CharacterId, (c, i) => new { Character = c, Icon = i, })
            .ToArrayAsync();
          history.Characters = charaData.Select(d =>
          {
            d.Character.Icon = d.Icon;
            return d.Character;
          }).ToArray();
          history.Countries = await this.container.Context.HistoricalCountries
            .Where(c => c.HistoryId == history.Id)
            .ToArrayAsync();
        }

        return history.ToOptional();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }
  }
}
