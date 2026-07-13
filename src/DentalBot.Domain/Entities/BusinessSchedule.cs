using DentalBot.Domain.Common;

namespace DentalBot.Domain.Entities;

public class BusinessSchedule : BaseEntity
{
    public Guid BranchId { get; set; }
    public Branch Branch { get; set; } = null!;

    public ICollection<ScheduleDay> Days { get; set; } = [];
    public ICollection<BreakPeriod> Breaks { get; set; } = [];
    public LunchConfig? LunchConfig { get; set; }
}
