using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SangokuKmy.Models.Data.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Commands;

namespace SangokuKmy.Models.Data.Repositories
{
  public class CharacterCommandRepository
  {
    private readonly IRepositoryContainer container;

    public CharacterCommandRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// 指定した武将の設定したすべてのコマンドを取得
    /// </summary>
    /// <returns>すべての入力済コマンド</returns>
    /// <param name="characterId">武将ID</param>
    public async Task<IEnumerable<CharacterCommand>> GetAllAsync(uint characterId)
    {
      try
      {
        return (await this.container.Context.CharacterCommands
          .Where(c => c.CharacterId == characterId)
          .OrderBy(c => c.IntGameDateTime)
          .ToArrayAsync());
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }

    /// <summary>
    /// 指定した武将のコマンドを設定
    /// </summary>
    /// <param name="characterId">武将ID</param>
    /// <param name="type">コマンドの種類</param>
    /// <param name="target">入力先の年月</param>
    /// <param name="parameters">コマンドパラメータ</param>
    public async Task SetAsync(uint characterId, CharacterCommandType type, GameDateTime target, params CharacterCommandParameter[] parameters)
    {
      try
      {
        var targetInt = target.ToInt();
        var old = await this.container.Context.CharacterCommands
          .FirstOrDefaultAsync((c) => c.CharacterId == characterId && c.IntGameDateTime == targetInt);

        uint id;
        if (old != null)
        {
          // 古いコマンドを置き換え
          old.CharacterId = characterId;
          old.Type = type;
          old.IntGameDateTime = targetInt;
          id = old.Id;

          // パラメータを削除
          if (parameters.Any())
          {
            var removeParameters = this.container.Context.CharacterCommandParameters
              .Where((cp) => cp.CharacterCommandId == old.Id);
            this.container.Context.CharacterCommandParameters.RemoveRange(removeParameters);
          }
        }
        else
        {
          var newCommand = new CharacterCommand
          {
            CharacterId = characterId,
            Type = type,
            IntGameDateTime = targetInt,
          };

          // 新しくコマンドを追加
          await this.container.Context.CharacterCommands.AddAsync(newCommand);

          id = newCommand.Id;
        }

        // パラメータを保存
        if (parameters.Any())
        {
          foreach (var param in parameters)
          {
            param.Id = default;
            param.CharacterCommandId = id;
          }
          await this.container.Context.CharacterCommandParameters.AddRangeAsync(parameters);
        }
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
      }
    }
  }
}
