using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using DentalBot.Shared.DTOs.AI;
using DentalBot.Shared.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalBot.Api.Controllers;

[ApiController]
[Route("api/ai-settings")]
[Authorize]
public class AISettingsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AISettingsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<AISettingsDto>>> Get()
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<AISettingsDto>.Fail("CompanyId no encontrado"));

        var settings = (await _unitOfWork.AISettings.FindAsync(
            a => a.CompanyId == companyId.Value)).FirstOrDefault();

        if (settings == null)
        {
            settings = new AISettings
            {
                CompanyId = companyId.Value,
                OllamaUrl = "http://localhost:11434",
                ModelName = "qwen3:4b",
                SystemPrompt = "Eres un asistente virtual de una clínica dental. Ayudas a los pacientes a agendar citas, consultar horarios, servicios y precios. Siempre sé amable y profesional.",
                MaxTokens = 500,
                Temperature = 0.7m,
                IsEnabled = false,
                WelcomeMessage = "¡Hola! Soy el asistente virtual de la clínica. ¿Cómo puedo ayudarte hoy?",
                TransferMessage = "Un representante se comunicará contigo pronto."
            };
            await _unitOfWork.AISettings.AddAsync(settings);
            await _unitOfWork.SaveChangesAsync();
        }

        return Ok(ApiResponse<AISettingsDto>.Ok(MapToDto(settings)));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<AISettingsDto>>> Update([FromBody] UpdateAISettingsRequest request)
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<AISettingsDto>.Fail("CompanyId no encontrado"));

        var settings = (await _unitOfWork.AISettings.FindAsync(
            a => a.CompanyId == companyId.Value)).FirstOrDefault();

        if (settings == null)
        {
            settings = new AISettings
            {
                CompanyId = companyId.Value
            };
            await _unitOfWork.AISettings.AddAsync(settings);
        }

        settings.OllamaUrl = request.OllamaUrl;
        settings.ModelName = request.ModelName;
        settings.SystemPrompt = request.SystemPrompt;
        settings.MaxTokens = request.MaxTokens;
        settings.Temperature = request.Temperature;
        settings.IsEnabled = request.IsEnabled;
        settings.WelcomeMessage = request.WelcomeMessage;
        settings.TransferMessage = request.TransferMessage;
        settings.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.AISettings.Update(settings);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<AISettingsDto>.Ok(MapToDto(settings), "Configuración de IA actualizada"));
    }

    private static AISettingsDto MapToDto(AISettings s) => new()
    {
        Id = s.Id,
        CompanyId = s.CompanyId,
        OllamaUrl = s.OllamaUrl ?? string.Empty,
        ModelName = s.ModelName ?? string.Empty,
        SystemPrompt = s.SystemPrompt ?? string.Empty,
        MaxTokens = s.MaxTokens,
        Temperature = s.Temperature,
        IsEnabled = s.IsEnabled,
        WelcomeMessage = s.WelcomeMessage,
        TransferMessage = s.TransferMessage
    };

    private Guid? GetCompanyId()
    {
        var claim = User.FindFirst("companyId")?.Value;
        return Guid.TryParse(claim, out var companyId) ? companyId : null;
    }
}
