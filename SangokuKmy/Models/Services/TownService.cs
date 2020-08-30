using System;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Services
{
  public static class TownService
  {
    public async static Task<int> GetTownBuyCostAsync(MainRepository repo, Town town, Country country)
    {
      var countryOptional = await repo.Country.GetAliveByIdAsync(town.CountryId);

      var townCount = await repo.Town.CountByCountryIdAsync(country.Id);
      var characterCount = await repo.Country.CountCharactersAsync(country.Id, true);
      var townTypeCost = (countryOptional.HasData && countryOptional.Data.CapitalTownId == town.Id) ? 50000 :
        town.Type == TownType.Large ? 25000 : 15000;

      return (int)(townTypeCost * Math.Pow(1.03f, townCount) * Math.Pow(1.12f, characterCount) + town.TakeoverDefensePoint);
    }
  }
}
