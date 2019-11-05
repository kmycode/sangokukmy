using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SangokuKmy.Common;
using SangokuKmy.Filters;
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
  public class UnitController : Controller, IAuthenticationDataReceiver
  {
    private readonly ILogger _logger;
    public AuthenticationData AuthData { private get; set; }

    public UnitController(ILogger<SangokuKmyController> logger)
    {
      this._logger = logger;
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
    [HttpGet("units/{id}")]
    public async Task<Unit> GetUnitAsync(
      [FromRoute] uint id = default)
    {
      using (var repo = MainRepository.WithRead())
      {
        var character = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
        var unit = await repo.Unit.GetByIdAsync(id).GetOrErrorAsync(ErrorCode.UnitNotFoundError);
        if (unit.CountryId != character.CountryId)
        {
          ErrorCode.NotPermissionError.Throw();
        }
        return unit;
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

      if (string.IsNullOrEmpty(param.Name) || param.Name.Length > 24)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("name", param.Name.Length, 1, 24));
      }

      if (param.Message?.Length > 240)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("message", param.Message.Length, 1, 240));
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
      if (unit == null)
      {
        ErrorCode.LackOfParameterError.Throw();
      }

      if (string.IsNullOrEmpty(unit.Name) || unit.Name.Length > 24)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("name", unit.Name.Length, 1, 24));
      }

      if (unit.Message?.Length > 240)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("message", unit.Message.Length, 1, 240));
      }

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
    [HttpPut("unit/{id}/leader")]
    public async Task ChangeUnitLeaderAsync(
      [FromRoute] uint id,
      [FromBody] UnitMember newLeader)
    {
      if (newLeader == null)
      {
        ErrorCode.LackOfParameterError.Throw();
      }

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

        var members = await repo.Unit.GetMembersAsync(id);
        var targetMember = members.FirstOrDefault(m => m.CharacterId == newLeader.CharacterId);
        if (targetMember == null)
        {
          ErrorCode.InvalidOperationError.Throw();
        }
        if (targetMember.Post == UnitMemberPostType.Leader)
        {
          ErrorCode.MeaninglessOperationError.Throw();
        }
        var targetChara = await repo.Character.GetByIdAsync(targetMember.CharacterId);
        if (!targetChara.HasData)
        {
          ErrorCode.CharacterNotFoundError.Throw();
        }
        if (targetChara.Data.AiType != CharacterAiType.Human)
        {
          ErrorCode.InvalidOperationError.Throw();
        }
        targetMember.Post = UnitMemberPostType.Leader;
        member.Member.Data.Post = UnitMemberPostType.Normal;

        await repo.SaveChangesAsync();
      }
    }

    [AuthenticationFilter]
    [HttpDelete("unit/{id}")]
    public async Task RemoveUnitAsync(
      [FromRoute] uint id)
    {
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
  }
}
