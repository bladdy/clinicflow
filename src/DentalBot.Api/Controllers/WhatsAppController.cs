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
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppController> _logger;

    public WhatsAppController(
        IUnitOfWork unitOfWork,
        IWhatsAppService whatsAppService,
        IAIService aiService,
        IConfiguration configuration,
        ILogger<WhatsAppController> logger)
    {
        _unitOfWork = unitOfWork;
        _whatsAppService = whatsAppService;
        _aiService = aiService;
        _configuration = configuration;
        _logger = logger;
    }

    #region Webhook

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
            _logger.LogInformation("Webhook event received: {EventType}", eventType);

            switch (eventType)
            {
                case "MESSAGES_UPSERT":
                    await HandleIncomingMessage(doc.RootElement);
                    break;
                case "CONNECTION_UPDATE":
                    await HandleConnectionUpdate(doc.RootElement);
                    break;
                case "QRCODE_UPDATED":
                    _logger.LogInformation("QR code updated event received");
                    break;
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook");
            return Ok();
        }
    }

    #endregion

    #region Instances CRUD

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

    [HttpGet("instances/{id:guid}")]
    public async Task<ActionResult<ApiResponse<WhatsAppInstanceDetailDto>>> GetInstance(Guid id)
    {
        var instance = await _unitOfWork.WhatsAppInstances.GetByIdAsync(id);
        if (instance == null)
            return NotFound(ApiResponse<WhatsAppInstanceDetailDto>.Fail("Instancia no encontrada"));

        var connectionState = await _whatsAppService.GetConnectionStateAsync(id);
        var webhookInfo = await _whatsAppService.GetWebhookInfoAsync(id);

        var dto = new WhatsAppInstanceDetailDto
        {
            Id = instance.Id,
            CompanyId = instance.CompanyId,
            BranchId = instance.BranchId,
            InstanceName = instance.InstanceName,
            ApiUrl = instance.ApiUrl,
            ApiKey = instance.ApiKey,
            PhoneNumber = instance.PhoneNumber,
            IsActive = instance.IsActive,
            WebhookUrl = instance.WebhookUrl,
            ConnectionState = connectionState,
            WebhookConfigured = webhookInfo != null && webhookInfo.Enabled,
            WebhookEvents = webhookInfo?.Events ?? [],
            CreatedAt = instance.CreatedAt
        };

        return Ok(ApiResponse<WhatsAppInstanceDetailDto>.Ok(dto));
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

        var evolutionCreated = await _whatsAppService.CreateEvolutionInstanceAsync(request.ApiUrl, request.ApiKey, request.InstanceName);
        if (!evolutionCreated)
        {
            instance.IsDeleted = true;
            instance.DeletedAt = DateTime.UtcNow;
            _unitOfWork.WhatsAppInstances.Update(instance);
            await _unitOfWork.SaveChangesAsync();
            return BadRequest(ApiResponse<WhatsAppInstance>.Fail(
                "No se pudo crear la instancia en Evolution API. Verifica la URL y API Key."));
        }

        var webhookUrl = GetWebhookBaseUrl();
        await _whatsAppService.ConfigureWebhookAsync(instance.Id, webhookUrl);

        return Ok(ApiResponse<WhatsAppInstance>.Ok(instance, "Instancia creada exitosamente"));
    }

    [HttpDelete("instances/{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteInstance(Guid id)
    {
        var instance = await _unitOfWork.WhatsAppInstances.GetByIdAsync(id);
        if (instance == null)
            return NotFound(ApiResponse<object>.Fail("Instancia no encontrada"));

        await _whatsAppService.DeleteInstanceAsync(id);

        return Ok(ApiResponse<object>.Ok(null!, "Instancia eliminada"));
    }

    #endregion

    #region Instance Actions

    [HttpGet("instances/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<object>>> GetInstanceStatus(Guid id)
    {
        var instance = await _unitOfWork.WhatsAppInstances.GetByIdAsync(id);
        if (instance == null)
            return NotFound(ApiResponse<object>.Fail("Instancia no encontrada"));

        var connectionState = await _whatsAppService.GetConnectionStateAsync(id);
        var isConnected = connectionState == "open";

        return Ok(ApiResponse<object>.Ok(new
        {
            isConnected,
            connectionState,
            instanceId = id
        }));
    }

    [HttpGet("instances/{id:guid}/qrcode")]
    public async Task<ActionResult<ApiResponse<string>>> GetQrCode(Guid id)
    {
        var instance = await _unitOfWork.WhatsAppInstances.GetByIdAsync(id);
        if (instance == null)
            return NotFound(ApiResponse<string>.Fail("Instancia no encontrada"));

        var qr = await _whatsAppService.GetQrCodeAsync(id);
        return Ok(ApiResponse<string>.Ok(qr));
    }

    [HttpPost("instances/{id:guid}/restart")]
    public async Task<ActionResult<ApiResponse<object>>> RestartInstance(Guid id)
    {
        var instance = await _unitOfWork.WhatsAppInstances.GetByIdAsync(id);
        if (instance == null)
            return NotFound(ApiResponse<object>.Fail("Instancia no encontrada"));

        var success = await _whatsAppService.RestartInstanceAsync(id);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail("No se pudo reiniciar la instancia"));

        return Ok(ApiResponse<object>.Ok(null!, "Instancia reiniciada"));
    }

    [HttpPost("instances/{id:guid}/logout")]
    public async Task<ActionResult<ApiResponse<object>>> LogoutInstance(Guid id)
    {
        var instance = await _unitOfWork.WhatsAppInstances.GetByIdAsync(id);
        if (instance == null)
            return NotFound(ApiResponse<object>.Fail("Instancia no encontrada"));

        var success = await _whatsAppService.LogoutAsync(id);
        if (!success)
            return BadRequest(ApiResponse<object>.Fail("No se pudo desconectar la instancia"));

        return Ok(ApiResponse<object>.Ok(null!, "Sesión cerrada"));
    }

    [HttpGet("instances/{id:guid}/info")]
    public async Task<ActionResult<ApiResponse<EvolutionInstanceInfo>>> GetInstanceInfo(Guid id)
    {
        var instance = await _unitOfWork.WhatsAppInstances.GetByIdAsync(id);
        if (instance == null)
            return NotFound(ApiResponse<EvolutionInstanceInfo>.Fail("Instancia no encontrada"));

        var info = await _whatsAppService.GetInstanceInfoAsync(id);
        if (info == null)
            return NotFound(ApiResponse<EvolutionInstanceInfo>.Fail("No se pudo obtener información de Evolution API"));

        return Ok(ApiResponse<EvolutionInstanceInfo>.Ok(info));
    }

    [HttpGet("instances/{id:guid}/webhook")]
    public async Task<ActionResult<ApiResponse<object>>> GetWebhookStatus(Guid id)
    {
        var instance = await _unitOfWork.WhatsAppInstances.GetByIdAsync(id);
        if (instance == null)
            return NotFound(ApiResponse<object>.Fail("Instancia no encontrada"));

        var webhookInfo = await _whatsAppService.GetWebhookInfoAsync(id);

        return Ok(ApiResponse<object>.Ok(new
        {
            configured = webhookInfo != null,
            enabled = webhookInfo?.Enabled ?? false,
            url = webhookInfo?.WebhookUrl ?? "",
            events = webhookInfo?.Events ?? [],
            by = webhookInfo?.By ?? "",
            savedUrl = instance.WebhookUrl ?? ""
        }));
    }

    [HttpPost("instances/{id:guid}/webhook/fix")]
    public async Task<ActionResult<ApiResponse<object>>> FixWebhook(Guid id)
    {
        var instance = await _unitOfWork.WhatsAppInstances.GetByIdAsync(id);
        if (instance == null)
            return NotFound(ApiResponse<object>.Fail("Instancia no encontrada"));

        var webhookUrl = GetWebhookBaseUrl();
        var fixed_ = await _whatsAppService.FixWebhookAsync(id, webhookUrl);

        return Ok(ApiResponse<object>.Ok(new
        {
            configured = fixed_,
            url = webhookUrl
        }, fixed_ ? "Webhook configurado correctamente" : "No se pudo configurar el webhook"));
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

    #endregion

    #region Private Methods

    private string GetWebhookBaseUrl()
    {
        return _configuration["WhatsApp:WebhookBaseUrl"] ?? $"{Request.Scheme}://{Request.Host}";
    }

    private async Task HandleConnectionUpdate(System.Text.Json.JsonElement root)
    {
        if (!root.TryGetProperty("data", out var dataProp)) return;

        string? instanceName = null;
        string? state = null;

        if (dataProp.TryGetProperty("instance", out var instanceProp))
        {
            if (instanceProp.TryGetProperty("instanceName", out var nameProp))
                instanceName = nameProp.GetString();
            if (instanceProp.TryGetProperty("state", out var stateProp))
                state = stateProp.GetString();
        }

        if (string.IsNullOrEmpty(instanceName)) return;

        _logger.LogInformation("Connection update for {InstanceName}: {State}", instanceName, state);

        var instance = (await _unitOfWork.WhatsAppInstances.FindAsync(
            w => w.InstanceName == instanceName && !w.IsDeleted)).FirstOrDefault();

        if (instance == null) return;

        instance.IsActive = state == "open";
        _unitOfWork.WhatsAppInstances.Update(instance);
        await _unitOfWork.SaveChangesAsync();
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

    #endregion
}

#region DTOs

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

public class WhatsAppInstanceDetailDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? BranchId { get; set; }
    public string InstanceName { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public string? WebhookUrl { get; set; }
    public string ConnectionState { get; set; } = "unknown";
    public bool WebhookConfigured { get; set; }
    public string[] WebhookEvents { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}

#endregion
