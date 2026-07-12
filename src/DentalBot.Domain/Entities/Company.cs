using DentalBot.Domain.Common;

namespace DentalBot.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Website { get; set; }
    public string? TaxId { get; set; }

    public ICollection<Branch> Branches { get; set; } = [];
    public ICollection<User> Users { get; set; } = [];
    public ICollection<Doctor> Doctors { get; set; } = [];
    public ICollection<Patient> Patients { get; set; } = [];
    public ICollection<Service> Services { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
    public ICollection<Conversation> Conversations { get; set; } = [];
    public ICollection<KnowledgeArticle> KnowledgeArticles { get; set; } = [];
    public ICollection<Holiday> Holidays { get; set; } = [];
    public AISettings? AISettings { get; set; }
    public ICollection<WhatsAppInstance> WhatsAppInstances { get; set; } = [];
}
