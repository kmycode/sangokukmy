using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SangokuKmy.Common;
using SangokuKmy.Filters;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [ServiceFilter(typeof(SangokuKmyErrorFilterAttribute))]
  [AuthenticationFilter]
  public class SoldierTypeController : Controller, IAuthenticationDataReceiver
  {
    public AuthenticationData AuthData { private get; set; }

    [HttpGet("soldiertypes/{id}")]
    public async Task<CharacterSoldierType> GetAsync([FromRoute] uint id)
    {
      using (var repo = MainRepository.WithRead())
      {
        var type = await repo.CharacterSoldierType.GetByIdAsync(id).GetOrErrorAsync(ErrorCode.SoldierTypeNotFoundError);
        if (type.CharacterId != this.AuthData.CharacterId && type.Status != CharacterSoldierStatus.Available)
        {
          ErrorCode.NotPermissionError.Throw();
        }
        return type;
      }
    }

    [HttpPost("soldiertypes")]
    public async Task<CharacterSoldierType> AddAsync([FromBody] CharacterSoldierType param)
    {
      if (!string.IsNullOrEmpty(param.Name) || param.Name.Length > 10)
      {
        ErrorCode.StringLengthError.Throw(new ErrorCode.RangeErrorParameter("name", param.Name?.Length ?? 0, 1, 10));
      }
      CharacterSoldierType type;
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        if (!(await this.CheckAsync(repo, chara.CountryId, param)))
        {
          ErrorCode.InvalidParameterError.Throw();
        }

        param.CharacterId = this.AuthData.CharacterId;
        param.Status = CharacterSoldierStatus.InDraft;
        await repo.CharacterSoldierType.AddAsync(param);
        await repo.SaveChangesAsync();
        type = param;
      }
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(type), this.AuthData.CharacterId);
      return type;
    }

    [HttpPut("soldiertypes")]
    public async Task<CharacterSoldierType> PutAsync([FromBody] CharacterSoldierType param)
    {
      if (!string.IsNullOrEmpty(param.Name) || param.Name.Length > 10)
      {
        ErrorCode.StringLengthError.Throw(new ErrorCode.RangeErrorParameter("name", param.Name?.Length ?? 0, 1, 10));
      }
      CharacterSoldierType type;
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        if (!(await this.CheckAsync(repo, chara.CountryId, param)))
        {
          ErrorCode.InvalidParameterError.Throw();
        }

        var old = await repo.CharacterSoldierType.GetByIdAsync(param.Id).GetOrErrorAsync(ErrorCode.SoldierTypeNotFoundError);
        if (old.Status != CharacterSoldierStatus.InDraft)
        {
          ErrorCode.InvalidOperationError.Throw();
        }
        if (old.CharacterId != this.AuthData.CharacterId)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        old.Name = param.Name;
        old.Common = param.Common;
        old.Archer = param.Archer;
        old.LightInfantry = param.LightInfantry;
        old.StrongCrossbow = param.StrongCrossbow;
        old.LightCavalry = param.LightCavalry;
        old.LightIntellect = param.LightIntellect;
        old.HeavyInfantry = param.HeavyInfantry;
        old.HeavyCavalry = param.HeavyCavalry;
        old.RepeatingCrossbow = param.RepeatingCrossbow;
        old.Intellect = param.Intellect;
        old.StrongGuards = param.StrongGuards;
        old.Seiran = param.Seiran;

        await repo.SaveChangesAsync();
        type = param;
      }
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(type), this.AuthData.CharacterId);
      return type;
    }

    [HttpDelete("soldiertypes/{id}")]
    public async Task DeleteAsync([FromRoute] uint id)
    {
      CharacterSoldierType type;
      using (var repo = MainRepository.WithRead())
      {
        type = await repo.CharacterSoldierType.GetByIdAsync(id).GetOrErrorAsync(ErrorCode.SoldierTypeNotFoundError);
        if (type.CharacterId != this.AuthData.CharacterId)
        {
          ErrorCode.NotPermissionError.Throw();
        }
        type.Status = CharacterSoldierStatus.Removed;
        await repo.SaveChangesAsync();
      }
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(type), this.AuthData.CharacterId);
    }

    private async Task<bool> CheckAsync(MainRepository repo, uint countryId, CharacterSoldierType type)
    {
      return type.IsVerify && type.Size >= 10 && type.Size <= 15 && type.ToParts().All(t => t.CanConscript);
    }
  }
}
