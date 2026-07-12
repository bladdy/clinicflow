using DentalBot.Domain.Common;

namespace DentalBot.Domain.Entities;

public class Holiday : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool IsRecurring { get; set; }

    public Company Company { get; set; } = null!;
    public Branch? Branch { get; set; }
}
