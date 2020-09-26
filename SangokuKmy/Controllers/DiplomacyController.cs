using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
      Optional<CountryAlliance> old = default;

      if (param.Status != CountryAllianceStatus.Available &&
          param.Status != CountryAllianceStatus.ChangeRequesting &&
          param.Status != CountryAllianceStatus.Dismissed &&
          param.Status != CountryAllianceStatus.InBreaking &&
          param.Status != CountryAllianceStatus.Requesting &&
          param.Status != CountryAllianceStatus.Changed &&
          param.Status != CountryAllianceStatus.None)
      {
        ErrorCode.InvalidParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var system = await repo.System.GetAsync();
        var self = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var myPosts = await repo.Country.GetCharacterPostsAsync(self.Id);
        if (!myPosts.Any(p => p.Type.CanDiplomacy()))
        {
          ErrorCode.NotPermissionError.Throw();
        }

        if (system.IsBattleRoyaleMode)
        {
          // 全国戦争中に同盟操作はできない
          ErrorCode.InvalidOperationError.Throw();
        }

        var target = await repo.Country.GetAliveByIdAsync(targetId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        if (target.AiType != CountryAiType.Human)
        {
          // 人間の国以外に同盟を申請することはできない
          ErrorCode.InvalidOperationError.Throw();
        }

        old = await repo.CountryDiplomacies.GetCountryAllianceAsync(self.CountryId, targetId);
        var changeTo = await repo.CountryDiplomacies.GetCountryAllianceChangingValueAsync(self.CountryId, targetId);
        var war = await repo.CountryDiplomacies.GetCountryWarAsync(self.CountryId, targetId);
        if (old.HasData &&
          old.Data.Status != CountryAllianceStatus.Broken &&
          old.Data.Status != CountryAllianceStatus.Dismissed &&
          old.Data.Status != CountryAllianceStatus.None)
        {
          var o = old.Data;

          if ((param.Status == CountryAllianceStatus.Available || param.Status == CountryAllianceStatus.ChangeRequesting || param.Status == CountryAllianceStatus.Changed) &&
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
          else if (param.Status == CountryAllianceStatus.Changed)
          {
            if (!changeTo.HasData || o.Status != CountryAllianceStatus.ChangeRequesting)
            {
              // 変更提案していないのに承諾はできない
              ErrorCode.InvalidOperationError.Throw();
            }
            else if (changeTo.Data.RequestedCountryId == self.CountryId)
            {
              // 自分の変更要求を自分で承認できない
              ErrorCode.InvalidOperationError.Throw();
            }
          }

          if (param.Status == CountryAllianceStatus.Available || param.Status == CountryAllianceStatus.InBreaking)
          {
            param.BreakingDelay = o.BreakingDelay;
            param.IsPublic = o.IsPublic;
            param.CanMissionary = o.CanMissionary;
            param.Memo = o.Memo;
          }
          else if (param.Status == CountryAllianceStatus.Changed)
          {
            param.BreakingDelay = changeTo.Data.BreakingDelay;
            param.IsPublic = changeTo.Data.IsPublic;
            param.CanMissionary = changeTo.Data.CanMissionary;
            param.Memo = changeTo.Data.Memo;
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
          CanMissionary = param.CanMissionary,
          Memo = param.Memo,
          Status = param.Status == CountryAllianceStatus.Changed ? CountryAllianceStatus.Available : param.Status,
        };
        if (param.Status != CountryAllianceStatus.ChangeRequesting)
        {
          await repo.CountryDiplomacies.SetAllianceAsync(alliance);
        }
        else
        {
          alliance.ChangeTargetId = old.Data.Id;
          alliance.Status = CountryAllianceStatus.ChangeRequestingValue;
          old.Data.Status = CountryAllianceStatus.ChangeRequesting;
          await repo.CountryDiplomacies.SetAllianceChangingValueAsync(alliance);
        }

        // 同盟関係を周りに通知
        if (alliance.IsPublic && old.HasData && alliance.Status != CountryAllianceStatus.ChangeRequestingValue)
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
      if (alliance.IsPublic && alliance.Status != CountryAllianceStatus.ChangeRequestingValue)
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

      if (alliance.Status == CountryAllianceStatus.ChangeRequestingValue && old.HasData)
      {
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(old.Data), alliance.RequestedCountryId);
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(old.Data), alliance.InsistedCountryId);
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

      if (param.Status != CountryWarStatus.InReady &&
          param.Status != CountryWarStatus.StopRequesting &&
          param.Status != CountryWarStatus.Stoped)
      {
        ErrorCode.InvalidParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var system = await repo.System.GetAsync();
        var self = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var myPosts = await repo.Country.GetCharacterPostsAsync(self.Id);
        if (!myPosts.Any(p => p.Type.CanDiplomacy()))
        {
          ErrorCode.NotPermissionError.Throw();
        }

        if (system.IsBattleRoyaleMode)
        {
          // 全国戦争中に戦争操作はできない
          ErrorCode.InvalidOperationError.Throw();
        }

        if (system.RuleSet != GameRuleSet.Religion && param.Mode == CountryWarMode.Religion)
        {
          // 宗教戦争は宗教ルールでないと布告できない
          ErrorCode.RuleSetError.Throw();
        }

        var towns = await repo.Town.GetAllAsync();
        var myCountry = await repo.Country.GetAliveByIdAsync(self.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var target = await repo.Country.GetAliveByIdAsync(targetId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var wars = await repo.CountryDiplomacies.GetAllWarsAsync();

        var old = await repo.CountryDiplomacies.GetCountryWarAsync(self.CountryId, targetId);
        alliance = await repo.CountryDiplomacies.GetCountryAllianceAsync(self.CountryId, targetId);
        old.Some((o) =>
        {
          if (o.Status == param.Status)
          {
            ErrorCode.MeaninglessOperationError.Throw();
          }

          if ((o.Status == CountryWarStatus.Available || o.Status == CountryWarStatus.InReady) &&
              param.Status == CountryWarStatus.InReady)
          {
            // 重複して宣戦布告はできない
            ErrorCode.MeaninglessOperationError.Throw();
          }
          else if (o.Status == CountryWarStatus.StopRequesting && param.Status == CountryWarStatus.Stoped &&
                   o.RequestedStopCountryId == self.CountryId)
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
            // 停戦後の再布告
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
          else if (o.Status == CountryWarStatus.StopRequesting && param.Status == CountryWarStatus.InReady)
          {
            // 停戦撤回または拒否
            if (o.StartGameDate.ToInt() <= system.GameDateTime.ToInt())
            {
              // 開戦後の場合は開戦扱い
              param.Status = CountryWarStatus.Available;
            }
          }

          param.RequestedStopCountryId = o.RequestedStopCountryId;
          if (param.Status == CountryWarStatus.StopRequesting)
          {
            param.RequestedStopCountryId = self.CountryId;
          }

          param.RequestedCountryId = o.RequestedCountryId;
          param.InsistedCountryId = o.InsistedCountryId;
          param.StartGameDate = o.StartGameDate;
          param.Mode = o.Mode;
        });
        old.None(() =>
        {
          if (param.Mode == CountryWarMode.Religion)
          {
            if (myCountry.Religion == ReligionType.Any || myCountry.Religion == ReligionType.None)
            {
              // 国教を持たない国は宗教戦争を布告できない
              ErrorCode.InvalidOperationError.Throw();
            }

            if (target.Religion == myCountry.Religion)
            {
              // 同じ宗教の国には宗教戦争を布告できない
              ErrorCode.ReligionError.Throw();
            }
          }

          if (wars.Where(w => w.Status == CountryWarStatus.Available || w.Status == CountryWarStatus.InReady || w.Status == CountryWarStatus.StopRequesting)
                  .Where(w => w.IsJoin(myCountry.Id) || w.IsJoin(target.Id))
                  .Any(w => w.Mode != param.Mode))
          {
            // 就業戦争と通常戦争を両方することはできない
            ErrorCode.InvalidWarModeError.Throw();
          }

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
          else if (param.StartGameDate.ToInt() < system.IntGameDateTime + 12 * 12 + 1 ||
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
        });

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

        war = new CountryWar
        {
          RequestedCountryId = param.RequestedCountryId,
          InsistedCountryId = param.InsistedCountryId,
          StartGameDate = param.StartGameDate,
          Status = param.Status,
          RequestedStopCountryId = param.RequestedStopCountryId,
          Mode = param.Mode,
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
        if (system.RuleSet == GameRuleSet.SimpleBattle)
        {
          ErrorCode.RuleSetError.Throw();
        }

        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var myPosts = await repo.Country.GetCharacterPostsAsync(chara.Id);
        if (!myPosts.Any(p => p.Type.CanDiplomacy()))
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

    [AuthenticationFilter]
    [HttpPost("country/penalties")]
    public async Task<IEnumerable<uint>> GetPenaltyCountries(
      [FromBody] CountryWar assumptionWar = null)
    {
      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var countries = await CountryService.GetPenaltyCountriesAsync(repo, assumptionWar);
        return countries;
      }
    }

    [AuthenticationFilter]
    [HttpPut("country/{countryId}/give/{townId}")]
    public async Task GiveTownAsync(
      [FromRoute] uint townId,
      [FromRoute] uint countryId)
    {
      ErrorCode.NotSupportedError.Throw();

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var system = await repo.System.GetAsync();
        if (system.RuleSet == GameRuleSet.SimpleBattle)
        {
          ErrorCode.RuleSetError.Throw();
        }

        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var myPosts = await repo.Country.GetCharacterPostsAsync(chara.Id);
        if (!myPosts.Any(p => p.Type.CanDiplomacy()))
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var country = await repo.Country.GetAliveByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var targetTown = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.TownNotFoundError);
        var targetCountry = await repo.Country.GetAliveByIdAsync(countryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);

        if (targetTown.CountryId != chara.CountryId)
        {
          // 自分以外の国の都市を割譲できない
          ErrorCode.InvalidOperationError.Throw();
        }

        if (targetCountry.AiType != CountryAiType.Human)
        {
          // AI国家に割譲できない
          ErrorCode.InvalidOperationError.Throw();
        }

        var allTowns = await repo.Town.GetAllAsync();
        if (!allTowns.GetAroundCountries(targetTown).Contains(countryId))
        {
          // 割譲で飛び地は作れない
          ErrorCode.InvalidOperationError.Throw();
        }

        var isSurrender = allTowns.Count(t => t.CountryId == chara.CountryId) == 1;

        // 割譲
        var defenders = (await repo.Town.GetAllDefendersAsync()).Where(d => d.TownId == townId);
        foreach (var d in defenders)
        {
          repo.Town.RemoveDefender(d.CharacterId);
          await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(d), repo, targetTown);
        }
        targetTown.CountryId = targetCountry.Id;
        var townCharacters = await repo.Town.GetCharactersAsync(townId);
        foreach (var character in townCharacters)
        {
          await CharacterService.StreamCharacterAsync(repo, character);
        }
        await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(targetTown), repo);
        await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(targetTown)));
        await LogService.AddMapLogAsync(repo, true, EventType.Concession, $"<country>{country.Name}</country> は、<country>{targetCountry.Name}</country> に <town>{targetTown.Name}</town> を割譲しました");

        if (isSurrender)
        {
          // 降伏
          await CountryService.OverThrowAsync(repo, country, targetCountry);
          await LogService.AddMapLogAsync(repo, true, EventType.Surrender, $"<country>{country.Name}</country> は、<country>{targetCountry.Name}</country> に降伏しました");
        }
      }
    }

    [AuthenticationFilter]
    [HttpPost("buy/towndef/{townId}")]
    public async Task<int> AddBuyTownCostAsync(
      [FromRoute] uint townId)
    {
      int cost;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var system = await repo.System.GetAsync();
        if (system.RuleSet == GameRuleSet.SimpleBattle)
        {
          ErrorCode.RuleSetError.Throw();
        }

        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var town = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.TownNotFoundError);
        var country = await repo.Country.GetAliveByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var myPosts = await repo.Country.GetCharacterPostsAsync(chara.Id);
        if (!myPosts.Any(p => p.Type.CanPolicy()))
        {
          ErrorCode.NotPermissionError.Throw();
        }

        if (country.PolicyPoint < 1000)
        {
          ErrorCode.InvalidOperationError.Throw();
        }

        if (chara.CountryId != town.CountryId)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var alliance = await repo.CountryDiplomacies.GetCountryAllianceAsync(chara.CountryId, town.CountryId);
        if (alliance.HasData)
        {
          ErrorCode.InvalidOperationError.Throw();
        }

        country.PolicyPoint -= 1000;
        town.TakeoverDefensePoint += 1000;

        await StatusStreaming.Default.SendCountryAsync(ApiData.From(country), country.Id);
        await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(town), repo);

        await repo.SaveChangesAsync();

        cost = await TownService.GetTownBuyCostAsync(repo, town, country);
      }

      return cost;
    }

    [AuthenticationFilter]
    [HttpGet("town/buycost/{townId}")]
    public async Task<IEnumerable<GetBuyTownCostResponse>> GetBuyTownCostAsync(
      [FromRoute] uint townId)
    {
      var result = new List<GetBuyTownCostResponse>();

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var countries = await repo.Country.GetAllAsync();
        var town = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.TownNotFoundError);
        foreach (var c in countries.Where(c => !c.HasOverthrown))
        {
          var r = new GetBuyTownCostResponse
          {
            Country = new CountryForAnonymous(c),
            Cost = await TownService.GetTownBuyCostAsync(repo, town, c),
          };
          result.Add(r);
        }
      }

      return result;
    }

    public struct GetBuyTownCostResponse
    {
      [JsonProperty("country")]
      public CountryForAnonymous Country { get; set; }
      [JsonProperty("cost")]
      public int Cost { get; set; }
    }

    [AuthenticationFilter]
    [HttpPost("buy/town/{townId}")]
    public async Task BuyTownAsync(
      [FromRoute] uint townId)
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var system = await repo.System.GetAsync();
        if (system.RuleSet == GameRuleSet.SimpleBattle)
        {
          ErrorCode.RuleSetError.Throw();
        }

        if (system.GameDateTime.Year < Config.BuyTownStartYear)
        {
          ErrorCode.TooEarlyError.Throw();
        }

        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var myPosts = await repo.Country.GetCharacterPostsAsync(chara.Id);
        if (!myPosts.Any(p => p.Type.CanPolicy()))
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var country = await repo.Country.GetAliveByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var targetTown = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.TownNotFoundError);
        var targetCountry = await repo.Country.GetByIdAsync(targetTown.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var cost = await TownService.GetTownBuyCostAsync(repo, targetTown, country);

        if (targetTown.CountryId == chara.CountryId)
        {
          // 自分の国の都市を購入できない
          ErrorCode.InvalidOperationError.Throw();
        }

        if (country.PolicyPoint < cost)
        {
          // 政策ポイントが足りない
          ErrorCode.InvalidOperationError.Throw();
        }

        var allTowns = await repo.Town.GetAllAsync();
        if (!allTowns.GetAroundCountries(targetTown).Contains(chara.CountryId))
        {
          // 購入で飛び地は作れない
          ErrorCode.InvalidOperationError.Throw();
        }

        var isSurrender = allTowns.Count(t => t.CountryId == chara.CountryId) == 1;

        // 購入
        country.PolicyPoint -= cost;
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(country), country.Id);

        var defenders = (await repo.Town.GetAllDefendersAsync()).Where(d => d.TownId == townId);
        foreach (var d in defenders)
        {
          repo.Town.RemoveDefender(d.CharacterId);
          await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(d), repo, targetTown);
        }
        targetTown.CountryId = chara.CountryId;
        targetTown.Agriculture = (int)(targetTown.Agriculture * 0.9f);
        targetTown.Commercial = (int)(targetTown.Commercial * 0.9f);
        targetTown.Technology = (int)(targetTown.Technology * 0.8f);
        targetTown.TownBuildingValue = (int)(targetTown.TownBuildingValue * 0.8f);
        targetTown.People = (int)(targetTown.People * 0.8f);
        targetTown.Security = (short)(targetTown.Security * 0.8f);
        var townCharacters = await repo.Town.GetCharactersAsync(townId);
        foreach (var character in townCharacters)
        {
          await CharacterService.StreamCharacterAsync(repo, character);
        }
        await StatusStreaming.Default.SendTownToAllAsync(ApiData.From(targetTown), repo);
        await AnonymousStreaming.Default.SendAllAsync(ApiData.From(new TownForAnonymous(targetTown)));
        await LogService.AddMapLogAsync(repo, true, EventType.BuyTown, $"<country>{country.Name}</country> は、<country>{targetCountry.Name}</country> の <town>{targetTown.Name}</town> を購入しました");

        if (isSurrender)
        {
          // 滅亡
          await CountryService.OverThrowAsync(repo, targetCountry, country);
          await LogService.AddMapLogAsync(repo, true, EventType.Surrender, $"<country>{targetCountry.Name}</country> は、<country>{country.Name}</country> に降伏しました");
        }

        await repo.SaveChangesAsync();
      }
    }
  }
}
