using Microsoft.EntityFrameworkCore;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Models.Data.ApiEntities;

namespace SangokuKmy.Models.Data.Repositories
{
  public class UnitRepository
  {
    private readonly IRepositoryContainer container;

    public UnitRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// IDから部隊を取得する
    /// </summary>
    /// <returns>部隊</returns>
    /// <param name="id">ID</param>
    public async Task<Optional<Unit>> GetByIdAsync(uint id)
    {
      try
      {
        return await this.container.Context.Units
          .FindAsync(id)
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 国IDから部隊を取得する
    /// </summary>
    /// <returns>部隊</returns>
    /// <param name="id">国ID</param>
    public async Task<IReadOnlyList<Unit>> GetByCountryIdAsync(uint id)
    {
      try
      {
        var tmp = await this.container.Context.Units
          .Where(u => u.CountryId == id)
          .GroupJoin(this.container.Context.UnitMembers
            .Join(
              this.container.Context.Characters
                .GroupJoin(this.container.Context.CharacterIcons, c => c.Id, ci => ci.CharacterId, (c, ci) => new { Character = c, Icons = ci, }),
              um => um.CharacterId,
              c => c.Character.Id,
              (um, c) => new { Member = um, c.Character, c.Icons, }),
            u => u.Id,
            um => um.Member.UnitId,
            (u, ums) => new { Unit = u, Members = ums, })
          .ToArrayAsync();
        return tmp.Select(val =>
        {
          val.Unit.Members = val.Members.Select(umd =>
          {
            var um = umd.Member;
            um.Character = new CharacterForAnonymous(umd.Character, umd.Icons.GetMainOrFirst().Data, CharacterShareLevel.Anonymous);
            return um;
          }).ToArray();
          return val.Unit;
        }).ToList();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 武将IDから部隊を取得する
    /// </summary>
    /// <returns>部隊</returns>
    /// <param name="id">武将ID</param>
    public async Task<(Optional<Unit> Unit, Optional<UnitMember> Member)> GetByMemberIdAsync(uint id)
    {
      try
      {
        var member = await this.container.Context.UnitMembers
          .FirstOrDefaultAsync(um => um.CharacterId == id);
        if (member != null)
        {
          var unit = await this.container.Context.Units
            .FirstOrDefaultAsync(u => u.Id == member.UnitId);
          return (unit.ToOptional(), member.ToOptional());
        }
        return default;
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 部隊を追加する
    /// </summary>
    /// <param name="unit">追加する部隊</param>
    public async Task AddAsync(Unit unit)
    {
      try
      {
        await this.container.Context.Units
          .AddAsync(unit);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// メンバを取得する
    /// </summary>
    /// <param name="id">ID</param>
    public async Task<IReadOnlyList<UnitMember>> GetMembersAsync(uint id)
    {
      try
      {
        return await this.container.Context.UnitMembers
          .Where(um => um.UnitId == id)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 部隊のメンバを追加する
    /// </summary>
    /// <param name="member">部隊長の情報</param>
    public async Task SetMemberAsync(UnitMember member)
    {
      try
      {
        var olds = this.container.Context.UnitMembers
          .Where(um => um.CharacterId == member.CharacterId);
        this.container.Context.UnitMembers.RemoveRange(olds);

        await this.container.Context.UnitMembers
          .AddAsync(member);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 部隊のメンバを削除する
    /// </summary>
    /// <param name="characterId">武将ID</param>
    public void RemoveMember(uint characterId)
    {
      try
      {
        var olds = this.container.Context.UnitMembers
          .Where(um => um.CharacterId == characterId);
        this.container.Context.UnitMembers.RemoveRange(olds);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 部隊を削除する
    /// </summary>
    /// <param name="id">ID</param>
    public void RemoveUnit(uint id)
    {
      try
      {
        var unit = this.container.Context.Units.Where(u => u.Id == id);
        this.container.Context.Units.RemoveRange(unit);

        var oldMembers = this.container.Context.UnitMembers
          .Where(um => um.UnitId == id);
        this.container.Context.UnitMembers.RemoveRange(oldMembers);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 指定した国の部隊をすべて削除する
    /// </summary>
    /// <param name="countryId">国ID</param>
    public void RemoveUnitsByCountryId(uint countryId)
    {
      try
      {
        var units = this.container.Context.Units
          .Where(u => u.CountryId == countryId)
          .GroupJoin(this.container.Context.UnitMembers, u => u.Id, um => um.UnitId, (u, ums) => new { Unit = u, Members = ums, });
        foreach (var unit in units)
        {
          this.container.Context.Units.Remove(unit.Unit);
          this.container.Context.UnitMembers.RemoveRange(unit.Members);
        }
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
