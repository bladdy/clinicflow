using DentalBot.Domain.Common;

namespace DentalBot.Domain.Entities;

public class Service : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public string? Category { get; set; }
    public bool IsActive { get; set; }

    public Company Company { get; set; } = null!;
    public ICollection<Appointment> Appointments { get; set; } = [];
}
