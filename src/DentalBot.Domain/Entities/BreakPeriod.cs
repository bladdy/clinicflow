using DentalBot.Domain.Common;

namespace DentalBot.Domain.Entities;

public class BreakPeriod : BaseEntity
{
    public Guid BusinessScheduleId { get; set; }
    public BusinessSchedule BusinessSchedule { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int SortOrder { get; set; }
}
