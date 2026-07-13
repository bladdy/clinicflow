using DentalBot.Domain.Common;
using DentalBot.Domain.Enums;

namespace DentalBot.Domain.Entities;

public class ScheduleDay : BaseEntity
{
    public Guid BusinessScheduleId { get; set; }
    public BusinessSchedule BusinessSchedule { get; set; } = null!;

    public Enums.DayOfWeek DayOfWeek { get; set; }
    public bool IsOpen { get; set; }
    public TimeSpan OpenTime { get; set; } = new(9, 0, 0);
    public TimeSpan CloseTime { get; set; } = new(17, 0, 0);
}
