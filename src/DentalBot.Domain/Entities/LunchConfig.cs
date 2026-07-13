using DentalBot.Domain.Common;

namespace DentalBot.Domain.Entities;

public class LunchConfig : BaseEntity
{
    public Guid BusinessScheduleId { get; set; }
    public BusinessSchedule BusinessSchedule { get; set; } = null!;

    public int DurationMinutes { get; set; } = 45;
    public TimeSpan StartTime { get; set; } = new(13, 0, 0);

    public TimeSpan EndTime => StartTime.Add(TimeSpan.FromMinutes(DurationMinutes));
}
