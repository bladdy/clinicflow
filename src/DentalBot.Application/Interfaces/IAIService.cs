namespace DentalBot.Application.Interfaces;

public interface IAIService
{
    Task<string> GenerateResponseAsync(string message, string conversationContext, string systemPrompt, List<string> availableTools);
    Task<string> DetectIntentAsync(string message);
    Task<bool> IsUrgentAsync(string message);
}
