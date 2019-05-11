using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Updates.Ai
{
  public class AiCountryFactory
  {
    public static async Task<AiCountry> CreateAsync(MainRepository repo, Country country)
    {
      if (country.AiType != CountryAiType.Managed)
      {
        return new HumanAiCountry(country);
      }

      var management = await repo.AiCountry.GetManagementByCountryIdAsync(country.Id);
      if (!management.HasData)
      {
        return new HumanAiCountry(country);
      }

      return new ManagedAiCountry(country);
    }
  }

  public abstract class AiCountry
  {
    protected Country Country { get; private set; }

    protected AiCountryManagement Management { get; private set; }

    protected SystemData Game { get; private set; }

    public AiCountry(Country country)
    {
      this.Country = country;
    }

    public async Task RunAsync(MainRepository repo)
    {
      var management = await repo.AiCountry.GetManagementByCountryIdAsync(this.Country.Id);
      if (!management.HasData)
      {
        return;
      }
      this.Management = management.Data;

      this.Game = await repo.System.GetAsync();

      await this.RunInnerAsync(repo);
    }

    protected abstract Task RunInnerAsync(MainRepository repo);
  }

  public class HumanAiCountry : AiCountry
  {
    public HumanAiCountry(Country country) : base(country)
    {
    }

    protected override async Task RunInnerAsync(MainRepository repo)
    {
    }
  }
}
