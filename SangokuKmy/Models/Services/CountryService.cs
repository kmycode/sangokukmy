using System;
using System.Threading.Tasks;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;
using System.Collections.Generic;
using System.Linq;
using SangokuKmy.Models.Common;

namespace SangokuKmy.Models.Services
{
  public static class CountryService
  {
    public static async Task<float> GetCountryBuildingSizeAsync(MainRepository repo, uint countryId, CountryBuilding type)
    {
      var towns = await repo.Town.GetByCountryIdAsync(countryId);
      return GetSize(towns.Where(t => t.CountryBuilding == type).Select(t => t.CountryBuildingValue));
    }

    public static async Task<float> GetCountryLaboratorySizeAsync(MainRepository repo, uint countryId, CountryLaboratory type)
    {
      var towns = await repo.Town.GetByCountryIdAsync(countryId);
      return GetSize(towns.Where(t => t.CountryLaboratory == type).Select(t => t.CountryLaboratoryValue));
    }

    public static async Task<int> GetSafeMaxAsync(MainRepository repo, uint countryId)
    {
      var size = await GetCountryBuildingSizeAsync(repo, countryId, CountryBuilding.CountrySafe);
      return (int)(size * Config.CountryBuildingMax * Config.SafePerEndurance);
    }

    private static float GetSize(IEnumerable<int> values)
    {
      var size = 0.0f;
      var add = 1.0f;
      foreach (var value in values.OrderByDescending(v => v))
      {
        size += add * ((float)value / Config.CountryBuildingMax);
        add *= 2.0f / 3;
      }
      return size;
    }
  }
}
