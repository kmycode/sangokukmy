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
        ErrorCode.DatabaseError.Throw(ex);
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
            .Join(this.container.Context.Characters,
              cp => cp.CharacterId,
              c => c.Id,
              (cp, c) => new { Post = cp, Character = c, }),
            c => c.Id,
            cpds => cpds.Character.CountryId,
            (c, cps) => new { Country = c, PostData = cps, })
          .ToArrayAsync())
          .Select(data =>
          {
            return new CountryForAnonymous(data.Country)
            {
              Posts = data.PostData.Select(pd =>
              {
                pd.Post.Character = new CharacterForAnonymous(pd.Character, null, CharacterShareLevel.Anonymous);
                return pd.Post;
              })
              .OrderBy(p => p.ApiType),
            };
          })
          .ToArray();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
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
        ErrorCode.DatabaseError.Throw(ex);
        return Optional<Country>.Null();
      }
    }

    /// <summary>
    /// 国IDから武将を取得
    /// </summary>
    /// <param name="townId">都市ID</param>
    /// <returns>その都市に滞在する武将</returns>
    public async Task<IReadOnlyCollection<(Character Character, CharacterIcon Icon)>> GetCharactersAsync(uint countryId)
    {
      try
      {
        return (await this.container.Context.Characters
          .Where(c => c.CountryId == countryId)
          .GroupJoin(this.container.Context.CharacterIcons,
            c => c.Id,
            i => i.CharacterId,
            (c, i) => new { Character = c, Icons = i, })
          .ToArrayAsync())
          .OrderBy(data => data.Character.LastUpdated)
          .Select(data =>
          {
            return (data.Character, data.Icons.GetMainOrFirst().Data);
          })
          .ToArray();
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
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
        ErrorCode.DatabaseError.Throw(ex);
        return default;
      }
    }

    /// <summary>
    /// 国の役職を設定する
    /// </summary>
    /// <param name="post">役職データ</param>
    public async Task SetPostAsync(CountryPost post)
    {
      try
      {
        var old = await this.container.Context.CountryPosts
          .FirstOrDefaultAsync(p => p.CountryId == post.CountryId && p.Type == post.Type);
        if (old != null)
        {
          this.container.Context.CountryPosts.Remove(old);
        }

        await this.container.Context.CountryPosts.AddAsync(post);
      }
      catch (Exception ex)
      {
        ErrorCode.DatabaseError.Throw(ex);
      }
    }
  }
}
