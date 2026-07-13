using DentalBot.Domain.Common;

namespace DentalBot.Domain.Entities;

public class Doctor : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public string Specialty { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Color { get; set; }

    public User User { get; set; } = null!;
    public Company Company { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = [];
}
