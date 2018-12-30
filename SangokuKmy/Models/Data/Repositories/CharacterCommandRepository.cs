using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SangokuKmy.Models.Data.Entities;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Commands;
using SangokuKmy.Common;

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
    /// 指定した武将の指定した年月のコマンドを取得する
    /// </summary>
    /// <returns>コマンド</returns>
    /// <param name="characterId">武将ID</param>
    /// <param name="month">取得する年月</param>
    public async Task<Optional<CharacterCommand>> GetAsync(uint characterId, GameDateTime month)
    {
      var monthInt = month.ToInt();

      try
      {
        var newest = await this.container.Context.CharacterCommands
          .Where(c => c.IntGameDateTime == monthInt)
          .GroupJoin(this.container.Context.CharacterCommandParameters,
            c => c.Id,
            cp => cp.CharacterCommandId,
            (c, cps) => c.SetParameters(cps))
          .FirstOrDefaultAsync()
          .ToOptionalAsync();
        return newest;
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }

    /// <summary>
    /// 指定した武将の指定した年月を含めた、古いコマンドを削除する
    /// </summary>
    /// <returns>コマンド</returns>
    /// <param name="characterId">武将ID</param>
    /// <param name="month">削除する年月のうち、一番新しいもの</param>
    public void RemoveOlds(uint characterId, GameDateTime month)
    {
      var monthInt = month.ToInt();

      try
      {
        var commands = this.container.Context.CharacterCommands.Where(c => c.IntGameDateTime <= monthInt).ToArray();
        var commandParameters = this.container.Context.CharacterCommandParameters.Where(cp => commands.Select(c => c.Id).Contains(cp.CharacterCommandId));
        this.container.Context.CharacterCommands.RemoveRange(commands);
        this.container.Context.CharacterCommandParameters.RemoveRange(commandParameters);
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
      }
    }

    /// <summary>
    /// 指定した武将の設定したすべてのコマンドを取得
    /// </summary>
    /// <returns>すべての入力済コマンド</returns>
    /// <param name="characterId">武将ID</param>
    /// <param name="startMonth">最初の月</param>
    public async Task<IEnumerable<CharacterCommand>> GetAllAsync(uint characterId, GameDateTime startMonth)
    {
      var intStartMonth = startMonth.ToInt();

      try
      {
        return (await this.container.Context.CharacterCommands
          .Where(c => c.CharacterId == characterId && c.IntGameDateTime >= intStartMonth)
          .OrderBy(c => c.IntGameDateTime)
          .GroupJoin(this.container.Context.CharacterCommandParameters,
            c => c.Id,
            cp => cp.CharacterCommandId,
            (c, cps) => c.SetParameters(cps))
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
    /// <param name="targets">入力先の年月</param>
    /// <param name="parameters">コマンドパラメータ</param>
    public async Task SetAsync(uint characterId, CharacterCommandType type, IEnumerable<GameDateTime> targets, params CharacterCommandParameter[] parameters)
    {
      try
      {
        var intTargets = targets.Select(t => t.ToInt());
        var existCommands = await this.container.Context.CharacterCommands
          .Where(c => c.CharacterId == characterId)
          .Where(c => intTargets.Contains(c.IntGameDateTime))
          .GroupJoin(this.container.Context.CharacterCommandParameters, c => c.Id, cp => cp.CharacterCommandId, (c, cps) => new { Command = c, CommandParameters = cps, })
          .ToArrayAsync();
        var existCommandIds = new List<uint>();

        // 存在するコマンドを置き換え
        this.container.Context.CharacterCommandParameters.RemoveRange(existCommands.SelectMany(c => c.CommandParameters));
        foreach (var existCommand in existCommands)
        {
          existCommand.Command.Type = type;
          existCommandIds.Add(existCommand.Command.Id);
        }


        // 新しいコマンドを追加
        var newCommandDates = intTargets
          .Except(existCommands.Select(c => c.Command.IntGameDateTime))
          .Select(d => new CharacterCommand
          {
            CharacterId = characterId,
            Type = type,
            IntGameDateTime = d,
          })
          .ToArray();
        await this.container.Context.CharacterCommands.AddRangeAsync(newCommandDates);

        // パラメータを保存
        if (parameters.Any())
        {
          await this.container.Context.SaveChangesAsync();
          var newParameters = existCommandIds
            .Concat(newCommandDates.Select(d => d.Id))
            .SelectMany(id => parameters.Select(cp => new CharacterCommandParameter
            {
              CharacterCommandId = id,
              Type = cp.Type,
              NumberValue = cp.NumberValue,
              StringValue = cp.StringValue,
            }));
          await this.container.Context.CharacterCommandParameters.AddRangeAsync(newParameters);
        }
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
      }
    }
  }
}
