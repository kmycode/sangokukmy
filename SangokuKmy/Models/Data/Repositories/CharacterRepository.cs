using System;
using System.Linq;
using SangokuKmy.Models.Common.Definitions;
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
    public uint GetIdFromAliasId(string aliasId)
    {
      try
      {
        return this.container.Context.Characters.FirstOrDefault(c => c.AliasId == aliasId)?.Id ?? (uint)0;
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return 0;
      }
    }

    /// <summary>
    /// ログインのときに必要となる情報だけを取得
    /// </summary>
    /// <returns>ログイン情報</returns>
    /// <param name="aliasId">エイリアスID</param>
    public (uint Id, string PasswordHash) GetLoginParameter(string aliasId)
    {
      try
      {
        var chara = this.container.Context.Characters.FirstOrDefault(c => c.AliasId == aliasId);
        if (chara == null)
        {
          ErrorCode.LoginCharacterNotFoundError.Throw();
        }
        return (chara.Id, chara.PasswordHash);
      }
      catch (SangokuKmyException ex)
      {
        throw ex;
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }
  }
}
