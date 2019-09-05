using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Services;

namespace SangokuKmy.Models.Commands
{
  public class GenerateItemCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.GenerateItem;

    public static async Task<bool> ResultAsync(MainRepository repo, SystemData system, DelayEffect delay, Func<uint, string, Task> logAsync)
    {
      if (delay.IntAppearGameDateTime + 12 <= system.IntGameDateTime)
      {
        var chara = await repo.Character.GetByIdAsync(delay.CharacterId);
        var type = (CharacterItemType)delay.TypeData;
        var info = CharacterItemInfoes.Get(type);
        if (chara.HasData && info.HasData)
        {
          var item = new CharacterItem
          {
            Type = type,
            Status = CharacterItemStatus.CharacterHold,
            CharacterId = chara.Data.Id,
          };

          if (info.Data.IsResource)
          {
            item.Resource = (ushort)(chara.Data.Strong * 7.6 + RandomService.Next(chara.Data.Strong * 4));
          }

          await ItemService.GenerateItemAndSaveAsync(repo, item);
          await ItemService.SetCharacterPendingAsync(repo, item, chara.Data);

          if (info.Data.IsResource)
          {
            await logAsync(chara.Data.Id, $"アイテム {info.Data.Name} を生成しました。資源量: <num>{item.Resource}</num>");
          }
          else
          {
            await logAsync(chara.Data.Id, $"アイテム {info.Data.Name} を生成しました");
          }

          return true;
        }
      }

      return false;
    }

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var itemTypeOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();
      if (!itemTypeOptional.HasData)
      {
        await game.CharacterLogAsync("アイテム生成のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var itemType = (CharacterItemType)itemTypeOptional.Data.NumberValue;

      var infoOptional = CharacterItemInfoes.Get(itemType);
      if (!infoOptional.HasData)
      {
        await game.CharacterLogAsync($"ID: {(short)itemType} のアイテムは存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var info = infoOptional.Data;

      var money = info.MoneyPerResource * info.DefaultResource / 2;
      if (character.Money < money)
      {
        await game.CharacterLogAsync("アイテム生成しようとしましたが、金が足りません。<num>" + money + "</num> 必要です");
        return;
      }

      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (townOptional.HasData)
      {
        var country = await repo.Country.GetByIdAsync(character.CountryId);

        var town = townOptional.Data;
        if (town.CountryId != character.CountryId && country.HasData && !country.Data.HasOverthrown)
        {
          await game.CharacterLogAsync($"<town>{town.Name}</town> でアイテム生成しようとしましたが、自国の都市ではありません");
          return;
        }

        var delays = await repo.DelayEffect.GetAllAsync();
        if (delays.Any(d => d.CharacterId == character.Id && d.Type == DelayEffectType.GenerateItem))
        {
          await game.CharacterLogAsync($"アイテム生成しようとしましたが、複数のアイテムを同時に生成することはできません");
          return;
        }

        character.Money -= money;
        var delay = new DelayEffect
        {
          CharacterId = character.Id,
          CountryId = character.CountryId,
          AppearGameDateTime = game.GameDateTime,
          Type = DelayEffectType.GenerateItem,
          TypeData = (int)itemType,
        };
        await repo.DelayEffect.AddAsync(delay);

        character.AddStrongEx(500);
        character.Contribution += 200;
        await game.CharacterLogAsync($"<town>{town.Name}</town> で <num>{money}</num> を投し、{info.Name} の生成を開始しました。結果は <num>{game.GameDateTime.Year + 1}</num> 年 <num>{game.GameDateTime.Month}</num> 月に来ます。武力Ex <num>+500</num>");
      }
      else
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var itemType = (CharacterItemType)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;

      var skills = await repo.Character.GetSkillsAsync(characterId);
      if (!skills.AnySkillEffects(CharacterSkillEffectType.GenerateItem, (int)itemType))
      {
        ErrorCode.NotSkillError.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
