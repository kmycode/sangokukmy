using SangokuKmy.Common;
using SangokuKmy.Models.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Services
{
  public static class EntryService
  {
    public static int GetAttributeMax(GameDateTime current) => 100 + (Math.Max(current.Year, Config.UpdateStartYear) - Config.UpdateStartYear) / 16;
    public static int GetAttributeSumMax(GameDateTime current) => 200 + (Math.Max(current.Year, Config.UpdateStartYear) - Config.UpdateStartYear) / 4;

    public static async Task EntryAsync(MainRepository repo, Character newChara, CharacterIcon newIcon, string password, Country newCountry)
    {
      var town = await repo.Town.GetByIdAsync(newChara.TownId).GetOrErrorAsync(ErrorCode.TownNotFoundError);

      var system = await repo.System.GetAsync();
      CheckEntryStatus(system.GameDateTime, newChara, password, newIcon, town, newCountry);

      // 既存との重複チェック
      if (await repo.Character.IsAlreadyExistsAsync(newChara.Name, newChara.AliasId)) {
        ErrorCode.DuplicateCharacterNameOrAliasIdError.Throw();
      }

      var updateCountriesRequested = false;
      MapLog maplog = null;
      var chara = new Character
      {
        Name = newChara.Name,
        AliasId = newChara.AliasId,
        Strong = newChara.Strong,
        StrongEx = 0,
        Intellect = newChara.Intellect,
        IntellectEx = 0,
        Leadership = newChara.Leadership,
        LeadershipEx = 0,
        Popularity = newChara.Popularity,
        PopularityEx = 0,
        Contribution = 0,
        Class = 0,
        DeleteTurn = 0,
        LastUpdated = DateTime.Now,
        LastUpdatedGameDate = system.GameDateTime,
        Message = newChara.Message,
        Money = 1000,
        Rice = 500,
        SoldierType = SoldierType.Common,
        SoldierNumber = 0,
        Proficiency = 0,
        TownId = newChara.TownId,
      };
      chara.SetPassword(password);

      if (town.CountryId > 0)
      {
        // 武将総数チェック
        var country = await repo.Country.GetByIdAsync(town.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        if (country.IntEstablished + Config.CountryBattleStopDuring > system.GameDateTime.ToInt())
        {
          var countryCharaCount = await repo.Country.CountCharactersAsync(country.Id);
          if (countryCharaCount >= Config.CountryJoinMaxOnLimited)
          {
            ErrorCode.CantJoinAtSuchCountryhError.Throw();
          }
          else if (countryCharaCount + 1 == Config.CountryJoinMaxOnLimited)
          {
            updateCountriesRequested = true;
          }
        }

        chara.CountryId = town.CountryId;
        await repo.Character.AddAsync(chara);

        maplog = new MapLog
        {
          Date = DateTime.Now,
          ApiGameDateTime = system.GameDateTime,
          EventType = EventType.CharacterEntry,
          IsImportant = false,
          Message = $"<character>{chara.Name}</character> が <country>{country.Name}</country> に仕官しました",
        };
        await repo.MapLog.AddAsync(maplog);
      }
      else
      {
        // 重複チェック
        if ((await repo.Country.GetAllAsync()).Any(c => c.Name == newCountry.Name || c.CountryColorId == newCountry.CountryColorId))
        {
          ErrorCode.DuplicateCountryNameOrColorError.Throw();
        }

        var country = new Country
        {
          Name = newCountry.Name,
          CountryColorId = newCountry.CountryColorId,
          CapitalTownId = newChara.TownId,
          IntEstablished = Math.Max(system.GameDateTime.ToInt(), new GameDateTime { Year = Config.UpdateStartYear, Month = 1, }.ToInt()),
          HasOverthrown = false,
          IntOverthrownGameDate = 0,
          LastMoneyIncomes = 0,
          LastRiceIncomes = 0,
        };
        updateCountriesRequested = true;
        await repo.Country.AddAsync(country);
        await repo.SaveChangesAsync();

        chara.CountryId = country.Id;
        town.CountryId = country.Id;
        await repo.Character.AddAsync(chara);
        await repo.SaveChangesAsync();

        var countryPost = new CountryPost
        {
          Type = CountryPostType.Monarch,
          CountryId = country.Id,
          CharacterId = chara.Id,
        };
        await repo.Country.SetPostAsync(countryPost);

        maplog = new MapLog
        {
          Date = DateTime.Now,
          ApiGameDateTime = system.GameDateTime,
          EventType = EventType.Publish,
          IsImportant = true,
          Message = $"<character>{chara.Name}</character> が <town>{town.Name}</town> に <country>{country.Name}</country> を建国しました",
        };
        await repo.MapLog.AddAsync(maplog);
      }

      await repo.SaveChangesAsync();

      var icon = new CharacterIcon
      {
        Type = newIcon.Type,
        IsAvailable = true,
        IsMain = true,
        FileName = newIcon.FileName,
        CharacterId = chara.Id,
      };
      await repo.Character.AddCharacterIconAsync(icon);

      await repo.SaveChangesAsync();

      if (updateCountriesRequested)
      {
        var countries = await repo.Country.GetAllForAnonymousAsync();
        await AnonymousStreaming.Default.SendAllAsync(countries.Select(c => ApiData.From(c)));
        await StatusStreaming.Default.SendAllAsync(countries.Select(c => ApiData.From(c)));
      }

      var townData = ApiData.From(new TownForAnonymous(town));
      var maplogData = ApiData.From(maplog);
      await AnonymousStreaming.Default.SendAllAsync(townData);
      await AnonymousStreaming.Default.SendAllAsync(maplogData);
      await StatusStreaming.Default.SendAllAsync(townData);
      await StatusStreaming.Default.SendAllAsync(maplogData);
    }

    private static void CheckEntryStatus(GameDateTime current, Character chara, string password, CharacterIcon icon, Town town, Country country)
    {
      if (string.IsNullOrEmpty(password))
      {
        ErrorCode.LackOfParameterError.Throw();
      }
      if (password.Length < 4 || password.Length > 12)
      {
        ErrorCode.StringLengthError.Throw(new ErrorCode.RangeErrorParameter("password", password.Length, 4, 12));
      }
      if (string.IsNullOrEmpty(chara.Name))
      {
        ErrorCode.LackOfNameParameterError.Throw();
      }
      if (chara.Name.Length < 1 || chara.Name.Length > 12)
      {
        ErrorCode.StringLengthError.Throw(new ErrorCode.RangeErrorParameter("name", chara.Name.Length, 1, 12));
      }
      if (string.IsNullOrEmpty(chara.AliasId))
      {
        ErrorCode.LackOfParameterError.Throw();
      }
      if (chara.AliasId.Length < 4 || chara.AliasId.Length > 12)
      {
        ErrorCode.StringLengthError.Throw(new ErrorCode.RangeErrorParameter("aliasId", chara.AliasId.Length, 4, 12));
      }

      if (icon.Type == CharacterIconType.Default)
      {
        var iconIdStr = icon.FileName.Split('.');
        if (uint.TryParse(iconIdStr[0], out uint iconId))
        {
          if (iconId < 0 || iconId > 99)
          {
            ErrorCode.CharacterIconNotFoundError.Throw();
          }
        }
        else
        {
          ErrorCode.CharacterIconNotFoundError.Throw();
        }
      }
      else if (icon.Type == CharacterIconType.Gravatar)
      {
        if (string.IsNullOrEmpty(icon.FileName))
        {
          ErrorCode.LackOfParameterError.Throw();
        }
      }
      else
      {
        ErrorCode.InvalidParameterError.Throw();
      }

      var attributeMax = GetAttributeMax(current);
      var attributeSumMax = GetAttributeSumMax(current);
      if (chara.Strong < 5 || chara.Intellect < 5 || chara.Leadership < 5 || chara.Popularity < 5)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("attribute", 0, 5, attributeMax));
      }
      if (chara.Strong > attributeMax || chara.Intellect > attributeMax || chara.Leadership > attributeMax || chara.Popularity > attributeMax)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("attribute", 0, 5, attributeMax));
      }
      if (chara.Strong + chara.Intellect + chara.Leadership + chara.Popularity < attributeSumMax)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("sumOfAttribute", 0, attributeSumMax, attributeSumMax));
      }
      if (chara.Strong + chara.Intellect + chara.Leadership + chara.Popularity > attributeSumMax)
      {
        ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("sumOfAttribute", 0, attributeSumMax, attributeSumMax));
      }

      if (country != null && (!string.IsNullOrEmpty(country.Name) || country.CountryColorId != 0))
      {
        if (country.CountryColorId < 1 || country.CountryColorId > Config.CountryColorMax)
        {
          ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("countryColor", country.CountryColorId, 1, Config.CountryColorMax));
        }
        if (string.IsNullOrEmpty(country.Name) || country.Name.Length < 1 || country.Name.Length > 8)
        {
          ErrorCode.NumberRangeError.Throw(new ErrorCode.RangeErrorParameter("countryName", country.Name.Length, 1, 8));
        }
        if (town.CountryId > 0)
        {
          ErrorCode.CantPublisAtSuchTownhError.Throw();
        }
      }
      else
      {
        if (town.CountryId <= 0)
        {
          ErrorCode.CantJoinAtSuchTownhError.Throw();
        }
      }
    }
  }
}
