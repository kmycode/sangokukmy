using System;
using System.Linq;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Data.Repositories
{
  public class CharacterRepository
  {
    private readonly IRepositoryContainer container;

    public CharacterRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// エイリアスIDからIDを取得
    /// </summary>
    /// <returns>武将ID</returns>
    /// <param name="aliasId">エイリアスID</param>
    public uint GetIdByAliasId(string aliasId)
    {
      try
      {
        return this.GetByAliasId(aliasId).Data?.Id ?? (uint)0;
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return 0;
      }
    }

    /// <summary>
    /// IDから武将を取得する
    /// </summary>
    /// <returns>武将</returns>
    /// <param name="id">ID</param>
    public Optional<Character> GetById(uint id)
    {
      try
      {
        return this.container.Context.Characters.Find(id).ToOptional();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return Optional<Character>.Null();
      }
    }

    /// <summary>
    /// エイリアスIDから武将を取得する
    /// </summary>
    /// <returns>武将</returns>
    /// <param name="aliasId">エイリアスID</param>
    public Optional<Character> GetByAliasId(string aliasId)
    {
      try
      {
        return this.container.Context.Characters.FirstOrDefault(c => c.AliasId == aliasId).ToOptional();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return Optional<Character>.Null();
      }
    }
  }
}
