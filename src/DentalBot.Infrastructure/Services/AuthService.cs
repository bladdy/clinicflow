using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using DentalBot.Domain.Enums;
using DentalBot.Shared.DTOs.Auth;
using Microsoft.EntityFrameworkCore;

namespace DentalBot.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthService(IUnitOfWork unitOfWork, IJwtTokenService jwtTokenService)
    {
        _unitOfWork = unitOfWork;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = (await _unitOfWork.Users.FindAsync(u => u.Email == request.Email)).FirstOrDefault();

        if (user == null || !VerifyPassword(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Email o contraseña incorrectos");

        if (!user.IsActive)
            throw new UnauthorizedAccessException("La cuenta está desactivada");

        user.Role = (await _unitOfWork.Roles.FindAsync(r => r.Id == user.RoleId)).FirstOrDefault();

        var token = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(60),
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existingUser = (await _unitOfWork.Users.FindAsync(u => u.Email == request.Email)).FirstOrDefault();
        if (existingUser != null)
            throw new InvalidOperationException("Ya existe una cuenta con este email");

        var role = (await _unitOfWork.Roles.FindAsync(r => r.Name == (request.CompanyId.HasValue ? RoleName.Recepcion : RoleName.Administrador))).FirstOrDefault();
        if (role == null)
            throw new InvalidOperationException("Rol no encontrado");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            RoleId = role.Id,
            CompanyId = request.CompanyId,
            IsActive = true
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var token = _jwtTokenService.GenerateAccessToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(60),
            User = MapToUserDto(user)
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken)
    {
        if (!_jwtTokenService.ValidateToken(token))
            throw new UnauthorizedAccessException("Token inválido");

        var userId = _jwtTokenService.GetUserIdFromToken(token);
        if (userId == null)
            throw new UnauthorizedAccessException("Token inválido");

        var user = await _unitOfWork.Users.GetByIdAsync(userId.Value);
        if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token inválido o expirado");

        var newToken = _jwtTokenService.GenerateAccessToken(user);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return new AuthResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
            Expiration = DateTime.UtcNow.AddMinutes(60),
            User = MapToUserDto(user)
        };
    }

    public async Task RevokeTokenAsync(string refreshToken)
    {
        var users = await _unitOfWork.Users.FindAsync(u => u.RefreshToken == refreshToken);
        var user = users.FirstOrDefault();
        if (user == null) return;

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();
    }

    private static string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(bytes);
    }

    private static bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }

    private static UserDto MapToUserDto(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        Phone = user.Phone,
        Role = user.Role?.Name.ToString() ?? "SoloLectura",
        CompanyId = user.CompanyId,
        BranchId = user.BranchId
    };
}
