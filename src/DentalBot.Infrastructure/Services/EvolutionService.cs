using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using DentalBot.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace DentalBot.Infrastructure.Services;

public class EvolutionService : IEvolutionService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<EvolutionService> _logger;

    public EvolutionService(IHttpClientFactory httpClientFactory, ILogger<EvolutionService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<bool> CreateInstanceAsync(string apiUrl, string apiKey, string instanceName)
    {
        try
        {
            var client = CreateClient(apiKey);
            var payload = new
            {
                instanceName,
                integration = "WHATSAPP-BAILEYS",
                qrcode = true
            };

            var response = await client.PostAsJsonAsync($"{apiUrl}/instance/create", payload);
            var body = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Evolution API instance {InstanceName} created", instanceName);
                return true;
            }

            _logger.LogWarning("Failed to create Evolution instance {InstanceName}: {Status} - {Body}",
                instanceName, response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating Evolution instance {InstanceName}", instanceName);
            return false;
        }
    }

    public async Task<bool> DeleteInstanceAsync(string apiUrl, string apiKey, string instanceName)
    {
        try
        {
            var client = CreateClient(apiKey);
            var response = await client.DeleteAsync($"{apiUrl}/instance/delete/{instanceName}");

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Evolution API instance {InstanceName} deleted", instanceName);
                return true;
            }

            var body = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to delete Evolution instance {InstanceName}: {Status} - {Body}",
                instanceName, response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting Evolution instance {InstanceName}", instanceName);
            return false;
        }
    }

    public async Task<string?> GetQRCodeAsync(string apiUrl, string apiKey, string instanceName)
    {
        try
        {
            var client = CreateClient(apiKey);
            var response = await client.GetAsync($"{apiUrl}/instance/connect/{instanceName}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get QR for {InstanceName}: {Status}", instanceName, response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("instance", out var instanceProp) &&
                instanceProp.TryGetProperty("base64", out var base64Prop))
            {
                return base64Prop.GetString();
            }

            if (doc.RootElement.TryGetProperty("base64", out var base64Root))
            {
                return base64Root.GetString();
            }

            _logger.LogWarning("QR code not found in response for {InstanceName}", instanceName);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting QR for {InstanceName}", instanceName);
            return null;
        }
    }

    public async Task<string> GetConnectionStateAsync(string apiUrl, string apiKey, string instanceName)
    {
        try
        {
            var client = CreateClient(apiKey);
            var response = await client.GetAsync($"{apiUrl}/instance/connectionState/{instanceName}");

            if (!response.IsSuccessStatusCode)
                return "error";

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("instance", out var instanceProp) &&
                instanceProp.TryGetProperty("state", out var stateProp))
            {
                return stateProp.GetString() ?? "unknown";
            }

            return "unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting connection state for {InstanceName}", instanceName);
            return "error";
        }
    }

    public async Task<EvolutionInstanceInfo?> GetInstanceInfoAsync(string apiUrl, string apiKey, string instanceName)
    {
        try
        {
            var client = CreateClient(apiKey);
            var response = await client.GetAsync($"{apiUrl}/instance/findInstance/{instanceName}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var instances = JsonSerializer.Deserialize<List<EvolutionInstanceInfoDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return instances?.FirstOrDefault()?.ToDomain();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting instance info for {InstanceName}", instanceName);
            return null;
        }
    }

    public async Task<List<EvolutionInstanceInfo>> GetInstancesAsync(string apiUrl, string apiKey)
    {
        try
        {
            var client = CreateClient(apiKey);
            var response = await client.GetAsync($"{apiUrl}/instance/fetchInstances");

            if (!response.IsSuccessStatusCode)
                return [];

            var json = await response.Content.ReadAsStringAsync();
            var instances = JsonSerializer.Deserialize<List<EvolutionInstanceInfoDto>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return instances?.Select(i => i.ToDomain()).ToList() ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching Evolution instances");
            return [];
        }
    }

    public async Task<bool> SetWebhookAsync(string apiUrl, string apiKey, string instanceName, string webhookUrl, string[] events)
    {
        try
        {
            var client = CreateClient(apiKey);
            var payload = new
            {
                webhook = new
                {
                    enabled = true,
                    url = webhookUrl,
                    events
                }
            };

            var response = await client.PostAsJsonAsync($"{apiUrl}/webhook/set/{instanceName}", payload);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Webhook configured for {InstanceName}: {Url} with events [{Events}]",
                    instanceName, webhookUrl, string.Join(", ", events));
                return true;
            }

            var body = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to set webhook for {InstanceName}: {Status} - {Body}",
                instanceName, response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting webhook for {InstanceName}", instanceName);
            return false;
        }
    }

    public async Task<EvolutionWebhookInfo?> GetWebhookAsync(string apiUrl, string apiKey, string instanceName)
    {
        try
        {
            var client = CreateClient(apiKey);
            var response = await client.GetAsync($"{apiUrl}/webhook/find/{instanceName}");

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();

            if (json == "null" || string.IsNullOrWhiteSpace(json))
                return null;

            var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("webhook", out var webhookProp))
                return null;

            var webhookUrl = webhookProp.TryGetProperty("url", out var urlProp) ? urlProp.GetString() ?? "" : "";
            var enabled = webhookProp.TryGetProperty("enabled", out var enabledProp) && enabledProp.GetBoolean();
            var by = webhookProp.TryGetProperty("by", out var byProp) ? byProp.GetString() ?? "" : "";

            string[] webhookEvents = [];
            if (webhookProp.TryGetProperty("events", out var eventsProp) && eventsProp.ValueKind == JsonValueKind.Array)
            {
                webhookEvents = eventsProp.EnumerateArray()
                    .Select(e => e.GetString() ?? "")
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToArray();
            }

            return new EvolutionWebhookInfo
            {
                WebhookUrl = webhookUrl,
                Enabled = enabled,
                Events = webhookEvents,
                By = by
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting webhook for {InstanceName}", instanceName);
            return null;
        }
    }

    public async Task<bool> SendTextAsync(string apiUrl, string apiKey, string instanceName, string number, string message)
    {
        try
        {
            var client = CreateClient(apiKey);
            var payload = new { number, text = message };
            var response = await client.PostAsJsonAsync($"{apiUrl}/message/sendText/{instanceName}", payload);

            if (response.IsSuccessStatusCode)
                return true;

            var body = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to send text to {Number} via {InstanceName}: {Status} - {Body}",
                number, instanceName, response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending text to {Number} via {InstanceName}", number, instanceName);
            return false;
        }
    }

    public async Task<bool> SendImageAsync(string apiUrl, string apiKey, string instanceName, string number, string imageUrl, string? caption)
    {
        try
        {
            var client = CreateClient(apiKey);
            var payload = new
            {
                number,
                mediatype = "image",
                media = imageUrl,
                caption = caption ?? ""
            };
            var response = await client.PostAsJsonAsync($"{apiUrl}/message/sendMedia/{instanceName}", payload);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending image to {Number} via {InstanceName}", number, instanceName);
            return false;
        }
    }

    public async Task<bool> SendDocumentAsync(string apiUrl, string apiKey, string instanceName, string number, string documentUrl, string? caption)
    {
        try
        {
            var client = CreateClient(apiKey);
            var payload = new
            {
                number,
                mediatype = "document",
                media = documentUrl,
                caption = caption ?? ""
            };
            var response = await client.PostAsJsonAsync($"{apiUrl}/message/sendMedia/{instanceName}", payload);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending document to {Number} via {InstanceName}", number, instanceName);
            return false;
        }
    }

    public async Task<bool> RestartInstanceAsync(string apiUrl, string apiKey, string instanceName)
    {
        try
        {
            var client = CreateClient(apiKey);
            var response = await client.PutAsync($"{apiUrl}/instance/restart/{instanceName}", null);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Evolution instance {InstanceName} restarted", instanceName);
                return true;
            }

            var body = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to restart {InstanceName}: {Status} - {Body}", instanceName, response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting {InstanceName}", instanceName);
            return false;
        }
    }

    public async Task<bool> LogoutAsync(string apiUrl, string apiKey, string instanceName)
    {
        try
        {
            var client = CreateClient(apiKey);
            var response = await client.PutAsync($"{apiUrl}/instance/logout/{instanceName}", null);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Evolution instance {InstanceName} logged out", instanceName);
                return true;
            }

            var body = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Failed to logout {InstanceName}: {Status} - {Body}", instanceName, response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging out {InstanceName}", instanceName);
            return false;
        }
    }

    private HttpClient CreateClient(string apiKey)
    {
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("apikey", apiKey);
        return client;
    }
}

