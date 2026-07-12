using DentalBot.Domain.Entities;

namespace DentalBot.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    bool ValidateToken(string token);
    Guid? GetUserIdFromToken(string token);
}
