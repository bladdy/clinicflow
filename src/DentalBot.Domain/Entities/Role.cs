using DentalBot.Domain.Common;
using DentalBot.Domain.Enums;

namespace DentalBot.Domain.Entities;

public class Role : BaseEntity
{
    public RoleName Name { get; set; }
    public string? Description { get; set; }

    public ICollection<User> Users { get; set; } = [];
}
