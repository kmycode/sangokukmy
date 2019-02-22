using Microsoft.AspNetCore.Mvc;
using SangokuKmy.Common;
using SangokuKmy.Filters;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [ServiceFilter(typeof(SangokuKmyErrorFilterAttribute))]
  [AuthenticationFilter]
  public class ReinforcementController : Controller, IAuthenticationDataReceiver
  {
    public AuthenticationData AuthData { private get; set; }

    [HttpPost("country/reinforcement")]
    public async Task SetAsync(
      [FromBody] Reinforcement param)
    {
      Reinforcement reinforcement;
      MapLog maplog = null;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        var country = await repo.Country.GetAliveByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var system = await repo.System.GetAsync();

        var oldTownId = chara.TownId;

        if (param.Status == ReinforcementStatus.Requesting || param.Status == ReinforcementStatus.RequestCanceled)
        {
          // chara: 要求するほう

          var posts = (await repo.Country.GetPostsAsync(country.Id)).Where(p => p.CharacterId == chara.Id);
          var hasPermission = posts.Any(p => p.Type == CountryPostType.Monarch || p.Type == CountryPostType.Warrior);

          if (!hasPermission)
          {
            ErrorCode.NotPermissionError.Throw();
          }

          var targetCharacter = await repo.Character.GetByIdAsync(param.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
          var targetCountry = await repo.Country.GetByIdAsync(targetCharacter.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
          var targetPosts = (await repo.Country.GetPostsAsync(targetCountry.Id)).Where(p => p.CharacterId == targetCharacter.Id);
          var olds = await repo.Reinforcement.GetByCharacterIdAsync(targetCharacter.Id);

          if (targetPosts.Any(p => p.Type == CountryPostType.Monarch))
          {
            ErrorCode.NotPermissionError.Throw();
          }
          if (country.Id == targetCountry.Id)
          {
            ErrorCode.MeaninglessOperationError.Throw();
          }
          var alliance = await repo.CountryDiplomacies.GetCountryAllianceAsync(country.Id, targetCountry.Id).GetOrErrorAsync(ErrorCode.NotPermissionError);
          if (alliance.Status != CountryAllianceStatus.Available && alliance.Status != CountryAllianceStatus.ChangeRequesting)
          {
            ErrorCode.NotPermissionError.Throw();
          }
          if (olds.Any(r => r.Status == ReinforcementStatus.Active))
          {
            ErrorCode.InvalidOperationError.Throw();
          }
          if (olds.Any(r => r.Status == ReinforcementStatus.Requesting && r.RequestedCountryId == chara.CountryId))
          {
            if (param.Status == ReinforcementStatus.Requesting)
            {
              ErrorCode.MeaninglessOperationError.Throw();
            }
          }
          else
          {
            if (param.Status == ReinforcementStatus.RequestCanceled)
            {
              ErrorCode.InvalidOperationError.Throw();
            }
          }

          if (param.Status == ReinforcementStatus.Requesting)
          {
            reinforcement = new Reinforcement
            {
              CharacterId = targetCharacter.Id,
              CharacterCountryId = targetCountry.Id,
              RequestedCountryId = country.Id,
              Status = ReinforcementStatus.Requesting,
            };
            await repo.Reinforcement.AddAsync(reinforcement);
          }
          else
          {
            reinforcement = olds.FirstOrDefault(r => r.RequestedCountryId == chara.CountryId);
            reinforcement.Status = ReinforcementStatus.RequestCanceled;
          }
        }
        else if (param.Status == ReinforcementStatus.RequestDismissed ||
                 param.Status == ReinforcementStatus.Active ||
                 param.Status == ReinforcementStatus.Returned ||
                 param.Status == ReinforcementStatus.Submited)
        {
          // chara: 要求されるほう

          var olds = await repo.Reinforcement.GetByCharacterIdAsync(chara.Id);
          var requestedCountry = await repo.Country.GetByIdAsync(param.RequestedCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
          var old = olds.FirstOrDefault(r => r.RequestedCountryId == requestedCountry.Id);

          if (old == null)
          {
            ErrorCode.InvalidOperationError.Throw();
          }

          if (param.Status == ReinforcementStatus.RequestDismissed)
          {
            if (old.Status != ReinforcementStatus.Requesting)
            {
              ErrorCode.InvalidOperationError.Throw();
            }
          }
          if (param.Status == ReinforcementStatus.Active)
          {
            if (old.Status != ReinforcementStatus.Requesting)
            {
              ErrorCode.InvalidOperationError.Throw();
            }
            if (requestedCountry.HasOverthrown)
            {
              ErrorCode.InvalidOperationError.Throw();
            }

            var alliance = await repo.CountryDiplomacies.GetCountryAllianceAsync(old.RequestedCountryId, old.CharacterCountryId).GetOrErrorAsync(ErrorCode.NotPermissionError);
            if (alliance.Status != CountryAllianceStatus.Available && alliance.Status != CountryAllianceStatus.ChangeRequesting)
            {
              ErrorCode.NotPermissionError.Throw();
            }

            chara.CountryId = requestedCountry.Id;
            chara.TownId = requestedCountry.CapitalTownId;
            maplog = new MapLog
            {
              ApiGameDateTime = system.GameDateTime,
              Date = DateTime.Now,
              EventType = EventType.ReinforcementActived,
              IsImportant = false,
              Message = $"<country>{country.Name}</country> の <character>{chara.Name}</character> は、 <country>{requestedCountry.Name}</country> へ援軍に行きました",
            };
          }
          if (param.Status == ReinforcementStatus.Returned)
          {
            if (old.Status != ReinforcementStatus.Active)
            {
              ErrorCode.InvalidOperationError.Throw();
            }

            var originalCountry = await repo.Country.GetByIdAsync(old.CharacterCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
            if (originalCountry.HasOverthrown)
            {
              ErrorCode.InvalidOperationError.Throw();
            }

            chara.CountryId = originalCountry.Id;
            chara.TownId = originalCountry.CapitalTownId;
            maplog = new MapLog
            {
              ApiGameDateTime = system.GameDateTime,
              Date = DateTime.Now,
              EventType = EventType.ReinforcementReturned,
              IsImportant = false,
              Message = $"<country>{originalCountry.Name}</country> の援軍 <character>{chara.Name}</character> は、 <country>{country.Name}</country> から帰還しました",
            };
          }
          if (param.Status == ReinforcementStatus.Submited)
          {
            if (old.Status != ReinforcementStatus.Active)
            {
              ErrorCode.InvalidOperationError.Throw();
            }

            var originalCountry = await repo.Country.GetByIdAsync(old.CharacterCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
            if (!originalCountry.HasOverthrown)
            {
              ErrorCode.InvalidOperationError.Throw();
            }

            maplog = new MapLog
            {
              ApiGameDateTime = system.GameDateTime,
              Date = DateTime.Now,
              EventType = EventType.ReinforcementSubmited,
              IsImportant = false,
              Message = $"<country>{originalCountry.Name}</country> の援軍 <character>{chara.Name}</character> は、 <country>{country.Name}</country> に帰順しました",
            };
          }

          reinforcement = old;
          reinforcement.Status = param.Status;
        }
        else
        {
          ErrorCode.InvalidParameterError.Throw();
          reinforcement = null;
        }

        if (maplog != null)
        {
          await repo.MapLog.AddAsync(maplog);
        }

        // 国変わった場合、役職を消す
        if (chara.TownId != oldTownId)
        {
          repo.Country.RemoveCharacterPosts(chara.Id);
        }

        await repo.SaveChangesAsync();

        // 援軍にいったことを通知
        if (chara.TownId != oldTownId)
        {
          await StatusStreaming.Default.SendCharacterAsync(ApiData.From(chara), chara.Id);
          var oldTown = await repo.Town.GetByIdAsync(oldTownId).GetOrErrorAsync(ErrorCode.TownNotFoundError);
          var town = await repo.Town.GetByIdAsync(chara.TownId).GetOrErrorAsync(ErrorCode.TownNotFoundError);
          await StatusStreaming.Default.SendCharacterAsync(ApiData.From(oldTown), (await repo.Town.GetCharactersAsync(oldTownId)).Select(c => c.Character.Id).Distinct());
          await StatusStreaming.Default.SendCharacterAsync(ApiData.From(town), (await repo.Town.GetCharactersAsync(chara.TownId)).Select(c => c.Character.Id).Distinct());
          await StatusStreaming.Default.SendCharacterAsync(ApiData.From(new TownForAnonymous(oldTown)), chara.Id);
        }
      }

      // マップログ、援軍情報を通知
      await StatusStreaming.Default.SendCountryAsync(ApiData.From(reinforcement), new uint[] { reinforcement.CharacterCountryId, reinforcement.RequestedCountryId, });
      if (maplog != null)
      {
        await StatusStreaming.Default.SendAllAsync(ApiData.From(maplog));
        await AnonymousStreaming.Default.SendAllAsync(ApiData.From(maplog));
      }
    }
  }
}
