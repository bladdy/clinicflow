using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using DentalBot.Shared.DTOs.Common;
using DentalBot.Shared.DTOs.Knowledge;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class KnowledgeArticlesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public KnowledgeArticlesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<KnowledgeArticleDto>>>> GetAll([FromQuery] PaginationQuery pagination, [FromQuery] string? category)
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<PagedResult<KnowledgeArticleDto>>.Fail("CompanyId no encontrado"));

        var (items, totalCount) = await _unitOfWork.KnowledgeArticles.GetPagedAsync(
            pagination.Page,
            pagination.PageSize,
            filter: a => a.CompanyId == companyId.Value &&
                         !a.IsDeleted &&
                         (string.IsNullOrEmpty(category) || a.Category == category),
            orderBy: q => q.OrderByDescending(a => a.CreatedAt)
        );

        var dtos = items.Select(MapToDto).ToList();

        var result = new PagedResult<KnowledgeArticleDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };

        return Ok(ApiResponse<PagedResult<KnowledgeArticleDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<KnowledgeArticleDto>>> GetById(Guid id)
    {
        var article = await _unitOfWork.KnowledgeArticles.GetByIdAsync(id);
        if (article == null || article.IsDeleted)
            return NotFound(ApiResponse<KnowledgeArticleDto>.Fail("Artículo no encontrado"));

        return Ok(ApiResponse<KnowledgeArticleDto>.Ok(MapToDto(article)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<KnowledgeArticleDto>>> Create([FromBody] CreateKnowledgeArticleRequest request)
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<KnowledgeArticleDto>.Fail("CompanyId no encontrado"));

        var article = new KnowledgeArticle
        {
            CompanyId = companyId.Value,
            Title = request.Title,
            Content = request.Content,
            Category = request.Category,
            Keywords = request.Keywords,
            IsActive = true
        };

        await _unitOfWork.KnowledgeArticles.AddAsync(article);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = article.Id }, ApiResponse<KnowledgeArticleDto>.Ok(MapToDto(article), "Artículo creado exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<KnowledgeArticleDto>>> Update(Guid id, [FromBody] UpdateKnowledgeArticleRequest request)
    {
        var article = await _unitOfWork.KnowledgeArticles.GetByIdAsync(id);
        if (article == null || article.IsDeleted)
            return NotFound(ApiResponse<KnowledgeArticleDto>.Fail("Artículo no encontrado"));

        if (request.Title != null) article.Title = request.Title;
        if (request.Content != null) article.Content = request.Content;
        if (request.Category != null) article.Category = request.Category;
        if (request.Keywords != null) article.Keywords = request.Keywords;
        if (request.IsActive.HasValue) article.IsActive = request.IsActive.Value;
        article.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.KnowledgeArticles.Update(article);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<KnowledgeArticleDto>.Ok(MapToDto(article), "Artículo actualizado exitosamente"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var article = await _unitOfWork.KnowledgeArticles.GetByIdAsync(id);
        if (article == null || article.IsDeleted)
            return NotFound(ApiResponse<object>.Fail("Artículo no encontrado"));

        _unitOfWork.KnowledgeArticles.SoftDelete(article);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null!, "Artículo eliminado exitosamente"));
    }

    private static KnowledgeArticleDto MapToDto(KnowledgeArticle a) => new()
    {
        Id = a.Id,
        CompanyId = a.CompanyId,
        Title = a.Title,
        Content = a.Content,
        Category = a.Category,
        Keywords = a.Keywords,
        IsActive = a.IsActive
    };

    private Guid? GetCompanyId()
    {
        var claim = User.FindFirst("companyId")?.Value;
        return Guid.TryParse(claim, out var companyId) ? companyId : null;
    }
}
