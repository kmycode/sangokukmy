using Microsoft.EntityFrameworkCore;
using SangokuKmy.Common;
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
    /// メッセージIDからメッセージを取得する
    /// </summary>
    /// <returns>メッセージ</returns>
    /// <param name="id">ID</param>
    public async Task<Optional<ChatMessage>> GetByIdAsync(uint id)
    {
      try
      {
        return await this.container.Context.ChatMessages.FindAsync(id).ToOptionalAsync();
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
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
      return await this.GetMessagesAsync(mes => (mes.Type == ChatMessageType.SelfCountry && mes.TypeData == countryId) || (mes.Type == ChatMessageType.OtherCountry && (mes.TypeData == countryId || mes.TypeData2 == countryId)), sinceId, count);
    }

    /// <summary>
    /// 全国宛を取得する
    /// </summary>
    /// <param name="sinceId">最初のID</param>
    /// <param name="count">取得数</param>
    /// <returns>全国宛の一覧</returns>
    public async Task<IReadOnlyCollection<ChatMessage>> GetGlobalMessagesAsync(uint sinceId, int type, int count)
    {
      return await this.GetMessagesAsync(mes => mes.Type == ChatMessageType.Global && mes.TypeData == type, sinceId, count);
    }

    /// <summary>
    /// 個人宛を取得する
    /// </summary>
    /// <param name="sinceId">最初のID</param>
    /// <param name="count">取得数</param>
    /// <returns>個人宛の一覧</returns>
    public async Task<IReadOnlyCollection<ChatMessage>> GetPrivateMessagesAsync(uint characterId, uint sinceId, int count)
    {
      return await this.GetMessagesAsync(mes => mes.Type == ChatMessageType.Private && (mes.TypeData == characterId || mes.TypeData2 == characterId), sinceId, count);
    }

    /// <summary>
    /// 個人宛に既読をつける
    /// </summary>
    /// <param name="characterId">武将ID</param>
    public async Task SetAllPrivateMessagesReadAsync(uint characterId)
    {
      try
      {
        var messages = await this.GetMessagesAsync(mes => mes.Type == ChatMessageType.Private && mes.TypeData2 == characterId && !mes.IsRead, default, 100);
        foreach (var message in messages)
        {
          message.IsRead = true;
        }
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 登用を取得する
    /// </summary>
    /// <param name="sinceId">最初のID</param>
    /// <param name="count">取得数</param>
    /// <returns>個人宛の一覧</returns>
    public async Task<IReadOnlyCollection<ChatMessage>> GetPromotionMessagesAsync(uint characterId, uint sinceId, int count)
    {
      return await this.GetMessagesAsync(mes => (mes.Type == ChatMessageType.Promotion || mes.Type == ChatMessageType.PromotionRefused || mes.Type == ChatMessageType.PromotionAccepted || mes.Type == ChatMessageType.PromotionDenied) && (mes.TypeData == characterId || mes.TypeData2 == characterId), sinceId, count);
    }

    /// <summary>
    /// 登用を取得する
    /// </summary>
    /// <param name="countryId">国ID</param>
    /// <returns>個人宛の一覧</returns>
    public async Task<IReadOnlyCollection<ChatMessage>> GetPromotionMessagesAsync(uint countryId)
    {
      return await this.GetMessagesAsync(mes => (mes.Type == ChatMessageType.Promotion || mes.Type == ChatMessageType.PromotionRefused || mes.Type == ChatMessageType.PromotionAccepted || mes.Type == ChatMessageType.PromotionDenied) && mes.CharacterCountryId == countryId, default, 8191);
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
      if (sinceId == default)
      {
        sinceId = uint.MaxValue;
      }
      if (count == default)
      {
        count = 50;
      }

      try
      {
        var data = await this.container.Context.ChatMessages
          .Where(subject)
          .Where(m => m.Id < sinceId)
          .OrderByDescending(m => m.Id)
          .Take(count)
          .GroupJoin(this.container.Context.Reinforcements.Where(r => r.Status == ReinforcementStatus.Active),
                m => m.CharacterId,
                r => r.CharacterId,
                (m, rs) => new { Message = m, Reinforcements = rs, })
          .Join(this.container.Context.Characters,
                m => m.Message.CharacterId,
                c => c.Id,
                (m, c) => new { m.Message, Character = new CharacterChatData(c, m.Reinforcements.FirstOrDefault()), })
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

        // 受信者情報を付加。受信者名以外は参照渡しなので、後で処理結果にマージする必要はない
        var data2 = data
          .Where(d => d.Message.Type == ChatMessageType.Private || d.Message.Type == ChatMessageType.Promotion || d.Message.Type == ChatMessageType.PromotionAccepted || d.Message.Type == ChatMessageType.PromotionRefused || d.Message.Type == ChatMessageType.PromotionDenied)
          .Join(this.container.Context.Characters, d => d.Message.TypeData2, c => c.Id, (d, c) => new { d.Message, d.Character, d.Icon, ReceiverName = c.Name, })
          .ToArray();
        var data3 = data
          .Where(d => d.Message.Type == ChatMessageType.OtherCountry)
          .Join(this.container.Context.Countries, d => d.Message.TypeData2, c => c.Id, (d, c) => new { d.Message, d.Character, d.Icon, ReceiverName = c.Name, });
        foreach (var d in data2.Concat(data3))
        {
          d.Message.ReceiverName = d.ReceiverName;
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

    public void RemoveMessage(ChatMessage message)
    {
      try
      {
        this.container.Context.ChatMessages
          .Remove(message);
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    public void RemoveByCountryId(uint countryId)
    {
      try
      {
        foreach (var message in this.container.Context.ChatMessages.Where(r => r.Type == ChatMessageType.SelfCountry && r.TypeData == countryId))
        {
          message.TypeData = 0;
          message.TypeData2 = 0;
        }
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }

    /// <summary>
    /// 武将IDから既読を取得する
    /// </summary>
    public async Task<ChatMessageRead> GetReadByCharacterIdAsync(uint charaId)
    {
      try
      {
        var read = await this.container.Context.ChatMessagesRead.FirstOrDefaultAsync(c => c.CharacterId == charaId);
        if (read == null)
        {
          read = new ChatMessageRead
          {
            CharacterId = charaId,
          };
          await this.container.Context.ChatMessagesRead.AddAsync(read);
        }
        return read;
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
        return default;
      }
    }

    /// <summary>
    /// 内容をすべてリセットする
    /// </summary>
    public async Task ResetAsync()
    {
      try
      {
        await this.container.RemoveAllRowsAsync(typeof(ChatMessage));
      }
      catch (Exception ex)
      {
        this.container.Error(ex);
      }
    }
  }
}
