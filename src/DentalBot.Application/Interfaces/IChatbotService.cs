using DentalBot.Domain.Entities;

namespace DentalBot.Application.Interfaces;

public interface IChatbotService
{
    Task<string> HandleMessageAsync(Conversation conversation, string userMessage, Guid companyId);
}
