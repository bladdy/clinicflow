namespace DentalBot.Application.Interfaces;

public interface IEvolutionService
{
    Task<bool> CreateInstanceAsync(string apiUrl, string apiKey, string instanceName);
    Task<bool> DeleteInstanceAsync(string apiUrl, string apiKey, string instanceName);
    Task<string?> GetQRCodeAsync(string apiUrl, string apiKey, string instanceName);
    Task<string> GetConnectionStateAsync(string apiUrl, string apiKey, string instanceName);
    Task<EvolutionInstanceInfo?> GetInstanceInfoAsync(string apiUrl, string apiKey, string instanceName);
    Task<List<EvolutionInstanceInfo>> GetInstancesAsync(string apiUrl, string apiKey);
    Task<bool> SetWebhookAsync(string apiUrl, string apiKey, string instanceName, string webhookUrl, string[] events);
    Task<EvolutionWebhookInfo?> GetWebhookAsync(string apiUrl, string apiKey, string instanceName);
    Task<bool> SendTextAsync(string apiUrl, string apiKey, string instanceName, string number, string message);
    Task<bool> SendImageAsync(string apiUrl, string apiKey, string instanceName, string number, string imageUrl, string? caption);
    Task<bool> SendDocumentAsync(string apiUrl, string apiKey, string instanceName, string number, string documentUrl, string? caption);
    Task<bool> RestartInstanceAsync(string apiUrl, string apiKey, string instanceName);
    Task<bool> LogoutAsync(string apiUrl, string apiKey, string instanceName);
    Task<bool> ConfigureSettingsAsync(string apiUrl, string apiKey, string instanceName, EvolutionSettingsRequest settings);
}

public class EvolutionSettingsRequest
{
    public bool RejectCall { get; set; }
    public string MsgCall { get; set; } = string.Empty;
    public bool GroupsIgnore { get; set; }
    public bool AlwaysOnline { get; set; }
    public bool ReadMessages { get; set; }
    public bool ReadStatus { get; set; }
    public bool SyncFullHistory { get; set; }
}

public class EvolutionInstanceInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ConnectionStatus { get; set; } = string.Empty;
    public string? OwnerJid { get; set; }
    public string? ProfileName { get; set; }
    public string? ProfilePicUrl { get; set; }
    public string? PhoneNumber { get; set; }
    public string Integration { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int MessageCount { get; set; }
    public int ContactCount { get; set; }
    public int ChatCount { get; set; }
}

public class EvolutionWebhookInfo
{
    public string WebhookUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string[] Events { get; set; } = [];
    public string By { get; set; } = string.Empty;
}
