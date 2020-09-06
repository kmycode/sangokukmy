using System;
using System.Linq;
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
      var characterCount = await repo.Country.CountCharactersAsync(country.Id, true, 200);
      var townTypeCost = (countryOptional.HasData && countryOptional.Data.CapitalTownId == town.Id) ? 12000 :
        town.Type == TownType.Large ? 10000 : 8000;

      var cost = townTypeCost * (Math.Pow(1.06f, townCount) + Math.Pow(1.11f, characterCount)) + town.TakeoverDefensePoint;

      var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
      if (wars.Any(w => w.IsJoinAvailable(town.CountryId) && w.IsJoinAvailable(country.Id)))
      {
        // 自分の国と戦争状態にある場合
        cost *= 3;
      }
      else if (wars.Any(w => w.IsJoin(town.CountryId) && (w.Status == CountryWarStatus.InReady || w.Status == CountryWarStatus.Available || (w.Status == CountryWarStatus.StopRequesting && w.RequestedStopCountryId != town.CountryId))))
      {
        // 他の国と戦争状態にある場合
        // 停戦協議中の場合はコストを増やさない（停戦協議の濫用が最近多いのであえて）
        cost *= 2.1f;
      }

      var alliance = await repo.CountryDiplomacies.GetCountryAllianceAsync(country.Id, town.CountryId);
      if (alliance.HasData && (alliance.Data.Status == CountryAllianceStatus.Available || alliance.Data.Status == CountryAllianceStatus.InBreaking))
      {
        cost *= 2;
      }

      if (town.Religion != ReligionType.Any && town.Religion != ReligionType.None && country.Religion == town.Religion &&
        country.Religion != countryOptional.Data?.Religion)
      {
        cost *= 0.82f;
      }

      return (int)cost;
    }
  }
}
