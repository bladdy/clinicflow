using DentalBot.Application.Interfaces;
using DentalBot.Shared.DTOs.Auth;
using DentalBot.Shared.DTOs.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace DentalBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RegisterRequest> _registerValidator;

    public AuthController(IAuthService authService, IValidator<LoginRequest> loginValidator, IValidator<RegisterRequest> registerValidator)
    {
        _authService = authService;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var validation = await _loginValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<AuthResponse>.Fail("Validación fallida", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var result = await _authService.LoginAsync(request);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Inicio de sesión exitoso"));
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        var validation = await _registerValidator.ValidateAsync(request);
        if (!validation.IsValid)
            return BadRequest(ApiResponse<AuthResponse>.Fail("Validación fallida", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var result = await _authService.RegisterAsync(request);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Registro exitoso"));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await _authService.RefreshTokenAsync(request.Token, request.RefreshToken);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Token renovado"));
    }

    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RefreshTokenRequest request)
    {
        await _authService.RevokeTokenAsync(request.RefreshToken);
        return Ok(ApiResponse<object>.Ok(null!, "Token revocado"));
    }
}

public class RefreshTokenRequest
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}