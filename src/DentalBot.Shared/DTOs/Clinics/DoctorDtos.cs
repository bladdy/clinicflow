namespace DentalBot.Shared.DTOs.Clinics;

public class DoctorDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CompanyId { get; set; }
    public string Specialty { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Color { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
}

public class CreateDoctorRequest
{
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Specialty { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Color { get; set; }
}

public class UpdateDoctorRequest
{
    public string Specialty { get; set; } = string.Empty;
    public string? LicenseNumber { get; set; }
    public string? Bio { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Color { get; set; }
}
