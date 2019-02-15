using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Services;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Filters;
using Newtonsoft.Json;
using System.Collections;
using SangokuKmy.Models.Commands;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Common;
using SangokuKmy.Models.Common;
using SangokuKmy.Streamings;
using System.Diagnostics;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [SangokuKmyErrorFilter]
  public class SangokuKmyController : Controller, IAuthenticationDataReceiver
  {
    public AuthenticationData AuthData { private get; set; }

    [HttpPost("authenticate")]
    public async Task<ApiData<AuthenticationData>> AuthenticateAsync
      ([FromBody] AuthenticateParameter param)
    {
      AuthenticationData authData;
      using (var repo = MainRepository.WithReadAndWrite())
      {
        authData = await AuthenticationService.WithIdAndPasswordAsync(repo, param.Id, param.Password);
      }
      return ApiData.From(authData);
    }

    public struct AuthenticateParameter
    {
      [JsonProperty("id")]
      public string Id { get; set; }
      [JsonProperty("password")]
      public string Password { get; set; }
    }

    [AuthenticationFilter]
    [HttpGet("commands")]
    public async Task<GetCharacterAllCommandsResponse> GetCharacterAllCommandsAsync()
    {
      IEnumerable<CharacterCommand> commands;
      int secondsNextCommand;
      using (var repo = MainRepository.WithRead())
      {
        var system = await repo.System.GetAsync();
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var isCurrentMonthExecuted = chara.LastUpdated >= system.CurrentMonthStartDateTime;
        var firstMonth = isCurrentMonthExecuted ? system.GameDateTime.NextMonth() : system.GameDateTime;
        commands = await repo.CharacterCommand.GetAllAsync(this.AuthData.CharacterId, firstMonth);

        // 次のコマンドが実行されるまでの秒数
        secondsNextCommand = (int)((isCurrentMonthExecuted ? chara.LastUpdated.AddSeconds(Config.UpdateTime) : chara.LastUpdated) - DateTime.Now).TotalSeconds;
      }
      return new GetCharacterAllCommandsResponse
      {
        Commands = commands,
        SecondsNextCommand = secondsNextCommand,
      };
    }

    public struct GetCharacterAllCommandsResponse
    {
      [JsonProperty("commands")]
      public IEnumerable<CharacterCommand> Commands { get; set; }
      [JsonProperty("secondsNextCommand")]
      public int SecondsNextCommand { get; set; }
    }

    [AuthenticationFilter]
    [HttpPut("commands")]
    public async Task SetCharacterCommandsAsync(
      [FromBody] IReadOnlyList<CharacterCommand> commands)
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        foreach (var commandGroup in commands.GroupBy(c => c.Type))
        {
          var cmd = Commands.Get(commandGroup.Key).GetOrError(ErrorCode.CommandTypeNotFoundError);
          var sameCommandParamsGroups = commandGroup.GroupBy(g => g.Parameters.Any() ? g.Parameters.Select(p => p.GetHashCode()).Aggregate((p1, p2) => p1 ^ p2) : 0, c => c);
          foreach (var sameCommandParamsGroup in sameCommandParamsGroups)
          {
            await cmd.InputAsync(repo, this.AuthData.CharacterId, sameCommandParamsGroup.Select(c => c.GameDateTime), sameCommandParamsGroup.First().Parameters.ToArray());
          }
        }
        await repo.SaveChangesAsync();
      }
    }

    [AuthenticationFilter]
    [HttpGet("icons")]
    public async Task<ApiArrayData<CharacterIcon>> GetCharacterAllIconsAsync()
    {
      IEnumerable<CharacterIcon> icons;
      using (var repo = MainRepository.WithRead())
      {
        icons = await repo.Character.GetCharacterAllIconsAsync(this.AuthData.CharacterId);
      }
      return ApiData.From(icons);
    }

    [HttpGet("characters")]
    public async Task<ApiArrayData<CharacterForAnonymous>> GetAllCharactersAsync()
    {
      IEnumerable<CharacterForAnonymous> charas;
      using (var repo = MainRepository.WithRead())
      {
        charas = (await repo.Character.GetAllAliveWithIconAsync())
          .Select(c => new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.AllCharacterList));
      }
      return ApiData.From(charas);
    }

    [HttpGet("countries")]
    public async Task<ApiArrayData<CountryForAnonymous>> GetAllCountriesAsync()
    {
      IEnumerable<CountryForAnonymous> countries;
      using (var repo = MainRepository.WithRead())
      {
        countries = (await repo.Country.GetAllForAnonymousAsync());
      }
      return ApiData.From(countries);
    }

    [AuthenticationFilter]
    [HttpGet("town/{townId}/characters")]
    public async Task<ApiArrayData<CharacterForAnonymous>> GetTownCharactersAsync(
      [FromRoute] uint townId)
    {
      IEnumerable<CharacterForAnonymous> charas;
      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var town = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.TownNotFoundError);

        if (town.CountryId != chara.CountryId && town.Id != chara.TownId)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        charas = (await repo.Town.GetCharactersAsync(townId))
          .Select(c => new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.SameTown));
      }
      return ApiData.From(charas);
    }

    [AuthenticationFilter]
    [HttpGet("town/{townId}/defenders")]
    public async Task<ApiArrayData<CharacterForAnonymous>> GetTownDefendersAsync(
      [FromRoute] uint townId)
    {
      IEnumerable<CharacterForAnonymous> charas;
      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var town = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.TownNotFoundError);

        if (town.CountryId != chara.CountryId && town.Id != chara.TownId)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        charas = (await repo.Town.GetDefendersAsync(townId))
          .Select(c => new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.SameTown));
      }
      return ApiData.From(charas);
    }

    [AuthenticationFilter]
    [HttpPost("town/scout")]
    public async Task ScoutTownAsync()
    {
      ScoutedTown scoutedTown, savedScoutedTown;
      Character chara;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var town = await repo.Town.GetByIdAsync(chara.TownId).GetOrErrorAsync(ErrorCode.TownNotFoundError);
        var country = await repo.Country.GetByIdAsync(chara.CountryId).GetOrErrorAsync(ErrorCode.InternalDataNotFoundError, new { type = "country", value = chara.CountryId, });

        if (town.CountryId == chara.CountryId)
        {
          ErrorCode.MeaninglessOperationError.Throw();
        }
        if (country.HasOverthrown)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var system = await repo.System.GetAsync();
        scoutedTown = ScoutedTown.From(town);
        scoutedTown.ScoutedDateTime = system.GameDateTime;
        scoutedTown.ScoutedCharacterId = chara.Id;
        scoutedTown.ScoutedCountryId = chara.CountryId;
        scoutedTown.ScoutMethod = ScoutMethod.Manual;

        await repo.ScoutedTown.AddScoutAsync(scoutedTown);
        await repo.SaveChangesAsync();
        
        savedScoutedTown = (await repo.ScoutedTown.GetByTownIdAsync(town.Id, chara.CountryId))
          .GetOrError(ErrorCode.InternalError, new { type = "already-scouted-town", value = scoutedTown.Id, });
      }

      await StatusStreaming.Default.SendCountryAsync(ApiData.From(savedScoutedTown), chara.CountryId);
    }

    [AuthenticationFilter]
    [HttpGet("country/{countryId}/characters")]
    public async Task<ApiArrayData<CharacterForAnonymous>> GetCountryCharactersAsync(
      [FromRoute] uint countryId)
    {
      IEnumerable<CharacterForAnonymous> charas;
      using (var repo = MainRepository.WithRead())
      {
        charas = (await repo.Country.GetCharactersAsync(countryId))
          .Select(c => new CharacterForAnonymous(c.Character, c.Icon, CharacterShareLevel.Anonymous));
      }
      return ApiData.From(charas);
    }

    [AuthenticationFilter]
    [HttpPut("country/posts")]
    public async Task SetCountryPostAsync(
      [FromBody] CountryPost param)
    {
      CountryPost post;
      using (var repo = MainRepository.WithReadAndWrite())
      {
        if (this.AuthData.CharacterId == param.CharacterId || param.Type == CountryPostType.Monarch)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var self = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var posts = await repo.Country.GetPostsAsync(self.CountryId);
        var myPost = posts.FirstOrDefault(p => p.CharacterId == self.Id);
        var targetPost = posts.FirstOrDefault(p => p.CharacterId == param.CharacterId);
        if (myPost == null || !myPost.Type.CanAppoint() || targetPost?.Type == CountryPostType.Monarch)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        var target = await repo.Character.GetByIdAsync(param.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        if (target.CountryId != self.CountryId)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        post = new CountryPost
        {
          CountryId = self.CountryId,
          CharacterId = target.Id,
          Type = param.Type,
          Character = new CharacterForAnonymous(target, null, CharacterShareLevel.Anonymous),
        };
        await repo.Country.SetPostAsync(post);

        await repo.SaveChangesAsync();
      }
      await StatusStreaming.Default.SendAllAsync(ApiData.From(post));
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
          param.Status != CountryAllianceStatus.Requesting)
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
        old.Some((o) =>
        {
          if (o.Status != CountryAllianceStatus.Broken)
          {
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
          }
          else
          {
            if (param.Status != CountryAllianceStatus.Requesting)
            {
              // 同盟を結んでいない場合、同盟の要求以外にできることはない
              ErrorCode.NotPermissionError.Throw();
            }
          }

          if (param.Status == CountryAllianceStatus.Available)
          {
            param.BreakingDelay = o.BreakingDelay;
            param.IsPublic = o.IsPublic;
          }
        });
        old.None(() =>
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
        });

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
        if (alliance.IsPublic)
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

        await repo.SaveChangesAsync();
      }

      // 同盟関係を周りに通知
      if (alliance.IsPublic)
      {
        await StatusStreaming.Default.SendAllAsync(ApiData.From(alliance));
        if (mapLog != null)
        {
          await StatusStreaming.Default.SendAllAsync(ApiData.From(mapLog));
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

          param.RequestedCountryId = o.RequestedCountryId;
          param.InsistedCountryId = o.InsistedCountryId;
          param.StartGameDate = o.StartGameDate;
        });
        old.None(() =>
        {
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
        await repo.CountryDiplomacies.SetWarAsync(war);

        // 戦争を周りに通知
        var country1 = await repo.Country.GetAliveByIdAsync(war.RequestedCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var country2 = await repo.Country.GetAliveByIdAsync(war.InsistedCountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        mapLog = new MapLog
        {
          ApiGameDateTime = (await repo.System.GetAsync()).GameDateTime,
          Date = DateTime.Now,
          EventType = EventType.WarInReady,
          IsImportant = true,
          Message = "<country>" + country1.Name + "</country> は、<date>" + war.StartGameDate.ToString() + "</date> より <country>" + country2.Name + "</country> へ侵攻します",
        };
        await repo.MapLog.AddAsync(mapLog);

        await repo.SaveChangesAsync();
      }

      await StatusStreaming.Default.SendAllAsync(ApiData.From(war));
      await StatusStreaming.Default.SendAllAsync(ApiData.From(mapLog));

      if (alliance.HasData)
      {
        var a = alliance.Data;
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(a), a.RequestedCountryId);
        await StatusStreaming.Default.SendCountryAsync(ApiData.From(a), a.InsistedCountryId);
      }
    }

    [AuthenticationFilter]
    [HttpGet("units")]
    public async Task<IEnumerable<Unit>> GetCountryUnitsAsync()
    {
      using (var repo = MainRepository.WithRead())
      {
        var character = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        var country = await repo.Country.GetAliveByIdAsync(character.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        return await repo.Unit.GetByCountryIdAsync(country.Id);
      }
    }

    [AuthenticationFilter]
    [HttpPost("unit")]
    public async Task CreateUnitAsync(
      [FromBody] Unit param)
    {
      if (param == null)
      {
        ErrorCode.LackOfParameterError.Throw();
      }
      var unit = new Unit
      {
        Name = param.Name,
        IsLimited = false,
        Message = param.Message,
      };
      if (string.IsNullOrEmpty(unit.Name))
      {
        ErrorCode.LackOfNameParameterError.Throw();
      }

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var character = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        var country = await repo.Country.GetAliveByIdAsync(character.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);

        var old = await repo.Unit.GetByMemberIdAsync(character.Id);
        if (old.Member.HasData)
        {
          var oldMember = old.Member.Data;
          if (oldMember.Post == UnitMemberPostType.Leader)
          {
            // 隊長は２つ以上の部隊を持てない
            ErrorCode.InvalidOperationError.Throw();
          }
        }

        unit.CountryId = character.CountryId;
        await repo.Unit.AddAsync(unit);
        await repo.SaveChangesAsync();

        await repo.Unit.SetMemberAsync(new UnitMember
        {
          CharacterId = character.Id,
          Post = UnitMemberPostType.Leader,
          UnitId = unit.Id,
        });
        await repo.SaveChangesAsync();
      }
    }

    [AuthenticationFilter]
    [HttpPost("unit/{id}/join")]
    public async Task SetUnitMemberAsync(
      [FromRoute] uint id)
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var character = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        var country = await repo.Country.GetAliveByIdAsync(character.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        var unit = await repo.Unit.GetByIdAsync(id).GetOrErrorAsync(ErrorCode.UnitNotFoundError);

        if (unit.CountryId != character.CountryId)
        {
          // 違う国の部隊には入れない
          ErrorCode.NotPermissionError.Throw();
        }
        if (unit.IsLimited)
        {
          // 入隊制限がかかっている
          ErrorCode.UnitJoinLimitedError.Throw();
        }
        
        var old = await repo.Unit.GetByMemberIdAsync(character.Id);
        if (old.Member.HasData)
        {
          var oldMember = old.Member.Data;
          if (oldMember.Post == UnitMemberPostType.Leader)
          {
            // 隊長は部隊から脱げられない
            ErrorCode.InvalidOperationError.Throw();
          }
          if (oldMember.UnitId == unit.Id)
          {
            ErrorCode.MeaninglessOperationError.Throw();
          }
        }

        await repo.Unit.SetMemberAsync(new UnitMember
        {
          CharacterId = character.Id,
          Post = UnitMemberPostType.Normal,
          UnitId = unit.Id,
        });
        await repo.SaveChangesAsync();
      }
    }

    [AuthenticationFilter]
    [HttpPost("unit/leave")]
    public async Task LeaveUnitMemberAsync()
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var old = await repo.Unit.GetByMemberIdAsync(this.AuthData.CharacterId);
        if (old.Member.HasData)
        {
          var oldMember = old.Member.Data;
          if (oldMember.Post == UnitMemberPostType.Leader)
          {
            // 隊長は部隊から脱げられない
            ErrorCode.InvalidOperationError.Throw();
          }
        }
        else
        {
          ErrorCode.UnitNotFoundError.Throw();
        }

        UnitService.Leave(repo, this.AuthData.CharacterId);
        await repo.SaveChangesAsync();
      }
    }

    [AuthenticationFilter]
    [HttpPut("unit/{id}")]
    public async Task UpdateUnitAsync(
      [FromRoute] uint id,
      [FromBody] Unit unit)
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var member = await repo.Unit.GetByMemberIdAsync(this.AuthData.CharacterId);
        var oldUnit = member.Unit.Data;
        if (member.Unit.HasData && member.Member.HasData)
        {
          var unitMember = member.Member.Data;
          if (unitMember.Post != UnitMemberPostType.Leader)
          {
            ErrorCode.NotPermissionError.Throw();
          }
          if (oldUnit.Id != id)
          {
            ErrorCode.NotPermissionError.Throw();
          }
        }
        else
        {
          ErrorCode.UnitNotFoundError.Throw();
        }

        oldUnit.Message = unit.Message;
        oldUnit.Name = unit.Name;
        oldUnit.IsLimited = unit.IsLimited;
        await repo.SaveChangesAsync();
      }
    }

    [AuthenticationFilter]
    [HttpDelete("unit/{id}")]
    public async Task RemoveUnitAsync(
      [FromRoute] uint id)
    {
      IEnumerable<UnitMember> members = null;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var member = await repo.Unit.GetByMemberIdAsync(this.AuthData.CharacterId);
        if (member.Unit.HasData && member.Member.HasData)
        {
          var unitMember = member.Member.Data;
          var unit = member.Unit.Data;
          if (unitMember.Post != UnitMemberPostType.Leader)
          {
            ErrorCode.NotPermissionError.Throw();
          }
          if (unit.Id != id)
          {
            ErrorCode.NotPermissionError.Throw();
          }
        }
        else
        {
          ErrorCode.UnitNotFoundError.Throw();
        }

        await UnitService.RemoveAsync(repo, id);
        await repo.SaveChangesAsync();
      }
    }

    [HttpGet("battle/{id}")]
    public async Task<BattleLog> GetBattleLogAsync(
      [FromRoute] uint id)
    {
      using (var repo = MainRepository.WithRead())
      {
        var log = await repo.BattleLog.GetWithLinesByIdAsync(id).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        return log;
      }
    }
  }
}
