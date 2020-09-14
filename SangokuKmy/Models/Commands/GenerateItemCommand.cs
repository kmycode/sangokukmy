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
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  public class GenerateItemCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.GenerateItem;

    private class GenerateItemInfo
    {
      private static readonly List<GenerateItemInfo> infos = new List<GenerateItemInfo>
      {
        new GenerateItemInfo
        {
          From = CharacterFrom.Engineer,
          ItemType = CharacterItemType.EquippedInfantry,
          ResourceAttribute = c => c.Strong,
          AddExAttribute = c => c.AddStrongEx(200),
          Length = 12,
          Contribution = 80,
        },
        new GenerateItemInfo
        {
          From = CharacterFrom.Engineer,
          ItemType = CharacterItemType.EquippedCavalry,
          ResourceAttribute = c => c.Strong,
          AddExAttribute = c => c.AddStrongEx(200),
          Length = 12,
          Contribution = 80,
        },
        new GenerateItemInfo
        {
          From = CharacterFrom.Engineer,
          ItemType = CharacterItemType.EquippedCrossbow,
          ResourceAttribute = c => c.Strong,
          AddExAttribute = c => c.AddStrongEx(200),
          Length = 12,
          Contribution = 80,
        },
        new GenerateItemInfo
        {
          From = CharacterFrom.Engineer,
          ItemType = CharacterItemType.EquippedHeavyGeki,
          ResourceAttribute = c => c.Strong,
          AddExAttribute = c => c.AddStrongEx(200),
          Length = 12,
          Contribution = 80,
        },
        new GenerateItemInfo
        {
          From = CharacterFrom.Engineer,
          ItemType = CharacterItemType.EquippedHeavyHorse,
          ResourceAttribute = c => c.Strong,
          AddExAttribute = c => c.AddStrongEx(200),
          Length = 12,
          Contribution = 80,
        },
        new GenerateItemInfo
        {
          From = CharacterFrom.Engineer,
          ItemType = CharacterItemType.EquippedStrongCrossbow,
          ResourceAttribute = c => c.Strong,
          AddExAttribute = c => c.AddStrongEx(200),
          Length = 12,
          Contribution = 80,
        },
        new GenerateItemInfo
        {
          From = CharacterFrom.Engineer,
          ItemType = CharacterItemType.EquippedRepeatingCrossbow,
          ResourceAttribute = c => c.Strong,
          AddExAttribute = c => c.AddStrongEx(200),
          Length = 12,
          Contribution = 80,
        },
        new GenerateItemInfo
        {
          From = CharacterFrom.Engineer,
          ItemType = CharacterItemType.EquippedSeishuYari,
          ResourceAttribute = c => c.Strong,
          AddExAttribute = c => c.AddStrongEx(200),
          Length = 12,
          Contribution = 80,
        },
        new GenerateItemInfo
        {
          From = CharacterFrom.Engineer,
          ItemType = CharacterItemType.EquippedChariot,
          ResourceAttribute = c => c.Strong,
          AddExAttribute = c => c.AddStrongEx(200),
          Length = 12,
          Contribution = 80,
        },
        new GenerateItemInfo
        {
          From = CharacterFrom.Terrorist,
          ItemType = CharacterItemType.Elephant,
          ResourceAttribute = c => c.Strong,
          AddExAttribute = c => c.AddStrongEx(200),
          Length = 12,
          Contribution = 80,
        },
        new GenerateItemInfo
        {
          From = CharacterFrom.Terrorist,
          ItemType = CharacterItemType.Toko,
          ResourceAttribute = c => c.Strong,
          AddExAttribute = c => c.AddStrongEx(200),
          Length = 12,
          Contribution = 80,
        },
        new GenerateItemInfo
        {
          From = CharacterFrom.People,
          ItemType = CharacterItemType.SuperSoldier,
          ResourceAttribute = c => c.Popularity,
          AddExAttribute = c => c.AddPopularityEx(200),
          Length = 12,
          Contribution = 80,
        },
        new GenerateItemInfo
        {
          From = CharacterFrom.People,
          ItemType = CharacterItemType.EliteSoldier,
          ResourceAttribute = c => c.Popularity,
          AddExAttribute = c => c.AddPopularityEx(200),
          Length = 12,
          Contribution = 80,
        },
        new GenerateItemInfo
        {
          From = CharacterFrom.Tactician,
          ItemType = CharacterItemType.MartialArtsBook,
          ResourceAttribute = c => c.Leadership,
          AddExAttribute = c => c.AddLeadershipEx(280),
          Length = 24,
          Contribution = 120,
        },
        new GenerateItemInfo
        {
          From = CharacterFrom.Scholar,
          ItemType = CharacterItemType.AnnotationBook,
          ResourceAttribute = c => c.Intellect,
          AddExAttribute = c => c.AddIntellectEx(1700),
          Length = 96,
          Contribution = 700,
        },
        new GenerateItemInfo
        {
          From = CharacterFrom.Scholar,
          ItemType = CharacterItemType.PrivateBook,
          ResourceAttribute = c => c.Intellect,
          AddExAttribute = c => c.AddIntellectEx(2000),
          Length = 120,
          Contribution = 800,
        },
      };

      public CharacterFrom From { get; set; }
      public CharacterItemType ItemType { get; set; }
      public Func<Character, int> ResourceAttribute { get; set; } = c => 0;
      public Action<Character> AddExAttribute { get; set; } = c => { };
      public int Length { get; set; }
      public int Contribution { get; set; }

      private GenerateItemInfo() { }

      public static GenerateItemInfo GetInfo(Character chara, CharacterItemType type)
      {
        return infos.FirstOrDefault(i => i.From == chara.From && i.ItemType == type);
      }
    }

    public static async Task<bool> ResultAsync(MainRepository repo, SystemData system, DelayEffect delay, Func<uint, string, Task> logAsync)
    {
      var chara = await repo.Character.GetByIdAsync(delay.CharacterId);
      var type = (CharacterItemType)delay.TypeData;
      if (!chara.HasData)
      {
        return false;
      }
      if (chara.Data.HasRemoved)
      {
        return true;
      }

      var generateInfo = GenerateItemInfo.GetInfo(chara.Data, type);
      if (generateInfo == null)
      {
        return false;
      }

      if (delay.IntAppearGameDateTime + generateInfo.Length <= system.IntGameDateTime)
      {
        var info = CharacterItemInfoes.Get(type);
        if (info.HasData)
        {
          var item = new CharacterItem
          {
            Type = type,
            Status = CharacterItemStatus.CharacterPending,
            CharacterId = chara.Data.Id,
            IntLastStatusChangedGameDate = system.IntGameDateTime,
          };

          if (info.Data.IsResource)
          {
            item.Resource = (ushort)(info.Data.DefaultResource + RandomService.Next((int)(generateInfo.ResourceAttribute(chara.Data) * 1.4f / info.Data.DefaultResource * 1000)));
          }

          await ItemService.GenerateItemAndSaveAsync(repo, item);
          await ItemService.SetCharacterPendingAsync(repo, item, chara.Data);

          if (info.Data.IsResource)
          {
            await logAsync(chara.Data.Id, $"アイテム {info.Data.Name} を生産しました。資源量: <num>{item.Resource}</num>");
          }
          else
          {
            await logAsync(chara.Data.Id, $"アイテム {info.Data.Name} を生産しました");
          }

          return true;
        }
      }

      return false;
    }

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var isRegularly = options.Any(p => p.Type == CharacterCommandParameterTypes.Regularly);

      var itemTypeOptional = options.FirstOrDefault(p => p.Type == 1);
      if (itemTypeOptional == null || itemTypeOptional.NumberValue == null)
      {
        await game.CharacterLogAsync("アイテム生産のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var itemType = (CharacterItemType)itemTypeOptional.NumberValue;

      var delays = await repo.DelayEffect.GetAllAsync();
      if (delays.Any(d => d.CharacterId == character.Id && d.Type == DelayEffectType.GenerateItem))
      {
        if (!isRegularly)
        {
          await game.CharacterLogAsync($"アイテム生産しようとしましたが、複数のアイテムを同時に生産することはできません");
        }
        return;
      }

      var infoOptional = CharacterItemInfoes.Get(itemType);
      if (!infoOptional.HasData)
      {
        await game.CharacterLogAsync($"ID: {(short)itemType} のアイテムは存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var info = infoOptional.Data;

      var generateInfo = GenerateItemInfo.GetInfo(character, itemType);
      if (generateInfo == null)
      {
        await game.CharacterLogAsync("アイテム生産の情報が不正です。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }

      var money = info.IsResource ? info.MoneyPerResource * info.DefaultResource / 16 : info.Money / 16;
      if (character.Money < money)
      {
        await game.CharacterLogAsync("アイテム生産しようとしましたが、金が足りません。<num>" + money + "</num> 必要です");
        return;
      }

      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (townOptional.HasData)
      {
        var country = await repo.Country.GetByIdAsync(character.CountryId);

        var town = townOptional.Data;
        if (town.CountryId != character.CountryId && country.HasData && !country.Data.HasOverthrown)
        {
          await game.CharacterLogAsync($"<town>{town.Name}</town> でアイテム生産しようとしましたが、自国の都市ではありません");
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
        await repo.SaveChangesAsync();

        await StatusStreaming.Default.SendCharacterAsync(ApiData.From(delay), character.Id);

        var finish = GameDateTime.FromInt(game.GameDateTime.ToInt() + generateInfo.Length);
        generateInfo.AddExAttribute(character);
        if (!isRegularly)
        {
          character.Contribution += generateInfo.Contribution;
          character.SkillPoint++;
        }
        await game.CharacterLogAsync($"<town>{town.Name}</town> で <num>{money}</num> を投し、{info.Name} の生産を開始しました。結果は <num>{finish.Year}</num> 年 <num>{finish.Month}</num> 月に来ます");
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

      var character = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
      var info = GenerateItemInfo.GetInfo(character, itemType);
      if (info == null)
      {
        ErrorCode.InvalidCommandParameter.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
