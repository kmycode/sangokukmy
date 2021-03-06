﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SangokuKmy.Common;
using SangokuKmy.Filters;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Streamings;

namespace SangokuKmy.Controllers
{
  [Route("api/v1")]
  [ServiceFilter(typeof(SangokuKmyErrorFilterAttribute))]
  [AuthenticationFilter]
  public class ThreadBbsItemController : Controller, IAuthenticationDataReceiver
  {
    public AuthenticationData AuthData { private get; set; }

    [AuthenticationFilter]
    [HttpPost("bbs/country")]
    public async Task PostCountryBbsAsync(
      [FromBody] ThreadBbsItem param)
    {
      ThreadBbsItem message;
      Character chara;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);

        if (await repo.BlockAction.IsBlockedAsync(chara.Id, BlockActionType.StopBbs))
        {
          ErrorCode.BlockedActionError.Throw();
        }

        var iconId = param.CharacterIconId;
        if (iconId == 0)
        {
          iconId = (await repo.Character.GetCharacterAllIconsAsync(chara.Id)).GetMainOrFirst().Data?.Id ?? 0;
        }
        if (param.ParentId != 0)
        {
          var parent = await repo.ThreadBbs.GetByIdAsync(param.ParentId).GetOrErrorAsync(ErrorCode.ParentNodeNotFoundError);
          if (parent.CountryId != chara.CountryId)
          {
            ErrorCode.NotPermissionError.Throw();
          }
          if (parent.ParentId != 0)
          {
            ErrorCode.NotTopNodeError.Throw();
          }
          if (!string.IsNullOrEmpty(param.Title))
          {
            ErrorCode.LackOfParameterError.Throw();
          }
        }
        else
        {
          if (string.IsNullOrEmpty(param.Title))
          {
            ErrorCode.InvalidParameterError.Throw();
          }
        }
        message = new ThreadBbsItem
        {
          CharacterId = chara.Id,
          CountryId = chara.CountryId,
          ParentId = param.ParentId,
          Title = param.Title,
          Text = param.Text,
          CharacterIconId = iconId,
          Written = DateTime.Now,
          Type = BbsType.CountryBbs,
        };
        await repo.ThreadBbs.AddAsync(message);

        await repo.SaveChangesAsync();

        var icon = await repo.Character.GetCharacterIconByIdAsync(iconId);
        message.Character = new CharacterForAnonymous(chara, icon, CharacterShareLevel.Anonymous);
      }

      await StatusStreaming.Default.SendCountryAsync(ApiData.From(message), chara.CountryId);
    }

    [AuthenticationFilter]
    [HttpGet("bbs/country")]
    public async Task<IEnumerable<ThreadBbsItem>> GetCountryBbsAsync()
    {
      using (var repo = MainRepository.WithRead())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        return await repo.ThreadBbs.GetCountryBbsByCountryIdAsync(chara.CountryId);
      }
    }

    [AuthenticationFilter]
    [HttpDelete("bbs/country/{id}")]
    public async Task DeleteCountryBbsAsync(
      [FromRoute] uint id)
    {
      ThreadBbsItem message;
      Character chara;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var item = await repo.ThreadBbs.GetByIdAsync(id).GetOrErrorAsync(ErrorCode.NodeNotFoundError);
        if (item.CharacterId != chara.Id)
        {
          var posts = await repo.Country.GetCharacterPostsAsync(chara.Id);
          var characterPosts = posts.Select(p => p.Type);
          if (!characterPosts.Any(p => p == CountryPostType.Monarch || p == CountryPostType.Warrior))
          {
            ErrorCode.NotPermissionError.Throw();
          }
        }

        message = item;
        repo.ThreadBbs.Remove(item);
        await repo.SaveChangesAsync();
      }

      message.IsRemove = true;
      await StatusStreaming.Default.SendCountryAsync(ApiData.From(message), chara.CountryId);
    }

    [AuthenticationFilter]
    [HttpPut("bbs/country/read/{id}")]
    public async Task SetCountryBbsReadAsync([FromRoute] uint id = 0)
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var read = await repo.ChatMessage.GetReadByCharacterIdAsync(this.AuthData.CharacterId);
        read.LastCountryBbsId = id;
        await repo.SaveChangesAsync();
      }
    }

    [AuthenticationFilter]
    [HttpPost("bbs/global")]
    public async Task PostGlobalBbsAsync(
      [FromBody] ThreadBbsItem param)
    {
      ThreadBbsItem message;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);

        if (await repo.BlockAction.IsBlockedAsync(chara.Id, BlockActionType.StopBbs))
        {
          ErrorCode.BlockedActionError.Throw();
        }

        var iconId = param.CharacterIconId;
        if (iconId == 0)
        {
          iconId = (await repo.Character.GetCharacterAllIconsAsync(chara.Id)).GetMainOrFirst().Data?.Id ?? 0;
        }
        if (param.ParentId != 0)
        {
          var parent = await repo.ThreadBbs.GetByIdAsync(param.ParentId).GetOrErrorAsync(ErrorCode.ParentNodeNotFoundError);
          if (parent.ParentId != 0)
          {
            ErrorCode.NotTopNodeError.Throw();
          }
          if (!string.IsNullOrEmpty(param.Title))
          {
            ErrorCode.LackOfParameterError.Throw();
          }
        }
        else
        {
          if (string.IsNullOrEmpty(param.Title))
          {
            ErrorCode.InvalidParameterError.Throw();
          }
        }
        message = new ThreadBbsItem
        {
          CharacterId = chara.Id,
          CountryId = chara.CountryId,
          ParentId = param.ParentId,
          Title = param.Title,
          Text = param.Text,
          CharacterIconId = iconId,
          Written = DateTime.Now,
          Type = BbsType.GlobalBbs,
        };
        await repo.ThreadBbs.AddAsync(message);

        await repo.SaveChangesAsync();

        var icon = await repo.Character.GetCharacterIconByIdAsync(iconId);
        message.Character = new CharacterForAnonymous(chara, icon, CharacterShareLevel.Anonymous);
      }

      await StatusStreaming.Default.SendAllAsync(ApiData.From(message));
    }

    [AuthenticationFilter]
    [HttpDelete("bbs/global/{id}")]
    public async Task DeleteGlobalBbsAsync(
      [FromRoute] uint id)
    {
      ThreadBbsItem message;

      using (var repo = MainRepository.WithReadAndWrite())
      {
        var chara = await repo.Character.GetByIdAsync(this.AuthData.CharacterId).GetOrErrorAsync(ErrorCode.LoginCharacterNotFoundError);
        var item = await repo.ThreadBbs.GetByIdAsync(id).GetOrErrorAsync(ErrorCode.NodeNotFoundError);
        if (item.CharacterId != chara.Id)
        {
          ErrorCode.NotPermissionError.Throw();
        }

        message = item;
        repo.ThreadBbs.Remove(item);
        await repo.SaveChangesAsync();
      }

      message.IsRemove = true;
      await StatusStreaming.Default.SendAllAsync(ApiData.From(message));
    }

    [AuthenticationFilter]
    [HttpPut("bbs/global/read/{id}")]
    public async Task SetGlobalBbsReadAsync([FromRoute] uint id = 0)
    {
      using (var repo = MainRepository.WithReadAndWrite())
      {
        var read = await repo.ChatMessage.GetReadByCharacterIdAsync(this.AuthData.CharacterId);
        read.LastGlobalBbsId = id;
        await repo.SaveChangesAsync();
      }
    }
  }
}
