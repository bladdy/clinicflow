namespace DentalBot.Shared.DTOs.AI;

public class AISettingsDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string OllamaUrl { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public int MaxTokens { get; set; }
    public decimal Temperature { get; set; }
    public bool IsEnabled { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? TransferMessage { get; set; }
}

public class UpdateAISettingsRequest
{
    public string OllamaUrl { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string SystemPrompt { get; set; } = string.Empty;
    public int MaxTokens { get; set; } = 500;
    public decimal Temperature { get; set; } = 0.7m;
    public bool IsEnabled { get; set; } = true;
    public string? WelcomeMessage { get; set; }
    public string? TransferMessage { get; set; }
}
