using DentalBot.Shared.DTOs.Auth;

namespace DentalBot.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken);
    Task RevokeTokenAsync(string refreshToken);
}
