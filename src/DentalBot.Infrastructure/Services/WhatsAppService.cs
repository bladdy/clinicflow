using System.Net.Http.Json;
using System.Text.Json;
using DentalBot.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DentalBot.Infrastructure.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<WhatsAppService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendMessageAsync(Guid instanceId, string phoneNumber, string message)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["EvolutionApi:BaseUrl"];
            var apiKey = _configuration["EvolutionApi:ApiKey"];

            client.DefaultRequestHeaders.Add("apikey", apiKey);

            var payload = new
            {
                number = phoneNumber,
                text = message
            };

            var response = await client.PostAsJsonAsync($"{baseUrl}/message/sendText/{instanceId}", payload);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Message sent to {Phone} via instance {InstanceId}", phoneNumber, instanceId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to {Phone}", phoneNumber);
            throw;
        }
    }

    public async Task SendTemplateMessageAsync(Guid instanceId, string phoneNumber, string templateName, Dictionary<string, string>? parameters = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["EvolutionApi:BaseUrl"];
            var apiKey = _configuration["EvolutionApi:ApiKey"];

            client.DefaultRequestHeaders.Add("apikey", apiKey);

            var components = new List<object>();
            if (parameters != null && parameters.Count > 0)
            {
                var paramList = parameters.Select(p => new { type = "text", text = p.Value }).ToList();
                components.Add(new
                {
                    type = "body",
                    parameters = paramList
                });
            }

            var payload = new
            {
                name = templateName,
                to = phoneNumber,
                language = new { code = "es_MX" },
                components
            };

            var response = await client.PostAsJsonAsync($"{baseUrl}/message/sendTemplate/{instanceId}", payload);
            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Template message {Template} sent to {Phone}", templateName, phoneNumber);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send template message {Template} to {Phone}", templateName, phoneNumber);
            throw;
        }
    }

    public async Task<bool> CheckConnectionAsync(Guid instanceId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["EvolutionApi:BaseUrl"];
            var apiKey = _configuration["EvolutionApi:ApiKey"];

            client.DefaultRequestHeaders.Add("apikey", apiKey);

            var response = await client.GetAsync($"{baseUrl}/instance/connectionState/{instanceId}");
            if (!response.IsSuccessStatusCode) return false;

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            var state = doc.RootElement.GetProperty("instance").GetProperty("state").GetString();
            return state == "open";
        }
        catch
        {
            return false;
        }
    }

    public async Task<string> GetQrCodeAsync(Guid instanceId)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["EvolutionApi:BaseUrl"];
            var apiKey = _configuration["EvolutionApi:ApiKey"];

            client.DefaultRequestHeaders.Add("apikey", apiKey);

            var response = await client.GetAsync($"{baseUrl}/instance/connect/{instanceId}");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return doc.RootElement.GetProperty("base64").GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get QR code for instance {InstanceId}", instanceId);
            return string.Empty;
        }
    }
}
