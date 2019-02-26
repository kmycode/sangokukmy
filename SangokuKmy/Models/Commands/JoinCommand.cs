﻿using System;
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
  public class JoinCommand : Command
  {
    public override CharacterCommandType Type => CharacterCommandType.Join;

    public override async Task ExecuteAsync(MainRepository repo, Character character, IEnumerable<CharacterCommandParameter> options, CommandSystemData game)
    {
      var currentCountryOptional = await repo.Country.GetAliveByIdAsync(character.CountryId);
      if (currentCountryOptional.HasData)
      {
        await game.CharacterLogAsync("仕官は無所属でないと実行できません");
        return;
      }

      var townOptional = await repo.Town.GetByIdAsync(character.TownId);
      if (!townOptional.HasData)
      {
        await game.CharacterLogAsync("ID:" + character.TownId + " の都市は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }

      var town = townOptional.Data;
      if (town.CountryId == 0)
      {
        await game.CharacterLogAsync("<town>" + town.Name + "</town> で仕官しようとしましたが、その都市は国に支配されていません");
        return;
      }

      var countryOptional = await repo.Country.GetAliveByIdAsync(town.CountryId);
      if (!countryOptional.HasData)
      {
        await game.CharacterLogAsync("ID:" + town.CountryId + " の国は存在しません。<emerge>管理者にお問い合わせください</emerge>");
        return;
      }
      var country = countryOptional.Data;

      character.CountryId = country.Id;
      await game.CharacterLogAsync("<country>" + country.Name + "</country> に仕官しました");
      await game.MapLogAsync(EventType.CharacterJoin, "<character>" + character.Name + "</character> は <country>" + country.Name + "</country> に仕官しました", false);

      var townCharas = await repo.Town.GetCharactersWithIconAsync(town.Id);
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(town), townCharas.Select(tc => tc.Character.Id));
      await StatusStreaming.Default.SendCharacterAsync(ApiData.From(country), character.Id);
    }

    public override async Task InputAsync(MainRepository repo, uint characterId, IEnumerable<GameDateTime> gameDates, params CharacterCommandParameter[] options)
    {
      var chara = await repo.Character.GetByIdAsync(characterId).GetOrErrorAsync(ErrorCode.CharacterNotFoundError);
      var country = await repo.Country.GetAliveByIdAsync(chara.CountryId);
      if (country.HasData)
      {
        ErrorCode.NotPermissionError.Throw();
      }

      await repo.CharacterCommand.SetAsync(characterId, this.Type, gameDates);
    }
  }
}
