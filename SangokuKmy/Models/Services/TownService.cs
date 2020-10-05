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
      var system = await repo.System.GetAsync();
      var countryOptional = await repo.Country.GetAliveByIdAsync(town.CountryId);

      var reinforcements = (await repo.Reinforcement.GetByCountryIdAsync(country.Id))
        .Where(r => r.CharacterCountryId == country.Id && r.Status == ReinforcementStatus.Active);

      var townCount = await repo.Town.CountByCountryIdAsync(country.Id);
      var characterCount = await repo.Country.CountCharactersAsync(country.Id, true, 200) + reinforcements.Count();
      var townTypeCost = (countryOptional.HasData && countryOptional.Data.CapitalTownId == town.Id) ? 12000 :
        town.Type == TownType.Large ? 10000 : 8000;

      var cost = townTypeCost * (Math.Pow(1.07f, townCount) + Math.Pow(1.1f, characterCount)) + town.TakeoverDefensePoint * 1.5f;

      var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
      if (wars.Any(w => w.IsJoinAvailable(town.CountryId) && w.IsJoinAvailable(country.Id)))
      {
        if (wars.Any(w => w.IsJoinAvailable(town.CountryId) && w.IsJoinAvailable(country.Id) &&
          　　　　　　　　　　(w.Status == CountryWarStatus.Available || (w.Status == CountryWarStatus.StopRequesting && town.CountryId != w.RequestedStopCountryId) || (w.Status == CountryWarStatus.InReady && w.IntStartGameDate - 6 < system.IntGameDateTime)) &&
          　　　　　　　　　　w.Mode == CountryWarMode.Battle))
        {
          // 自分の国と戦争状態にある場合
          cost *= 2.4f;
        }
      }
      else if (wars.Any(w => w.IsJoin(town.CountryId) && (w.Status == CountryWarStatus.Available || (w.Status == CountryWarStatus.StopRequesting && w.RequestedStopCountryId != town.CountryId))))
      {
        // 他の国と戦争状態にある場合
        // 停戦協議中の場合はコストを増やさない（停戦協議の濫用が最近多いのであえて）
        if (countryOptional.HasData && countryOptional.Data.AiType == CountryAiType.Farmers)
        {
          if (countryOptional.Data.Religion == country.Religion)
          {
            cost *= 0.36f;
          }
          else
          {
            cost *= 1.2f;
          }
        }
        else
        {
          cost *= 2.1f;
        }
      }

      if (town.Religion != ReligionType.Any && town.Religion != ReligionType.None && country.Religion == town.Religion &&
        country.Religion != countryOptional.Data?.Religion)
      {
        cost *= 0.82f;
        if (system.RuleSet == GameRuleSet.Religion)
        {
          cost *= 0.9f;
        }
      }
      if (town.Religion == countryOptional.Data?.Religion)
      {
        cost *= 1.08f;
      }

      return (int)cost;
    }
  }
}
