namespace DentalBot.Shared.DTOs.Knowledge;

public class KnowledgeArticleDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Keywords { get; set; }
    public bool IsActive { get; set; }
}

public class CreateKnowledgeArticleRequest
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Keywords { get; set; }
}

public class UpdateKnowledgeArticleRequest
{
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? Category { get; set; }
    public string? Keywords { get; set; }
    public bool? IsActive { get; set; }
}
