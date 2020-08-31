﻿using System;
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
      var characterCount = await repo.Country.CountCharactersAsync(country.Id, true);
      var townTypeCost = (countryOptional.HasData && countryOptional.Data.CapitalTownId == town.Id) ? 36000 :
        town.Type == TownType.Large ? 14000 : 9000;

      var cost = townTypeCost * Math.Pow(1.06f, townCount) * Math.Pow(1.11f, characterCount) + town.TakeoverDefensePoint;

      var wars = await repo.CountryDiplomacies.GetAllWarsAsync();
      if (wars.Any(w => w.IsJoinAvailable(town.CountryId) && w.IsJoinAvailable(country.Id)))
      {
        // 自分の国と戦争状態にある場合
        cost *= 3;
      }
      else if (wars.Any(w => w.IsJoin(town.CountryId) && (w.Status == CountryWarStatus.InReady || w.Status == CountryWarStatus.Available)))
      {
        // 他の国と戦争状態にある場合
        // 停戦協議中の場合はコストを増やさない（停戦協議の濫用が最近多いのであえて）
        cost *= 1.6f;
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
