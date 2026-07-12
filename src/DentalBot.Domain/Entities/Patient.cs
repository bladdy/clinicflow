using DentalBot.Domain.Common;
using DentalBot.Domain.Enums;

namespace DentalBot.Domain.Entities;

public class Patient : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public string? MedicalHistory { get; set; }

    public Company Company { get; set; } = null!;
    public Branch? Branch { get; set; }
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<Conversation> Conversations { get; set; } = [];
}
