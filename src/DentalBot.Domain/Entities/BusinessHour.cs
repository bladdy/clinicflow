using DentalBot.Domain.Common;
using DentalBot.Domain.Enums;

namespace DentalBot.Domain.Entities;

public class BusinessHour : BaseEntity
{
    public Guid BranchId { get; set; }
    public Guid? DoctorId { get; set; }
    public Enums.DayOfWeek DayOfWeek { get; set; }
    public TimeSpan OpenTime { get; set; }
    public TimeSpan CloseTime { get; set; }
    public bool IsClosed { get; set; }

    public Branch Branch { get; set; } = null!;
    public Doctor? Doctor { get; set; }
}
