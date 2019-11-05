using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SangokuKmy.Common;
using SangokuKmy.Filters;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Streamings;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [ServiceFilter(typeof(SangokuKmyErrorFilterAttribute))]
  public class DiplomacyController : Controller, IAuthenticationDataReceiver
  {
    private readonly ILogger _logger;
    public AuthenticationData AuthData { private get; set; }

    public DiplomacyController(ILogger<SangokuKmyController> logger)
    {
      this._logger = logger;
    }

    [AuthenticationFilter]
    [HttpPut("country/{targetId}/alliance")]
    public async Task SetCountryAllianceAsync(
      [FromRoute] uint targetId,
      [FromBody] CountryAlliance param)
    {
      CountryAlliance alliance;
      MapLog mapLog = null;

      if (param.Status != CountryAllianceStatus.Available &&
          param.Status != CountryAllianceStatus.ChangeRequesting &&
          param.Status != CountryAllianceStatus.Dismissed &&
          param.Status != CountryAllianceStatus.InBreaking &&
          param.Status != CountryAllianceStatus.Requesting &&
          param.Status != CountryAllianceStatus.None)
      {
        ErrorCode.InvalidParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var self = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var posts = await repo.Country.GetPostsAsync(self.CountryId);
        var myPost = posts.FirstOrDefault(p => p.CharacterId == self.Id);
        if (myPost == null || !myPost.Type.CanDiplomacy())
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var target = await repo.Country.GetAliveByIdAsync(targetId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);

        var old = await repo.CountryDiplomacies.GetCountryAllianceAsync(self.CountryId, targetId);
        var war = await repo.CountryDiplomacies.GetCountryWarAsync(self.CountryId, targetId);
        if (old.HasData &&
          old.Data.Status != CountryAllianceStatus.Broken &&
          old.Data.Status != CountryAllianceStatus.Dismissed &&
          old.Data.Status != CountryAllianceStatus.None)
        {
          var o = old.Data;

          if ((param.Status == CountryAllianceStatus.Available || param.Status == CountryAllianceStatus.ChangeRequesting) &&
                o.Status == CountryAllianceStatus.Requesting)
          {
            if (self.CountryId == o.RequestedCountryId)
            {
              // 自分で自分の要求を承認しようとした
              ErrorCode.NotPermissionError.Throw();
            }
          }
          else if (param.Status == CountryAllianceStatus.InBreaking &&
                    (o.Status != CountryAllianceStatus.Available && o.Status != CountryAllianceStatus.ChangeRequesting))
          {
            // 結んでいないのに破棄はエラー
            ErrorCode.NotPermissionError.Throw();
          }
          else if ((param.Status == CountryAllianceStatus.None || param.Status == CountryAllianceStatus.Requesting) &&
                    (o.Status == CountryAllianceStatus.Available || o.Status == CountryAllianceStatus.ChangeRequesting))
          {
            // 結んでいるものを一瞬でなかったことにするのはエラー
            ErrorCode.NotPermissionError.Throw();
          }
          else if (param.Status == o.Status && param.Status == CountryAllianceStatus.Available)
          {
            // 再承認はできない
            ErrorCode.MeaninglessOperationError.Throw();
          }

          if (param.Status == CountryAllianceStatus.Available)
          {
            param.BreakingDelay = o.BreakingDelay;
            param.IsPublic = o.IsPublic;
          }
        }
        else
        {
          if (param.Status != CountryAllianceStatus.Requesting)
          {
            // 同盟を結んでいない場合、同盟の要求以外にできることはない
            ErrorCode.NotPermissionError.Throw();
          }

          war.Some((w) =>
          {
            if (w.Status == CountryWarStatus.Available ||
                w.Status == CountryWarStatus.InReady ||
                w.Status == CountryWarStatus.StopRequesting)
            {
              // 戦争中
              ErrorCode.NotPermissionError.Throw();
            }
          });
        }

        alliance = new CountryAlliance
        {
          RequestedCountryId = self.CountryId,
          InsistedCountryId = targetId,
          BreakingDelay = param.BreakingDelay,
          IsPublic = param.IsPublic,
          Status = param.Status,
          NewBreakingDelay = param.NewBreakingDelay,
        };
        await repo.CountryDiplomacies.SetAllianceAsync(alliance);

        // 同盟関係を周りに通知
        if (alliance.IsPublic && old.HasData)
        {
          if (old.Data.Status == CountryAllianceStatus.Requesting &&
              alliance.Status == CountryAllianceStatus.Available)
          {
            var country1 = await repo.Country.GetByIdAsync(alliance.RequestedCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
            var country2 = await repo.Country.GetByIdAsync(alliance.InsistedCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
            mapLog = new MapLog
            {
              ApiGameDateTime = (await repo.System.GetAsync()).GameDateTime,
              Date = DateTime.Now,
              EventType = EventType.AllianceStart,
              IsImportant = true,
              Message = "<country>" + country1.Name + "</country> と <country>" + country2.Name + "</country> は、同盟を締結しました",
            };
            await repo.MapLog.AddAsync(mapLog);
          }
        }

        await repo.SaveChangesAsync();
      }

      // 同盟関係を周りに通知
      if (alliance.IsPublic)
      {
        await StatusStreaming.Default.SendAllAsync(ApiData.From(alliance));
        if (mapLog != null)
        {
          await StatusStreaming.Default.SendAllAsync(ApiData.From(mapLog));
          await AnonymousStreaming.Default.SendAllAsync(ApiData.From(mapLog));
        }
      }
      else
      {
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(alliance), alliance.RequestedCountryId);
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(alliance), alliance.InsistedCountryId);
      }
    }

    [AuthenticationFilter]
    [HttpPut("country/{targetId}/war")]
    public async Task SetCountryWarAsync(
      [FromRoute] uint targetId,
      [FromBody] CountryWar param)
    {
      CountryWar war;
      Optional<CountryAlliance> alliance;
      MapLog mapLog = null;

      if (param.Status != CountryWarStatus.InReady /* &&
          param.Status != CountryWarStatus.StopRequesting &&
          param.Status != CountryWarStatus.Stoped */ )
      {
        ErrorCode.InvalidParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var system = await repo.System.GetAsync();
        var self = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var posts = await repo.Country.GetPostsAsync(self.CountryId);
        var myPost = posts.FirstOrDefault(p => p.CharacterId == self.Id);
        if (myPost == null || !myPost.Type.CanDiplomacy())
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var towns = await repo.Town.GetAllAsync();
        var target = await repo.Country.GetAliveByIdAsync(targetId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);

        var old = await repo.CountryDiplomacies.GetCountryWarAsync(self.CountryId, targetId);
        alliance = await repo.CountryDiplomacies.GetCountryAllianceAsync(self.CountryId, targetId);
        old.Some((o) =>
        {
          if ((o.Status == CountryWarStatus.InReady || o.Status == CountryWarStatus.Available) &&
              param.Status == CountryWarStatus.InReady)
          {
            // 重複して宣戦布告はできない
            ErrorCode.MeaninglessOperationError.Throw();
          }
          else if (o.Status == CountryWarStatus.StopRequesting && param.Status == CountryWarStatus.Stoped &&
                   o.RequestedCountryId == self.CountryId)
          {
            // 自分の停戦要求を自分で承認できない
            ErrorCode.NotPermissionError.Throw();
          }
          else if (o.Status == CountryWarStatus.Stoped && param.Status == CountryWarStatus.StopRequesting)
          {
            // 一度決まった停戦を撤回できない
            ErrorCode.NotPermissionError.Throw();
          }

          if (o.Status == CountryWarStatus.Stoped && param.Status == CountryWarStatus.InReady)
          {
            if (param.StartGameDate.ToInt() < system.IntGameDateTime + 12 * 12 + 1 ||
              param.StartGameDate.Year < Config.StartYear + Config.UpdateStartYear + Config.CountryBattleStopDuring / 12)
            {
              // 開戦が早すぎる
              ErrorCode.InvalidParameterError.Throw();
            }
            else if (param.StartGameDate.ToInt() > system.IntGameDateTime + 12 * 24)
            {
              // 開戦が遅すぎる
              ErrorCode.InvalidParameterError.Throw();
            }
          }
          else
          {
            param.RequestedCountryId = o.RequestedCountryId;
            param.InsistedCountryId = o.InsistedCountryId;
            param.StartGameDate = o.StartGameDate;
          }
        });
        old.None(() =>
        {
          if (!towns.GetAroundCountries(towns.Where(t => t.CountryId == param.InsistedCountryId)).Contains(param.RequestedCountryId))
          {
            // 飛び地布告
            ErrorCode.InvalidOperationError.Throw();
          }

          if (param.Status == CountryWarStatus.StopRequesting || param.Status == CountryWarStatus.Stoped)
          {
            // 存在しない戦争を停戦にはできない
            ErrorCode.NotPermissionError.Throw();
          }
          else if (param.StartGameDate.ToInt() < system.IntGameDateTime + 12 * 12 + 1)
          {
            // 開戦が早すぎる
            ErrorCode.InvalidParameterError.Throw();
          }
          else if (param.StartGameDate.ToInt() > system.IntGameDateTime + 12 * 24)
          {
            // 開戦が遅すぎる
            ErrorCode.InvalidParameterError.Throw();
          }

          alliance.Some((a) =>
          {
            if (a.Status == CountryAllianceStatus.Available ||
                a.Status == CountryAllianceStatus.ChangeRequesting ||
                a.Status == CountryAllianceStatus.InBreaking)
            {
              // 同盟が有効中
              ErrorCode.NotPermissionError.Throw();
            }
            if (a.Status == CountryAllianceStatus.Requesting)
            {
              // 自動で同盟申請を却下する
              a.Status = CountryAllianceStatus.Broken;
            }
          });
        });

        war = new CountryWar
        {
          RequestedCountryId = param.RequestedCountryId,
          InsistedCountryId = param.InsistedCountryId,
          StartGameDate = param.StartGameDate,
          Status = param.Status,
          RequestedStopCountryId = param.RequestedStopCountryId,
        };

        await CountryService.SendWarAndSaveAsync(repo, war);
      }

      if (alliance.HasData)
      {
        var a = alliance.Data;
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(a), a.RequestedCountryId);
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(a), a.InsistedCountryId);
      }
    }

    [AuthenticationFilter]
    [HttpPut("town/{townId}/war")]
    public async Task SetTownWarAsync(
      [FromRoute] uint townId)
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var system = await repo.System.GetAsync();
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var posts = await repo.Country.GetPostsAsync(chara.CountryId);
        var myPost = posts.FirstOrDefault(p => p.CharacterId == chara.Id);
        if (myPost == null || !myPost.Type.CanDiplomacy())
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var country = await repo.Country.GetAliveByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var targetTown = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.TownNotFoundError);
        var targetCountry = await repo.Country.GetAliveByIdAsync(targetTown.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);

        if (targetTown.Id == targetCountry.CapitalTownId)
        {
          // 首都への攻略
          ErrorCode.InvalidOperationError.Throw();
        }
        if (await repo.Town.CountByCountryIdAsync(targetCountry.Id) <= 1)
        {
          // 残り１都市の国への攻略
          ErrorCode.NotPermissionError.Throw();
        }

        var alliance = await repo.CountryDiplomacies.GetCountryAllianceAsync(country.Id, targetCountry.Id);
        var countryWar = await repo.CountryDiplomacies.GetCountryWarAsync(country.Id, targetCountry.Id);
        if (alliance.HasData)
        {
          if (alliance.Data.Status == CountryAllianceStatus.Available ||
              alliance.Data.Status == CountryAllianceStatus.InBreaking ||
              alliance.Data.Status == CountryAllianceStatus.ChangeRequesting ||
              alliance.Data.Status == CountryAllianceStatus.Requesting)
          {
            ErrorCode.NotPermissionError.Throw();
          }
        }
        if (countryWar.HasData)
        {
          if (countryWar.Data.Status == CountryWarStatus.Available ||
              countryWar.Data.Status == CountryWarStatus.StopRequesting)
          {
            ErrorCode.MeaninglessOperationError.Throw();
          }
          if (countryWar.Data.Status == CountryWarStatus.InReady)
          {
            if (system.IntGameDateTime > countryWar.Data.IntStartGameDate - 6)
            {
              ErrorCode.NotPermissionError.Throw();
            }
          }
        }
        else
        {
          // 他国同士の戦争には介入できない
          var wars = (await repo.CountryDiplomacies.GetAllWarsAsync())
            .Where(w => w.RequestedCountryId == targetCountry.Id || w.InsistedCountryId == targetCountry.Id);
          if (wars.Any(w => w.Status == CountryWarStatus.InReady || w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.StopRequesting))
          {
            ErrorCode.NotPermissionError.Throw();
          }
        }

        var olds = (await repo.CountryDiplomacies.GetAllTownWarsAsync())
          .Where(o => o.RequestedCountryId == country.Id);
        if (olds.Any(o => o.Status == TownWarStatus.Available || o.Status == TownWarStatus.InReady))
        {
          // 重複して宣戦布告はできない
          ErrorCode.MeaninglessOperationError.Throw();
        }
        else if (olds.Any(o => o.Status == TownWarStatus.Terminated &&
                               system.IntGameDateTime - o.IntGameDate < 12 * 10))
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var war = new TownWar
        {
          RequestedCountryId = country.Id,
          InsistedCountryId = targetCountry.Id,
          IntGameDate = system.IntGameDateTime + 1,
          TownId = townId,
          Status = TownWarStatus.InReady,
        };
        await CountryService.SendTownWarAndSaveAsync(repo, war);
      }
    }
  }
}
