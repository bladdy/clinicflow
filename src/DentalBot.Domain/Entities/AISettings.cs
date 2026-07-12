using DentalBot.Domain.Common;

namespace DentalBot.Domain.Entities;

public class AISettings : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string? OllamaUrl { get; set; }
    public string? ModelName { get; set; }
    public string? SystemPrompt { get; set; }
    public int MaxTokens { get; set; }
    public decimal Temperature { get; set; }
    public bool IsEnabled { get; set; }
    public string? WelcomeMessage { get; set; }
    public string? TransferMessage { get; set; }

    public Company Company { get; set; } = null!;
}
