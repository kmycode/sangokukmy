using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;

namespace SangokuKmy.Models.Commands
{
  /// <summary>
  /// 移動
  /// </summary>
  public class MoveCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Move;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, Func<string, Task> loggerAsync)
    {
      var townIdOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();

      if (!townIdOptional.HasData)
      {
        await loggerAsync("徴兵のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
      }
      else
      {
        var townOptional = await repo.Town.GetByIdAsync((uint)townIdOptional.Data.NumberValue);
        var currentTownOptional = await repo.Town.GetByIdAsync(character.TownId);
        if (!townOptional.HasData)
        {
          await loggerAsync("ID:" + townIdOptional.Data + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        }
        else if (!currentTownOptional.HasData)
        {
          await loggerAsync("現在所在するID:" + townIdOptional.Data + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        }
        else
        {
          var town = townOptional.Data;
          var currentTown = currentTownOptional.Data;

          if (town.Id == currentTown.Id)
          {
            await loggerAsync("<town>" + town.Name + "</town> に移動しようとしましたが、すでに所在しています");
          }
          else if (Math.Abs(currentTown.X - town.X) <= 1 &&
            Math.Abs(currentTown.Y - town.Y) <= 1)
          {
            character.TownId = town.Id;
            character.AddLeadershipEx(50);

            var countryOptional = await repo.Country.GetByIdAsync(character.CountryId);
            if (countryOptional.HasData && !countryOptional.Data.HasOverthrown)
            {
              character.Contribution += 20;
            }

            await loggerAsync("<town>" + town.Name + "</town> へ移動しました");
          }
          else
          {
            await loggerAsync("現在都市と隣接していないため、 <town>" + town.Name + "</town> へ移動できませんでした");
          }
        }
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
      var townId = (uint)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var town = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.InternalDataNotFoundError, new { command = "move", townId, });

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
