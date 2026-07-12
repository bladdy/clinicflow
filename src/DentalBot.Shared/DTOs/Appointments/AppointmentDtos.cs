namespace DentalBot.Shared.DTOs.Appointments;

public class AppointmentDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Reason { get; set; }
    public string? PatientName { get; set; }
    public string? DoctorName { get; set; }
    public string? ServiceName { get; set; }
    public string? BranchName { get; set; }
    public string? DoctorColor { get; set; }
}

public class CreateAppointmentRequest
{
    public Guid? BranchId { get; set; }
    public Guid DoctorId { get; set; }
    public Guid PatientId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? Reason { get; set; }
}

public class UpdateAppointmentRequest
{
    public Guid? BranchId { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? PatientId { get; set; }
    public Guid? ServiceId { get; set; }
    public DateTime? AppointmentDate { get; set; }
    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
    public string? Reason { get; set; }
}

public class AvailableSlotDto
{
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}