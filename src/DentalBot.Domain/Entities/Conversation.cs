using DentalBot.Domain.Common;
using DentalBot.Domain.Enums;

namespace DentalBot.Domain.Entities;

public class Conversation : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid? PatientId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public Guid? BranchId { get; set; }
    public Guid? WhatsAppInstanceId { get; set; }
    public ConversationStatus Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? BookingState { get; set; }

    public Company Company { get; set; } = null!;
    public Patient? Patient { get; set; }
    public Branch? Branch { get; set; }
    public WhatsAppInstance? WhatsAppInstance { get; set; }
    public ICollection<Message> Messages { get; set; } = [];
}
