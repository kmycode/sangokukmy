using Microsoft.EntityFrameworkCore;
using SangokuKmy.Models.Common.Definitions;
using SangokuKmy.Models.Data.ApiEntities;
using SangokuKmy.Models.Data.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace SangokuKmy.Models.Data.Repositories
{
  public class ChatMessageRepository
  {
    private readonly IRepositoryContainer container;

    public ChatMessageRepository(IRepositoryContainer container)
    {
      this.container = container;
    }

    /// <summary>
    /// 国宛を取得する
    /// </summary>
    /// <param name="countryId">国ID</param>
    /// <param name="sinceId">最初のID</param>
    /// <param name="count">取得数</param>
    /// <returns>国宛の一覧</returns>
    public async Task<IReadOnlyCollection<ChatMessage>> GetCountryMessagesAsync(uint countryId, uint sinceId, int count)
    {
      return await this.GetMessagesAsync(mes => mes.Type == ChatMessageType.SelfCountry && mes.TypeData == countryId, sinceId, count);
    }

    /// <summary>
    /// 全国宛を取得する
    /// </summary>
    /// <param name="sinceId">最初のID</param>
    /// <param name="count">取得数</param>
    /// <returns>全国宛の一覧</returns>
    public async Task<IReadOnlyCollection<ChatMessage>> GetGlobalMessagesAsync(uint sinceId, int count)
    {
      return await this.GetMessagesAsync(mes => mes.Type == ChatMessageType.Global, sinceId, count);
    }

    /// <summary>
    /// 手紙を取得する
    /// </summary>
    /// <param name="subject">取得条件</param>
    /// <param name="sinceId">最初のID</param>
    /// <param name="count">取得数</param>
    /// <returns>手紙の一覧</returns>
    private async Task<IReadOnlyCollection<ChatMessage>> GetMessagesAsync(Expression<Func<ChatMessage, bool>> subject, uint sinceId, int count)
    {
      try
      {
        var data = await this.container.Context.ChatMessages
          .Where(subject)
          .Where(m => m.Id <= sinceId)
          .OrderBy(m => m.Id)
          .Take(count)
          .Join(this.container.Context.Characters,
                m => m.CharacterId,
                c => c.Id,
                (m, c) => new { Message = m, Character = new CharacterChatData(c), })
          .Join(this.container.Context.CharacterIcons,
                m => m.Message.CharacterIconId,
                i => i.Id,
                (m, i) => new { m.Message, m.Character, Icon = i, })
          .ToArrayAsync();
        foreach (var d in data)
        {
          d.Message.CharacterIcon = d.Icon;
          d.Message.Character = d.Character;
        }
        return data.Select(d => d.Message).OrderBy(m => m.Id).ToArray();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// メッセージを追加する
    /// </summary>
    /// <param name="message">メッセージ</param>
    public async Task AddMessageAsync(ChatMessage message)
    {
      try
      {
        await this.container.Context.ChatMessages
          .AddAsync(message);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
