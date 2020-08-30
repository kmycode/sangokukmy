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

      var cost = townTypeCost * Math.Pow(1.03f, townCount) * Math.Pow(1.12f, characterCount) + town.TakeoverDefensePoint;

      var war = await repo.CountryDiplomacies.GetCountryWarAsync(country.Id, town.CountryId);
      if (war.HasData && (war.Data.Status == CountryWarStatus.Available || war.Data.Status == CountryWarStatus.InReady || war.Data.Status == CountryWarStatus.StopRequesting))
      {
        cost *= 3;
      }

      var alliance = await repo.CountryDiplomacies.GetCountryAllianceAsync(country.Id, town.CountryId);
      if (alliance.HasData && (alliance.Data.Status == CountryAllianceStatus.Available || alliance.Data.Status == CountryAllianceStatus.InBreaking))
      {
        cost *= 2;
      }

      return (int)cost;
    }
  }
}
