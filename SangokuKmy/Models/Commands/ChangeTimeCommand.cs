using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Migrations;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  public class ChangeTimeCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.ChangeTime;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var items = await repo.Character.GetItemsAsync(character.Id);
      var item = items.FirstOrDefault(i => i.Type == CharacterItemType.TimeChanger);
      if (item == null || item.Resource <= 0)
      {
        if (item != null)
        {
          await ItemService.SpendCharacterAsync(repo, item, character);
        }
        await game.CharacterLogAsync("静養しようとしましたが、コマンド実行に必要なアイテムを所持していません");
        return;
      }

      var system = await repo.System.GetAsync();
      var time = RandomService.Next(0, Config.UpdateTime * 100_000) / 100_000.0;
      character.LastUpdated = system.CurrentMonthStartDateTime.AddSeconds(time - Config.UpdateTime);

      item.Resource--;
      if (item.Resource <= 0)
      {
        await ItemService.SpendCharacterAsync(repo, item, character);
      }
      else
      {
        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(item), character.Id);
      }

      await game.CharacterLogAsync($"更新時刻を月開始の <num>{(int)time}</num> 秒後に変更しました。アイテム残り: <num>{item.Resource}</num>");
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates);
    }
  }
}
