using DentalBot.Application.Interfaces;

namespace DentalBot.Application.Interfaces;

public interface IWhatsAppService
{
    Task SendMessageAsync(Guid instanceId, string phoneNumber, string message);
    Task SendTemplateMessageAsync(Guid instanceId, string phoneNumber, string templateName, Dictionary<string, string>? parameters = null);
    Task<bool> CheckConnectionAsync(Guid instanceId);
    Task<string> GetConnectionStateAsync(Guid instanceId);
    Task<string> GetQrCodeAsync(Guid instanceId);
    Task ConfigureWebhookAsync(Guid instanceId, string webhookUrl);
    Task<bool> VerifyWebhookAsync(Guid instanceId);
    Task<bool> CreateEvolutionInstanceAsync(string apiUrl, string apiKey, string instanceName);
    Task<bool> DeleteInstanceAsync(Guid instanceId);
    Task<bool> RestartInstanceAsync(Guid instanceId);
    Task<bool> LogoutAsync(Guid instanceId);
    Task<EvolutionInstanceInfo?> GetInstanceInfoAsync(Guid instanceId);
    Task<EvolutionWebhookInfo?> GetWebhookInfoAsync(Guid instanceId);
    Task<bool> FixWebhookAsync(Guid instanceId, string webhookUrl);
}
