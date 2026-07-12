using DentalBot.Domain.Common;

namespace DentalBot.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid RoleId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? CompanyId { get; set; }
    public bool IsActive { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    public Role Role { get; set; } = null!;
    public Branch? Branch { get; set; }
    public Company? Company { get; set; }
    public Doctor? Doctor { get; set; }
}
