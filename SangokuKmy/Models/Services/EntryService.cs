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
    public static int GetAttributeMax(GameDateTime current) => 100 + (int)((Math.Max(current.Year, Config.UpdateStartYear) - Config.UpdateStartYear) * 0.65f * 0.4f);
    public static int GetAttributeSumMax(GameDateTime current) => 200 + (int)((Math.Max(current.Year, Config.UpdateStartYear) - Config.UpdateStartYear) * 0.65f);

    public static async Task EntryAsync(MainRepository repo, string ipAddress, Character newChara, CharacterIcon newIcon, string password, Country newCountry, string invitationCode)
    {
      var town = await repo.Town.GetByIdAsync(newChara.TownId).GetOrErrorAsync(ErrorCode.TownNotFoundError);

      var system = await repo.System.GetAsync();
      CheckEntryStatus(system.GameDateTime, ipAddress, newChara, password, newIcon, town, newCountry);

      // 文字数チェックしてからエスケープ
      newChara.Name = HtmlUtil.Escape(newChara.Name);
      newCountry.Name = HtmlUtil.Escape(newCountry.Name);
      newChara.Message = HtmlUtil.Escape(newChara.Message);

      // 既存との重複チェック
      if (await repo.Character.IsAlreadyExistsAsync(newChara.Name, newChara.AliasId))
      {
        ErrorCode.DuplicateCharacterNameOrAliasIdError.Throw();
      }
      if ((system.IsDebug && (await repo.System.GetDebugDataAsync()).IsCheckDuplicateEntry || !system.IsDebug)
        && await repo.EntryHost.ExistsAsync(ipAddress))
      {
        ErrorCode.DuplicateEntryError.Throw();
      }

      // 招待コードチェック
      Optional<InvitationCode> invitationCodeOptional = default;
      if (system.InvitationCodeRequestedAtEntry)
      {
        invitationCodeOptional = await repo.InvitationCode.GetByCodeAsync(invitationCode);
        if (!invitationCodeOptional.HasData || invitationCodeOptional.Data.HasUsed || invitationCodeOptional.Data.Aim != InvitationCodeAim.Entry)
        {
          ErrorCode.InvitationCodeRequestedError.Throw();
        }
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
        DeleteTurn = (short)(Config.DeleteTurns - 10),
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

      // 来月の更新がまだ終わってないタイミングで登録したときの、武将更新時刻の調整
      if (chara.LastUpdated - system.CurrentMonthStartDateTime > TimeSpan.FromSeconds(Config.UpdateTime))
      {
        chara.IntLastUpdatedGameDate++;
      }

      if (town.CountryId > 0)
      {
        // 武将総数チェック
        var country = await repo.Country.GetByIdAsync(town.CountryId).GetOrErrorAsync(ErrorCode.CountryNotFoundError);
        if (country.IntEstablished + Config.CountryBattleStopDuring > system.GameDateTime.ToInt())
        {
          var countryCharaCount = await repo.Country.CountCharactersAsync(country.Id, true);
          if (countryCharaCount >= Config.CountryJoinMaxOnLimited)
          {
            ErrorCode.CantJoinAtSuchCountryhError.Throw();
          }
          else if (countryCharaCount + 1 == Config.CountryJoinMaxOnLimited)
          {
            updateCountriesRequested = true;
          }
        }

        // AI国家チェック
        if (country.AiType != CountryAiType.Human)
        {
          ErrorCode.CantJoinAtSuchCountryhError.Throw();
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
          PolicyPoint = 6000,
        };
        updateCountriesRequested = true;
        await repo.Country.AddAsync(country);

        // 大都市に変更
        town.SubType = town.Type;
        MapService.UpdateTownType(town, TownType.Large);

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

        var policies = new List<CountryPolicy>
        {
          new CountryPolicy
          {
            Type = CountryPolicyType.GunKen,
            Status = CountryPolicyStatus.Available,
            CountryId = country.Id,
          },
        };
        if (town.SubType == TownType.Agriculture)
        {
          policies.Add(new CountryPolicy
          {
            Type = CountryPolicyType.AgricultureCountry,
            Status = CountryPolicyStatus.Available,
            CountryId = country.Id,
          });
          policies.Add(new CountryPolicy
          {
            Type = CountryPolicyType.Scouter,
            Status = CountryPolicyStatus.Boosted,
            CountryId = country.Id,
          });
        }
        else if (town.SubType == TownType.Commercial)
        {
          policies.Add(new CountryPolicy
          {
            Type = CountryPolicyType.CommercialCountry,
            Status = CountryPolicyStatus.Available,
            CountryId = country.Id,
          });
          policies.Add(new CountryPolicy
          {
            Type = CountryPolicyType.Storage,
            Status = CountryPolicyStatus.Boosted,
            CountryId = country.Id,
          });
        }
        else if (town.SubType == TownType.Fortress)
        {
          policies.Add(new CountryPolicy
          {
            Type = CountryPolicyType.WallCountry,
            Status = CountryPolicyStatus.Available,
            CountryId = country.Id,
          });
          policies.Add(new CountryPolicy
          {
            Type = CountryPolicyType.AntiGang,
            Status = CountryPolicyStatus.Boosted,
            CountryId = country.Id,
          });
          country.PolicyPoint += 500;
        }
        foreach (var p in policies)
        {
          await repo.Country.AddPolicyAsync(p);
        }

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

      if (invitationCodeOptional.HasData)
      {
        var code = invitationCodeOptional.Data;
        code.HasUsed = true;
        code.Used = DateTime.Now;
        code.CharacterId = chara.Id;
      }

      var icon = new CharacterIcon
      {
        Type = newIcon.Type,
        IsAvailable = true,
        IsMain = true,
        FileName = newIcon.FileName,
        CharacterId = chara.Id,
      };
      await repo.Character.AddCharacterIconAsync(icon);

      var host = new EntryHost
      {
        CharacterId = chara.Id,
        IpAddress = ipAddress,
      };
      await repo.EntryHost.AddAsync(host);

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
      await StatusStreaming.Default.SendCountryAsync(ApiData.From(town), town.CountryId);

      await CharacterService.StreamCharacterAsync(repo, chara);
    }

    private static void CheckEntryStatus(GameDateTime current, string ipAddress, Character chara, string password, CharacterIcon icon, Town town, Country country)
    {
      if (string.IsNullOrEmpty(ipAddress))
      {
        ErrorCode.InvalidIpAddressError.Throw();
      }

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
      if (chara.Name.Contains('_'))
      {
        ErrorCode.NotPermissionError.Throw();
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
