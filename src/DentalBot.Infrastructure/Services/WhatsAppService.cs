using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DentalBot.Infrastructure.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly IEvolutionService _evolutionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(IEvolutionService evolutionService, IUnitOfWork unitOfWork, ILogger<WhatsAppService> logger)
    {
        _evolutionService = evolutionService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    private async Task<WhatsAppInstance?> GetInstanceAsync(Guid instanceId)
    {
        return await _unitOfWork.WhatsAppInstances.GetByIdAsync(instanceId);
    }

    public async Task SendMessageAsync(Guid instanceId, string phoneNumber, string message)
    {
        var instance = await GetInstanceAsync(instanceId);
        if (instance == null)
        {
            _logger.LogWarning("WhatsApp instance {InstanceId} not found", instanceId);
            return;
        }

        var sent = await _evolutionService.SendTextAsync(instance.ApiUrl, instance.ApiKey, instance.InstanceName, phoneNumber, message);
        if (sent)
            _logger.LogInformation("Message sent to {Phone} via {InstanceName}", phoneNumber, instance.InstanceName);
        else
            _logger.LogWarning("Failed to send message to {Phone} via {InstanceName}", phoneNumber, instance.InstanceName);
    }

    public async Task SendTemplateMessageAsync(Guid instanceId, string phoneNumber, string templateName, Dictionary<string, string>? parameters = null)
    {
        await SendMessageAsync(instanceId, phoneNumber, $"[Template: {templateName}]");
    }

    public async Task<bool> CheckConnectionAsync(Guid instanceId)
    {
        var instance = await GetInstanceAsync(instanceId);
        if (instance == null) return false;

        var state = await _evolutionService.GetConnectionStateAsync(instance.ApiUrl, instance.ApiKey, instance.InstanceName);
        return state == "open";
    }

    public async Task<string> GetConnectionStateAsync(Guid instanceId)
    {
        var instance = await GetInstanceAsync(instanceId);
        if (instance == null) return "error";

        return await _evolutionService.GetConnectionStateAsync(instance.ApiUrl, instance.ApiKey, instance.InstanceName);
    }

    public async Task<string> GetQrCodeAsync(Guid instanceId)
    {
        var instance = await GetInstanceAsync(instanceId);
        if (instance == null) return string.Empty;

        var qr = await _evolutionService.GetQRCodeAsync(instance.ApiUrl, instance.ApiKey, instance.InstanceName);
        return qr ?? string.Empty;
    }

    public async Task ConfigureWebhookAsync(Guid instanceId, string webhookUrl)
    {
        var instance = await GetInstanceAsync(instanceId);
        if (instance == null)
        {
            _logger.LogWarning("WhatsApp instance {InstanceId} not found for webhook config", instanceId);
            return;
        }

        var events = new[] { "MESSAGES_UPSERT", "CONNECTION_UPDATE", "QRCODE_UPDATED" };
        var configured = await _evolutionService.SetWebhookAsync(instance.ApiUrl, instance.ApiKey, instance.InstanceName, webhookUrl, events);

        _logger.LogInformation("ConfigureWebhook result for {InstanceName}: {Configured}", instance.InstanceName, configured);

        if (configured)
        {
            instance.WebhookUrl = webhookUrl;
            _unitOfWork.WhatsAppInstances.Update(instance);
            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("Webhook configured for {InstanceName}: {Url}", instance.InstanceName, webhookUrl);
        }
    }

    public async Task<bool> VerifyWebhookAsync(Guid instanceId)
    {
        var instance = await GetInstanceAsync(instanceId);
        if (instance == null) return false;

        var webhookInfo = await _evolutionService.GetWebhookAsync(instance.ApiUrl, instance.ApiKey, instance.InstanceName);
        return webhookInfo != null && webhookInfo.Enabled;
    }

    public async Task<bool> CreateEvolutionInstanceAsync(string apiUrl, string apiKey, string instanceName)
    {
        return await _evolutionService.CreateInstanceAsync(apiUrl, apiKey, instanceName);
    }

    public async Task<bool> DeleteInstanceAsync(Guid instanceId)
    {
        var instance = await GetInstanceAsync(instanceId);
        if (instance == null) return false;

        await _evolutionService.DeleteInstanceAsync(instance.ApiUrl, instance.ApiKey, instance.InstanceName);

        instance.IsDeleted = true;
        instance.DeletedAt = DateTime.UtcNow;
        _unitOfWork.WhatsAppInstances.Update(instance);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Instance {InstanceName} deleted from DB and Evolution API", instance.InstanceName);
        return true;
    }

    public async Task<bool> RestartInstanceAsync(Guid instanceId)
    {
        var instance = await GetInstanceAsync(instanceId);
        if (instance == null) return false;

        return await _evolutionService.RestartInstanceAsync(instance.ApiUrl, instance.ApiKey, instance.InstanceName);
    }

    public async Task<bool> LogoutAsync(Guid instanceId)
    {
        var instance = await GetInstanceAsync(instanceId);
        if (instance == null) return false;

        return await _evolutionService.LogoutAsync(instance.ApiUrl, instance.ApiKey, instance.InstanceName);
    }

    public async Task<EvolutionInstanceInfo?> GetInstanceInfoAsync(Guid instanceId)
    {
        var instance = await GetInstanceAsync(instanceId);
        if (instance == null) return null;

        return await _evolutionService.GetInstanceInfoAsync(instance.ApiUrl, instance.ApiKey, instance.InstanceName);
    }

    public async Task<EvolutionWebhookInfo?> GetWebhookInfoAsync(Guid instanceId)
    {
        var instance = await GetInstanceAsync(instanceId);
        if (instance == null) return null;

        return await _evolutionService.GetWebhookAsync(instance.ApiUrl, instance.ApiKey, instance.InstanceName);
    }

    public async Task<bool> FixWebhookAsync(Guid instanceId, string webhookUrl)
    {
        await ConfigureWebhookAsync(instanceId, webhookUrl);
        return await VerifyWebhookAsync(instanceId);
    }
}
