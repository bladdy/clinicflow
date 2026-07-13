namespace DentalBot.Domain.Entities;

public class BookingStateData
{
    public string Phase { get; set; } = "idle";
    public string? PatientFirstName { get; set; }
    public string? PatientLastName { get; set; }
    public string? PatientPhone { get; set; }
    public string? PatientEmail { get; set; }
    public Guid? SelectedServiceId { get; set; }
    public string? SelectedServiceName { get; set; }
    public int? SelectedServiceDuration { get; set; }
    public DateTime? SelectedDate { get; set; }
    public Guid? SelectedDoctorId { get; set; }
    public string? SelectedDoctorName { get; set; }
    public string? SelectedTime { get; set; }
    public Guid? ExistingPatientId { get; set; }
    public Guid? EditingAppointmentId { get; set; }
    public string? ModifyTarget { get; set; }
}
