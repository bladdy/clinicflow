using System.Net.Http.Json;
using System.Text.Json;
using DentalBot.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DentalBot.Infrastructure.Services;

public class AIService : IAIService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIService> _logger;

    public AIService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<AIService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GenerateResponseAsync(string message, string conversationContext, string systemPrompt, List<string> availableTools)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["Ollama:BaseUrl"];
            var model = _configuration["Ollama:Model"];

            var toolsDescription = availableTools.Count > 0
                ? "\n\nHerramientas disponibles: " + string.Join(", ", availableTools)
                : "";

            var fullSystemPrompt = $"{systemPrompt}{toolsDescription}\n\nContexto de la conversación:\n{conversationContext}";

            var payload = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = fullSystemPrompt },
                    new { role = "user", content = message }
                },
                stream = false
            };

            var response = await client.PostAsJsonAsync($"{baseUrl}/api/chat", payload);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("message", out var msgElement) &&
                msgElement.TryGetProperty("content", out var contentElement))
            {
                return contentElement.GetString() ?? "Lo siento, no puedo procesar tu mensaje en este momento.";
            }

            return "Lo siento, no puedo procesar tu mensaje en este momento.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate AI response");
            return "Lo siento, hubo un error al procesar tu mensaje. Por favor, intenta de nuevo o comunícate directamente con la clínica.";
        }
    }

    public async Task<string> DetectIntentAsync(string message)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["Ollama:BaseUrl"];
            var model = _configuration["Ollama:Model"];

            var payload = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "Clasifica el siguiente mensaje del usuario en UNA de estas categorías: AGENDAR_CITA, REAGENDAR_CITA, CANCELAR_CITA, CONSULTAR_HORARIOS, CONSULTAR_PRECIOS, CONSULTAR_SERVICIOS, PREGUNTA_FAQUE, URGENCIA, SALUDO, OTRO. Responde SOLO con la categoría, nada más." },
                    new { role = "user", content = message }
                },
                stream = false
            };

            var response = await client.PostAsJsonAsync($"{baseUrl}/api/chat", payload);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("message", out var msgElement) &&
                msgElement.TryGetProperty("content", out var contentElement))
            {
                return contentElement.GetString()?.Trim() ?? "OTRO";
            }

            return "OTRO";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to detect intent");
            return "OTRO";
        }
    }

    public async Task<bool> IsUrgentAsync(string message)
    {
        try
        {
            var client = _httpClientFactory.CreateClient();
            var baseUrl = _configuration["Ollama:BaseUrl"];
            var model = _configuration["Ollama:Model"];

            var payload = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "Analiza el siguiente mensaje y determina si es una URGENCIA dental (dolor intenso, sangrado abundante, trauma, hinchazón severa, fiebre). Responde SOLO 'true' o 'false'." },
                    new { role = "user", content = message }
                },
                stream = false
            };

            var response = await client.PostAsJsonAsync($"{baseUrl}/api/chat", payload);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("message", out var msgElement) &&
                msgElement.TryGetProperty("content", out var contentElement))
            {
                var content = contentElement.GetString()?.Trim().ToLower();
                return content == "true";
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking urgency for message");
            return false;
        }
    }
}
