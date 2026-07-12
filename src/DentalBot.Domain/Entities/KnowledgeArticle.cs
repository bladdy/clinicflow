using DentalBot.Domain.Common;

namespace DentalBot.Domain.Entities;

public class KnowledgeArticle : BaseEntity
{
    public Guid CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Keywords { get; set; }
    public bool IsActive { get; set; }

    public Company Company { get; set; } = null!;
}
