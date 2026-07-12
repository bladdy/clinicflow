using DentalBot.Domain.Common;

namespace DentalBot.Domain.Entities;

public class Plan : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MonthlyPrice { get; set; }
    public decimal AnnualPrice { get; set; }
    public int MaxBranches { get; set; }
    public int MaxDoctors { get; set; }
    public int MaxPatients { get; set; }
    public int MaxAppointmentsPerMonth { get; set; }
    public int MaxConversationsPerMonth { get; set; }
    public bool HasAI { get; set; }
    public bool HasWhatsAppIntegration { get; set; }
    public bool HasAdvancedReports { get; set; }
    public bool HasPrioritySupport { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }

    public ICollection<CompanySubscription> Subscriptions { get; set; } = [];
}
