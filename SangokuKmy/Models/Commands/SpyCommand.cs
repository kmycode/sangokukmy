using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;

namespace SangokuKmy.Models.Commands
{
  /// <summary>
  /// 偵察
  /// </summary>
  public class SpyCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Spy;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var townIdOptional = options.FirstOrDefault(p => p.Type == 1).ToOptional();

      if (character.Money < 200)
      {
        await game.CharacterLogAsync("偵察に必要な金が足りません");
      }
      if (!townIdOptional.HasData)
      {
        await game.CharacterLogAsync("偵察のパラメータが不正です。<emerge>管理者にお問い合わせください</emerge>");
      }
      else
      {
        var townOptional = await repo.Town.GetByIdAsync((uint)townIdOptional.Data.NumberValue);
        if (!townOptional.HasData)
        {
          await game.CharacterLogAsync("ID:" + townIdOptional.Data + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        }
        else
        {
          var town = townOptional.Data;

          var scoutedTown = ScoutedTown.From(townOptional.Data);
          scoutedTown.ScoutedDateTime = game.GameDateTime;
          scoutedTown.ScoutedCountryId = character.CountryId;
          scoutedTown.ScoutMethod = ScoutMethod.SpyCommand;

          await repo.ScoutedTown.AddScoutAsync(scoutedTown);
          await repo.SaveChangesAsync();

          await StatusStreaming.Default.SendCountryAsync(ApiData.From(scoutedTown), character.CountryId);

          var items = await repo.CharacterItem.GetByTownIdAsync(town.Id);
          var itemInfos = items.GetInfos();

          await game.CharacterLogAsync($"都市 {town.Name} を偵察しました");
          await game.CharacterLogAsync($"都市 {town.Name} のアイテム: {string.Join(", ", itemInfos.Select(i => i.Name))}");

          character.Money -= 200;
          character.Contribution += 40;
          character.AddIntellectEx(50);
        }
      }
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
      var townId = (uint)options.FirstOrDefault(p => p.Type == 1).Or(ErrorCode.LackOfCommandParameter).NumberValue;
      var town = await repo.Town.GetByIdAsync(townId).GetOrErrorAsync(ErrorCode.InternalDataNotFoundError, new { command = "spy", townId, });

      var skills = await repo.Character.GetSkillsAsync(characterId);
      if (!skills.AnySkillEffects(CharacterSkillEffectType.Command, (int)this.Type))
      {
        ErrorCode.NotSkillError.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates, options);
    }
  }
}
