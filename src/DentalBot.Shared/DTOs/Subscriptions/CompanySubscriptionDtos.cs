namespace DentalBot.Shared.DTOs.Subscriptions;

public class CompanySubscriptionDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsAnnual { get; set; }
    public int CurrentDoctors { get; set; }
    public int MaxDoctors { get; set; }
    public int CurrentPatients { get; set; }
    public int MaxPatients { get; set; }
    public int CurrentBranches { get; set; }
    public int MaxBranches { get; set; }
    public int AppointmentsThisMonth { get; set; }
    public int MaxAppointmentsPerMonth { get; set; }
    public int ConversationsThisMonth { get; set; }
    public int MaxConversationsPerMonth { get; set; }
}

public class CreateSubscriptionRequest
{
    public Guid CompanyId { get; set; }
    public Guid PlanId { get; set; }
    public bool IsAnnual { get; set; }
}

public class ChangePlanRequest
{
    public Guid NewPlanId { get; set; }
}
