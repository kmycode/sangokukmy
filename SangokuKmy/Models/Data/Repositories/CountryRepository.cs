﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SangokuKmy.Common;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data.Entities;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Common;

namespace SangokuKmy.Models.Data.Repositories
{
  public class CountryRepository
  {
    private readonly IRepositoryContainer container;

    public CountryRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// すべての国を取得する
    /// </summary>
    /// <returns>すべての国</returns>
    public async Task<IReadOnlyList<Country>> GetAllAsync()
    {
      try
      {
        return await this.container.Context.Countries.ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// すべての国を取得する
    /// </summary>
    /// <returns>すべての国</returns>
    public async Task<IReadOnlyList<Country>> GetAllAliveAsync()
    {
      try
      {
        return await this.container.Context.Countries.Where(c => !c.HasOverthrown).ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// すべての国を誰でも見れる情報に絞って取得する
    /// </summary>
    /// <returns>すべての国データ</returns>
    public async Task<IReadOnlyList<CountryForAnonymous>> GetAllForAnonymousAsync()
    {
      return await this.GetAllForAnonymousAsync(c => true);
    }

    /// <summary>
    /// 指定したIDの国の情報を、誰でも見れる情報に絞って取得する（役職情報もこれに含まれる）
    /// </summary>
    /// <returns>すべての国データ</returns>
    /// <param name="countryId">国ID</param>
    public async Task<Optional<CountryForAnonymous>> GetByIdForAnonymousAsync(uint countryId)
    {
      return (await this.GetAllForAnonymousAsync(c => c.Id == countryId)).FirstOrDefault().ToOptional();
    }

    private async Task<IReadOnlyList<CountryForAnonymous>> GetAllForAnonymousAsync(Expression<Func<Country, bool>> subject)
    {
      try
      {
        return (await this.container.Context.Countries
          .Where(subject)
          .GroupJoin(this.container.Context.CountryPosts
            .Join(this.container.Context.Characters
              .GroupJoin(this.container.Context.CharacterIcons,
                cc => cc.Id,
                ci => ci.CharacterId,
                (cc, cis) => new { Character = cc, Icon = cis.GetMainOrFirst().Data, }),
              cp => cp.CharacterId,
              c => c.Character.Id,
              (cp, c) => new { Post = cp, c.Character, c.Icon, }),
            c => c.Id,
            cpds => cpds.Post.CountryId,
            (c, cps) => new { Country = c, PostData = cps, })
          .ToArrayAsync())
          .Select(data =>
          {
            return new CountryForAnonymous(data.Country)
            {
              Posts = data.PostData.Select(pd =>
              {
                pd.Post.Character = new CharacterForAnonymous(pd.Character, pd.Icon, CharacterShareLevel.Anonymous);
                return pd.Post;
              })
              .OrderBy(p => p.ApiType),
            };
          })
          .ToArray();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// IDから国を取得する
    /// </summary>
    /// <returns>国</returns>
    /// <param name="id">ID</param>
    public async Task<Optional<Country>> GetByIdAsync(uint id)
    {
      try
      {
        return await this.container.Context.Countries
          .FindAsync(id)
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return Optional<Country>.Null();
      }
    }

    /// <summary>
    /// IDから生存中の国を取得する
    /// </summary>
    /// <returns>国</returns>
    /// <param name="id">ID</param>
    public async Task<Optional<Country>> GetAliveByIdAsync(uint id)
    {
      try
      {
        return await this.container.Context.Countries
          .FirstOrDefaultAsync(c => c.Id == id && !c.HasOverthrown)
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return Optional<Country>.Null();
      }
    }

    /// <summary>
    /// 国を追加する
    /// </summary>
    /// <param name="country">国</param>
    public async Task AddAsync(Country country)
    {
      try
      {
        await this.container.Context.Countries
          .AddAsync(country);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 国IDから武将を取得
    /// </summary>
    /// <param name="townId">都市ID</param>
    /// <returns>その都市に滞在する武将</returns>
    public async Task<IReadOnlyCollection<Character>> GetCharactersAsync(uint countryId)
    {
      try
      {
        return await this.container.Context.Characters
          .Where(c => c.CountryId == countryId && !c.HasRemoved)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 国IDから武将を取得
    /// </summary>
    /// <param name="townId">都市ID</param>
    /// <returns>その都市に滞在する武将</returns>
    public async Task<IReadOnlyCollection<(Character Character, CharacterIcon Icon, IReadOnlyList<CharacterCommand> Commands)>> GetCharactersWithIconsAndCommandsAsync(uint countryId)
    {
      try
      {
        var system = await this.container.Context.SystemData.FirstAsync();
        var intStartMonth = system.GameDateTime.Year >= Config.UpdateStartYear ? system.IntGameDateTime : new GameDateTime { Year = Config.UpdateStartYear, Month = 1, }.ToInt();
        return (await this.container.Context.Characters
          .Where(c => c.CountryId == countryId && !c.HasRemoved)
          .GroupJoin(this.container.Context.CharacterIcons,
            c => c.Id,
            i => i.CharacterId,
            (c, i) => new { Character = c, Icons = i, })
          .GroupJoin(this.container.Context.CharacterCommands.Where(c => c.IntGameDateTime <= intStartMonth + 8)
              .GroupJoin(this.container.Context.CharacterCommandParameters,
                c => c.Id,
                p => p.CharacterCommandId,
                (c, ps) => new { Command = c, Parameters = ps, }),
            c => c.Character.Id,
            c => c.Command.CharacterId,
            (c, cs) => new { c.Character, c.Icons, Commands = cs.ToArray(), })
          .ToArrayAsync())
          .OrderBy(data => data.Character.LastUpdated)
          .Select(data =>
          {
            return (data.Character, data.Icons.GetMainOrFirst().Data, (IReadOnlyList<CharacterCommand>)data.Commands.Select(c =>
            {
              c.Command.SetParameters(c.Parameters);
              return c.Command;
            }).ToArray());
          })
          .ToArray();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 国IDから武将数を数える
    /// </summary>
    /// <param name="townId">都市ID</param>
    /// <returns>その都市に滞在する武将の数</returns>
    public async Task<int> CountCharactersAsync(uint countryId, bool isHumanOnly = false, int deleteTurn = -1)
    {
      if (deleteTurn < 0)
      {
        deleteTurn = Config.DeleteTurns;
      }

      try
      {
        return await this.container.Context.Characters
          .CountAsync(c => c.CountryId == countryId && c.DeleteTurn <= deleteTurn && !c.HasRemoved && (!isHumanOnly || (c.AiType == CharacterAiType.Human || c.AiType == CharacterAiType.Administrator)));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 国の役職一覧を取得する。役職の中に武将データなど埋め込みたい場合は、GetByIdForAnonymousAsyncの利用も検討
    /// </summary>
    /// <returns>役職一覧</returns>
    /// <param name="countryId">国ID</param>
    public async Task<IReadOnlyList<CountryPost>> GetPostsAsync(uint countryId)
    {
      try
      {
        return await this.container.Context.CountryPosts
          .Where(p => p.CountryId == countryId)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task<IReadOnlyList<CountryPost>> GetCharacterPostsAsync(uint charaId)
    {
      try
      {
        return await this.container.Context.CountryPosts.Where(p => p.CharacterId == charaId).ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 国の役職を設定する
    /// </summary>
    /// <param name="post">役職データ</param>
    public async Task AddPostAsync(CountryPost post)
    {
      try
      {
        await this.container.Context.CountryPosts.AddAsync(post);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 武将の役職を削除する
    /// </summary>
    /// <param name="characterId">武将ID</param>
    public void RemoveCharacterPosts(uint characterId)
    {
      try
      {
        this.container.Context.CountryPosts.RemoveRange(
          this.container.Context.CountryPosts.Where(p => p.CharacterId == characterId));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public void RemoveCharacterPost(CountryPost post)
    {
      try
      {
        this.container.Context.CountryPosts.RemoveRange(
          this.container.Context.CountryPosts.Where(p => p.Id == post.Id));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 国のメッセージデータを取得する
    /// </summary>
    /// <param name="countryId">国ID</param>
    /// <param name="type">メッセージの種類</param>
    public async Task<Optional<CountryMessage>> GetMessageAsync(uint countryId, CountryMessageType type)
    {
      try
      {
        return await this.container.Context.CountryMessages
          .FirstOrDefaultAsync(m => m.CountryId == countryId && m.Type == type)
          .ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task<IReadOnlyList<CountryMessage>> GetMessagesAsync(CountryMessageType type)
    {
      try
      {
        return await this.container.Context.CountryMessages
          .Where(m => m.Type == type)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 国のメッセージデータを取得する
    /// </summary>
    /// <param name="countryId">国ID</param>
    public async Task<IReadOnlyList<CountryMessage>> GetMessagesAsync(uint countryId)
    {
      try
      {
        return (await this.container.Context.CountryMessages
          .Where(m => m.CountryId == countryId)
          .Join(this.container.Context.Characters, m => m.WriterCharacterId, c => c.Id, (m, c) => new { Message = m, CharacterName = c.Name, })
          .Join(this.container.Context.CharacterIcons, m => m.Message.WriterIconId, i => i.Id, (m, i) => new { m.Message, m.CharacterName, Icon = i, })
          .ToArrayAsync())
          .Select(m =>
          {
            m.Message.WriterCharacterName = m.CharacterName;
            m.Message.WriterIcon = m.Icon;
            return m.Message;
          })
          .ToArray();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// すべての国のメッセージデータを取得する
    /// </summary>
    public async Task<IReadOnlyList<CountryMessage>> GetAllMessagesByTypeAsync(CountryMessageType type)
    {
      try
      {
        return (await this.container.Context.CountryMessages
          .Where(m => m.Type == type)
          .Join(this.container.Context.Characters, m => m.WriterCharacterId, c => c.Id, (m, c) => new { Message = m, CharacterName = c.Name, })
          .Join(this.container.Context.CharacterIcons, m => m.Message.WriterIconId, i => i.Id, (m, i) => new { m.Message, m.CharacterName, Icon = i, })
          .ToArrayAsync())
          .Select(m =>
          {
            m.Message.WriterCharacterName = m.CharacterName;
            m.Message.WriterIcon = m.Icon;
            return m.Message;
          })
          .ToArray();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 国のメッセージデータを設定する
    /// </summary>
    /// <param name="message">メッセージ</param>
    public async Task SetMessageAsync(CountryMessage message)
    {
      try
      {
        var olds = this.container.Context.CountryMessages
          .Where(m => m.CountryId == message.CountryId && m.Type == message.Type);
        this.container.Context.CountryMessages.RemoveRange(olds);

        await this.container.Context.CountryMessages.AddAsync(message);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public async Task<IReadOnlyList<CountryCommander>> GetCommandersAsync(uint countryId)
    {
      try
      {
        return await this.container.Context.CountryCommanders
          .Where(m => m.CountryId == countryId)
          .ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    public async Task AddCommanderAsync(CountryCommander commander)
    {
      try
      {
        await this.container.Context.CountryCommanders.AddAsync(commander);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public void RemoveCommander(CountryCommander commander)
    {
      try
      {
        this.container.Context.CountryCommanders.Remove(commander);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 政策を追加する
    /// </summary>
    /// <param name="policy">政策</param>
    public async Task AddPolicyAsync(CountryPolicy policy)
    {
      try
      {
        await this.container.Context.CountryPolicies.AddAsync(policy);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 政策を削除する
    /// </summary>
    /// <param name="policy">政策</param>
    public void RemovePolicy(CountryPolicy policy)
    {
      try
      {
        this.container.Context.CountryPolicies.Remove(policy);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 政策を取得する
    /// </summary>
    public async Task<IReadOnlyList<CountryPolicy>> GetPoliciesAsync()
    {
      try
      {
        return await this.container.Context.CountryPolicies.ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 政策を取得する
    /// </summary>
    /// <param name="countryId">国ID</param>
    public async Task<IReadOnlyList<CountryPolicy>> GetPoliciesAsync(uint countryId)
    {
      try
      {
        return await this.container.Context.CountryPolicies.Where(p => p.CountryId == countryId).ToArrayAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 国のデータを削除する
    /// </summary>
    public void RemoveDataByCountryId(uint countryId)
    {
      try
      {
        this.container.Context.CountryPosts.RemoveRange(
          this.container.Context.CountryPosts
            .Where(p => p.CountryId == countryId));
        this.container.Context.CountryMessages.RemoveRange(
          this.container.Context.CountryMessages
            .Where(cm => cm.CountryId == countryId));
        this.container.Context.CountryCommanders.RemoveRange(
          this.container.Context.CountryCommanders
            .Where(cc => cc.CountryId == countryId));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 内容をすべてリセットする
    /// </summary>
    public async Task ResetAsync()
    {
      try
      {
        await this.container.RemoveAllRowsAsync(typeof(Country));
        await this.container.RemoveAllRowsAsync(typeof(CountryPost));
        await this.container.RemoveAllRowsAsync(typeof(CountryMessage));
        await this.container.RemoveAllRowsAsync(typeof(CountryPolicy));
        await this.container.RemoveAllRowsAsync(typeof(CountryCommander));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
