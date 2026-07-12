using DentalBot.Domain.Common;

namespace DentalBot.Domain.Entities;

public class CompanySubscription : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid PlanId { get; set; }
    public Plan Plan { get; set; } = null!;
    public DentalBot.Domain.Enums.SubscriptionStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsAnnual { get; set; }
    public int CurrentDoctors { get; set; }
    public int CurrentPatients { get; set; }
    public int CurrentBranches { get; set; }
    public int AppointmentsThisMonth { get; set; }
    public int ConversationsThisMonth { get; set; }
}
