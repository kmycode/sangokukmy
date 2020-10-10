using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SangokuKmy.Models.Data.Entities
{
  [Table("historical_chat_messages")]
  public class HistoricalChatMessage
  {
    [Key]
    [Column("id")]
    public uint Id { get; set; }

    [Column("history_id")]
    public uint HistoryId { get; set; }

    [Column("chat_message_id")]
    public uint ChatMessageId { get; set; }

    [Column("character_id")]
    public uint CharacterId { get; set; }

    [Column("type")]
    public ChatMessageType Type { get; set; }

    [Column("type_data")]
    public uint TypeData { get; set; }

    [Column("type_data_2")]
    public uint TypeData2 { get; set; }

    [Column("message")]
    public string Message { get; set; }

    [Column("posted")]
    public DateTime Posted { get; set; }


    public static HistoricalChatMessage FromChatMessage(ChatMessage message)
    {
      return new HistoricalChatMessage
      {
        ChatMessageId = message.Id,
        CharacterId = message.CharacterId,
        Type = message.Type,
        TypeData = message.TypeData,
        TypeData2 = message.TypeData2,
        Message = message.Message,
        Posted = message.Posted,
      };
    }
  }
}
