using DentalBot.Domain.Common;
using DentalBot.Domain.Enums;

namespace DentalBot.Domain.Entities;

public class Message : BaseEntity
{
    public Guid ConversationId { get; set; }
    public string Content { get; set; } = string.Empty;
    public MessageDirection Direction { get; set; }
    public SenderType SenderType { get; set; }
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }

    public Conversation Conversation { get; set; } = null!;
}
