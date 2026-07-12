namespace DentalBot.Shared.DTOs.Subscriptions;

public class PlanDto
{
    public Guid Id { get; set; }
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
    public bool IsActive { get; set; }
}

public class CreatePlanRequest
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
}

public class UpdatePlanRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? MonthlyPrice { get; set; }
    public decimal? AnnualPrice { get; set; }
    public int? MaxBranches { get; set; }
    public int? MaxDoctors { get; set; }
    public int? MaxPatients { get; set; }
    public int? MaxAppointmentsPerMonth { get; set; }
    public int? MaxConversationsPerMonth { get; set; }
    public bool? HasAI { get; set; }
    public bool? HasWhatsAppIntegration { get; set; }
    public bool? HasAdvancedReports { get; set; }
    public bool? HasPrioritySupport { get; set; }
}
