using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using DentalBot.Domain.Enums;
using DentalBot.Shared.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WhatsAppController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IAIService _aiService;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(
        IUnitOfWork unitOfWork,
        IWhatsAppService whatsAppService,
        IAIService aiService,
        ILogger<WhatsAppController> logger)
    {
        _unitOfWork = unitOfWork;
        _whatsAppService = whatsAppService;
        _aiService = aiService;
        _logger = logger;
    }

    [HttpPost("webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook([FromBody] object payload)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(payload);
            var doc = System.Text.Json.JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("event", out var eventProp))
                return Ok();

            var eventType = eventProp.GetString();

            if (eventType == "messages.upsert")
            {
                await HandleIncomingMessage(doc.RootElement);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return Ok();
        }
    }

    [HttpGet("instances")]
    public async Task<ActionResult<ApiResponse<List<WhatsAppInstance>>>> GetInstances()
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<List<WhatsAppInstance>>.Fail("CompanyId no encontrado"));

        var instances = await _unitOfWork.WhatsAppInstances.FindAsync(
            w => w.CompanyId == companyId.Value && !w.IsDeleted);

        return Ok(ApiResponse<List<WhatsAppInstance>>.Ok(instances.ToList()));
    }

    [HttpPost("instances")]
    public async Task<ActionResult<ApiResponse<WhatsAppInstance>>> CreateInstance([FromBody] CreateWhatsAppInstanceRequest request)
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<WhatsAppInstance>.Fail("CompanyId no encontrado"));

        var instance = new WhatsAppInstance
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId.Value,
            BranchId = request.BranchId,
            InstanceName = request.InstanceName,
            ApiUrl = request.ApiUrl,
            ApiKey = request.ApiKey,
            PhoneNumber = request.PhoneNumber,
            IsActive = true
        };

        await _unitOfWork.WhatsAppInstances.AddAsync(instance);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<WhatsAppInstance>.Ok(instance, "Instancia creada exitosamente"));
    }

    [HttpGet("instances/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> GetInstanceStatus(Guid id)
    {
        var instance = await _unitOfWork.WhatsAppInstances.GetByIdAsync(id);
        if (instance == null)
            return NotFound(ApiResponse<object>.Fail("Instancia no encontrada"));

        var isConnected = await _whatsAppService.CheckConnectionAsync(id);
        return Ok(ApiResponse<object>.Ok(new { isConnected, instanceId = id }));
    }

    [HttpPost("instances/{id:guid}/send")]
    public async Task<ActionResult<ApiResponse<object>>> SendMessage(Guid id, [FromBody] SendMessageRequest request)
    {
        var instance = await _unitOfWork.WhatsAppInstances.GetByIdAsync(id);
        if (instance == null)
            return NotFound(ApiResponse<object>.Fail("Instancia no encontrada"));

        await _whatsAppService.SendMessageAsync(id, request.PhoneNumber, request.Message);
        return Ok(ApiResponse<object>.Ok(null!, "Mensaje enviado"));
    }

    private async Task HandleIncomingMessage(System.Text.Json.JsonElement root)
    {
        if (!root.TryGetProperty("data", out var dataProp)) return;

        string? phone = null;
        string? content = null;
        string? instanceId = null;

        if (dataProp.TryGetProperty("key", out var keyProp))
        {
            if (keyProp.TryGetProperty("remoteJid", out var jidProp))
                phone = jidProp.GetString()?.Replace("@s.whatsapp.net", "").Replace("@lid", "");
        }

        if (dataProp.TryGetProperty("message", out var msgProp))
        {
            if (msgProp.TryGetProperty("conversation", out var convProp))
                content = convProp.GetString();
            else if (msgProp.TryGetProperty("extendedTextMessage", out var extProp) &&
                     extProp.TryGetProperty("text", out var textProp))
                content = textProp.GetString();
        }

        if (dataProp.TryGetProperty("instanceId", out var instProp))
            instanceId = instProp.GetString();

        if (string.IsNullOrEmpty(phone) || string.IsNullOrEmpty(content)) return;

        _logger.LogInformation("Incoming message from {Phone}: {Content}", phone, content.Substring(0, Math.Min(content.Length, 100)));

        var instance = !string.IsNullOrEmpty(instanceId) && Guid.TryParse(instanceId, out var instGuid)
            ? await _unitOfWork.WhatsAppInstances.GetByIdAsync(instGuid)
            : (await _unitOfWork.WhatsAppInstances.FindAsync(w => w.PhoneNumber == phone && w.IsActive)).FirstOrDefault();

        if (instance == null) return;

        var conversation = (await _unitOfWork.Conversations.FindAsync(
            c => c.Phone == phone && c.CompanyId == instance.CompanyId && c.Status == ConversationStatus.Open)).FirstOrDefault();

        if (conversation == null)
        {
            conversation = new Conversation
            {
                Id = Guid.NewGuid(),
                CompanyId = instance.CompanyId,
                Phone = phone,
                Status = ConversationStatus.Open,
                StartedAt = DateTime.UtcNow
            };

            var patient = (await _unitOfWork.Patients.FindAsync(
                p => p.CompanyId == instance.CompanyId && p.Phone == phone)).FirstOrDefault();
            if (patient != null)
                conversation.PatientId = patient.Id;

            await _unitOfWork.Conversations.AddAsync(conversation);
        }

        var incomingMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Content = content,
            Direction = MessageDirection.Incoming,
            SenderType = SenderType.Patient,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };
        await _unitOfWork.Messages.AddAsync(incomingMessage);
        await _unitOfWork.SaveChangesAsync();

        var aiSettings = (await _unitOfWork.AISettings.FindAsync(
            a => a.CompanyId == instance.CompanyId)).FirstOrDefault();

        if (aiSettings == null || !aiSettings.IsEnabled)
        {
            var humanMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                Content = aiSettings?.TransferMessage ?? "Un representante se comunicará contigo pronto.",
                Direction = MessageDirection.Outgoing,
                SenderType = SenderType.Human,
                SentAt = DateTime.UtcNow,
                IsRead = true
            };
            await _unitOfWork.Messages.AddAsync(humanMsg);
            await _unitOfWork.SaveChangesAsync();
            await _whatsAppService.SendMessageAsync(instance.Id, phone, humanMsg.Content);
            return;
        }

        var recentMessages = (await _unitOfWork.Messages.FindAsync(
            m => m.ConversationId == conversation.Id))
            .OrderBy(m => m.SentAt)
            .TakeLast(20)
            .ToList();

        var context = string.Join("\n", recentMessages.Select(m =>
            $"{(m.Direction == MessageDirection.Incoming ? "Paciente" : "Bot")}: {m.Content}"));

        var tools = new List<string>
        {
            "BuscarHorarios", "CrearCita", "ReagendarCita", "CancelarCita",
            "BuscarPaciente", "ConsultarServicios", "ConsultarFAQ", "TransferirHumano"
        };

        var isUrgent = await _aiService.IsUrgentAsync(content);
        if (isUrgent)
        {
            var urgentMsg = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = conversation.Id,
                Content = "Detectamos que esto puede ser una urgencia. Un doctor se comunicará contigo de inmediato. Si es una emergencia grave, por favor acude al servicio de urgencias más cercano.",
                Direction = MessageDirection.Outgoing,
                SenderType = SenderType.Bot,
                SentAt = DateTime.UtcNow,
                IsRead = true
            };
            await _unitOfWork.Messages.AddAsync(urgentMsg);
            await _unitOfWork.SaveChangesAsync();
            await _whatsAppService.SendMessageAsync(instance.Id, phone, urgentMsg.Content);
            return;
        }

        var aiResponse = await _aiService.GenerateResponseAsync(content, context, aiSettings.SystemPrompt ?? string.Empty, tools);

        var botMessage = new Message
        {
            Id = Guid.NewGuid(),
            ConversationId = conversation.Id,
            Content = aiResponse,
            Direction = MessageDirection.Outgoing,
            SenderType = SenderType.Bot,
            SentAt = DateTime.UtcNow,
            IsRead = true
        };
        await _unitOfWork.Messages.AddAsync(botMessage);
        await _unitOfWork.SaveChangesAsync();

        await _whatsAppService.SendMessageAsync(instance.Id, phone, aiResponse);
    }

    private Guid? GetCompanyId()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == "companyId");
        if (claim != null && Guid.TryParse(claim.Value, out var companyId))
            return companyId;
        return null;
    }
}

public class CreateWhatsAppInstanceRequest
{
    public Guid? BranchId { get; set; }
    public string InstanceName { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public class SendMessageRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