#region Evolution API DTOs

internal class EvolutionInstanceInfoDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("connectionStatus")]
    public string ConnectionStatus { get; set; } = string.Empty;

    [JsonPropertyName("ownerJid")]
    public string? OwnerJid { get; set; }

    [JsonPropertyName("profileName")]
    public string? ProfileName { get; set; }

    [JsonPropertyName("profilePicUrl")]
    public string? ProfilePicUrl { get; set; }

    [JsonPropertyName("number")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("integration")]
    public string Integration { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updatedAt")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("_count")]
    public EvolutionCountDto? Count { get; set; }

    public EvolutionInstanceInfo ToDomain() => new()
    {
        Id = Id,
        Name = Name,
        ConnectionStatus = ConnectionStatus,
        OwnerJid = OwnerJid,
        ProfileName = ProfileName,
        ProfilePicUrl = ProfilePicUrl,
        PhoneNumber = PhoneNumber,
        Integration = Integration,
        CreatedAt = CreatedAt,
        UpdatedAt = UpdatedAt,
        MessageCount = Count?.Message ?? 0,
        ContactCount = Count?.Contact ?? 0,
        ChatCount = Count?.Chat ?? 0
    };
}

internal class EvolutionCountDto
{
    [JsonPropertyName("Message")]
    public int Message { get; set; }

    [JsonPropertyName("Contact")]
    public int Contact { get; set; }

    [JsonPropertyName("Chat")]
    public int Chat { get; set; }
}

#endregion
