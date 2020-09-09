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
using SangokuKmy.Models.Services;
using SangokuKmy.Models.Common;

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
      Character self;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        var countryData = await repo.Country.GetAliveByIdAsync(chara.CountryId);
        var system = await repo.System.GetAsync();
        self = chara;

        if (system.IsBattleRoyaleMode && param.Status == ReinforcementStatus.Active)
        {
          // 全面戦争中は援軍にいけない
          ErrorCode.InvalidOperationError.Throw();
        }

        var oldTownId = chara.TownId;

        if (param.Status == ReinforcementStatus.Requesting || param.Status == ReinforcementStatus.RequestCanceled)
        {
          // chara: 要求するほう

          var country = countryData.GetOrError(ErrorCode.CountryNotFoundError);

          var posts = (await repo.Country.GetPostsAsync(country.Id)).Where(p => p.CharacterId == chara.Id);
          var hasPermission = posts.Any(p => p.Type.CanDiplomacy());

          if (!hasPermission)
          {
            ErrorCode.NotPermissionError.Throw();
          }

          var targetCharacter = await repo.Character.GetByIdAsync(param.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
          var targetCountry = await repo.Country.GetByIdAsync(targetCharacter.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
          var olds = await repo.Reinforcement.GetByCharacterIdAsync(targetCharacter.Id);

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
          var old = olds.FirstOrDefault(r => r.CharacterCountryId == chara.CountryId && r.RequestedCountryId == param.RequestedCountryId);

          if (old == null && param.Status != ReinforcementStatus.Returned && param.Status != ReinforcementStatus.Submited)
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
            var country = countryData.GetOrError(ErrorCode.CountryNotFoundError);
            var requestedCountry = await repo.Country.GetByIdAsync(param.RequestedCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);

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

            var post = (await repo.Country.GetPostsAsync(chara.CountryId)).Where(p => p.CharacterId == chara.Id);
            var isMonarch = post.Any(p => p.Type == CountryPostType.Monarch);

            if (isMonarch && !Config.Game.IsAllowMonarchReinforcement)
            {
              ErrorCode.NotPermissionError.Throw();
            }

            await CharacterService.ChangeTownAsync(repo, requestedCountry.CapitalTownId, chara);
            await CharacterService.ChangeCountryAsync(repo, requestedCountry.Id, new Character[] { chara, });

            // 君主が援軍に行く場合、君主データを残す
            if (isMonarch)
            {
              var monarch = new CountryPost
              {
                CharacterId = chara.Id,
                CountryId = old.CharacterCountryId,
                Type = CountryPostType.MonarchDisabled,
              };
              await repo.Country.SetPostAsync(monarch);
              await StatusStreaming.Default.SendCountryAsync(ApiData.From(monarch), old.CharacterCountryId);
            }

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
            old = olds.FirstOrDefault(o => o.Status == ReinforcementStatus.Active);

            if (old == null)
            {
              ErrorCode.InvalidOperationError.Throw();
            }

            var originalCountry = await repo.Country.GetByIdAsync(old.CharacterCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
            if (originalCountry.HasOverthrown)
            {
              ErrorCode.InvalidOperationError.Throw();
            }

            await CharacterService.ChangeTownAsync(repo, originalCountry.CapitalTownId, chara);
            await CharacterService.ChangeCountryAsync(repo, originalCountry.Id, new Character[] { chara, });

            // 援軍に行ったのが君主の場合、復活する
            var post = (await repo.Country.GetPostsAsync(chara.CountryId)).Where(p => p.CharacterId == chara.Id);
            if (post.Any(p => p.Type == CountryPostType.MonarchDisabled))
            {
              var monarch = new CountryPost
              {
                CharacterId = chara.Id,
                CountryId = old.CharacterCountryId,
                Type = CountryPostType.Monarch,
              };
              await repo.Country.SetPostAsync(monarch);
              await StatusStreaming.Default.SendCountryAsync(ApiData.From(monarch), old.CharacterCountryId);
            }

            var countryName = countryData.Data?.Name ?? "無所属";
            maplog = new MapLog
            {
              ApiGameDateTime = system.GameDateTime,
              Date = DateTime.Now,
              EventType = EventType.ReinforcementReturned,
              IsImportant = false,
              Message = $"<country>{originalCountry.Name}</country> の援軍 <character>{chara.Name}</character> は、 <country>{countryName}</country> から帰還しました",
            };
          }
          if (param.Status == ReinforcementStatus.Submited)
          {
            var country = countryData.GetOrError(ErrorCode.CountryNotFoundError);
            old = olds.FirstOrDefault(o => o.Status == ReinforcementStatus.Active);

            if (old == null)
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

        await repo.SaveChangesAsync();
      }

      // マップログ、援軍情報を通知
      await StatusStreaming.Default.SendCountryAsync(ApiData.From(reinforcement), new uint[] { reinforcement.CharacterCountryId, reinforcement.RequestedCountryId, });
      if (maplog != null)
      {
        await StatusStreaming.Default.SendAllAsync(ApiData.From(maplog));
        await AnonymousStreaming.Default.SendAllAsync(ApiData.From(maplog));
      }
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(self), self.Id);
    }
  }
}
