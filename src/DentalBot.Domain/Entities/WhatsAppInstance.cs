using DentalBot.Domain.Common;

namespace DentalBot.Domain.Entities;

public class WhatsAppInstance : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string InstanceName { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public string? WebhookUrl { get; set; }

    public Company Company { get; set; } = null!;
    public Branch? Branch { get; set; }
    public ICollection<Conversation> Conversations { get; set; } = [];
}
