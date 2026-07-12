namespace DentalBot.Application.Interfaces;

public interface IWhatsAppService
{
    Task SendMessageAsync(Guid instanceId, string phoneNumber, string message);
    Task SendTemplateMessageAsync(Guid instanceId, string phoneNumber, string templateName, Dictionary<string, string>? parameters = null);
    Task<bool> CheckConnectionAsync(Guid instanceId);
    Task<string> GetQrCodeAsync(Guid instanceId);
}
