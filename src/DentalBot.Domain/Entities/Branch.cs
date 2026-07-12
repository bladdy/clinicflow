using DentalBot.Domain.Common;

namespace DentalBot.Domain.Entities;

public class Branch : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public bool IsMain { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<User> Users { get; set; } = [];
    public ICollection<Patient> Patients { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<Conversation> Conversations { get; set; } = [];
    public ICollection<BusinessHour> BusinessHours { get; set; } = [];
    public ICollection<Holiday> Holidays { get; set; } = [];
    public ICollection<WhatsAppInstance> WhatsAppInstances { get; set; } = [];
}
