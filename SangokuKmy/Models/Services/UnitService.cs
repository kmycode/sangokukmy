using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Services
{
  public static class UnitService
  {
    public static async Task RemoveAsync(MainRepository repo, uint unitId)
    {
      var members = await repo.Unit.GetMembersAsync(unitId);
      repo.Unit.RemoveUnit(unitId);

      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(new ApiSignal
      {
        Type = SignalType.UnitRemoved,
      }), members.Select(um => um.CharacterId));
    }

    public static void Leave(MainRepository repo, uint characterId)
    {
      repo.Unit.RemoveMember(characterId);
    }
  }
}
