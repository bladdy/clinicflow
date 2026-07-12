using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using DentalBot.Domain.Enums;
using DentalBot.Shared.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConversationsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ConversationsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ConversationDto>>>> GetAll([FromQuery] PaginationQuery pagination, [FromQuery] string? status)
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<PagedResult<ConversationDto>>.Fail("CompanyId no encontrado"));

        var (items, totalCount) = await _unitOfWork.Conversations.GetPagedAsync(
            pagination.Page,
            pagination.PageSize,
            filter: c => c.CompanyId == companyId.Value &&
                         (string.IsNullOrEmpty(status) || c.Status.ToString() == status),
            orderBy: q => q.OrderByDescending(c => c.StartedAt)
        );

        var dtos = new List<ConversationDto>();
        foreach (var c in items)
        {
            Patient? patient = null;
            if (c.PatientId.HasValue)
                patient = await _unitOfWork.Patients.GetByIdAsync(c.PatientId.Value);

            var messages = await _unitOfWork.Messages.FindAsync(m => m.ConversationId == c.Id);
            var lastMessage = messages.OrderByDescending(m => m.SentAt).FirstOrDefault();

            dtos.Add(new ConversationDto
            {
                Id = c.Id,
                CompanyId = c.CompanyId,
                PatientId = c.PatientId,
                Phone = c.Phone,
                Status = c.Status.ToString(),
                StartedAt = c.StartedAt,
                EndedAt = c.EndedAt,
                PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : null,
                LastMessageContent = lastMessage?.Content,
                LastMessageAt = lastMessage?.SentAt
            });
        }

        var result = new PagedResult<ConversationDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };

        return Ok(ApiResponse<PagedResult<ConversationDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ConversationDetailDto>>> GetById(Guid id)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(id);
        if (conversation == null)
            return NotFound(ApiResponse<ConversationDetailDto>.Fail("Conversación no encontrada"));

        Patient? patient = null;
        if (conversation.PatientId.HasValue)
            patient = await _unitOfWork.Patients.GetByIdAsync(conversation.PatientId.Value);

        var messages = (await _unitOfWork.Messages.FindAsync(m => m.ConversationId == id))
            .OrderBy(m => m.SentAt)
            .ToList();

        var dto = new ConversationDetailDto
        {
            Id = conversation.Id,
            CompanyId = conversation.CompanyId,
            PatientId = conversation.PatientId,
            Phone = conversation.Phone,
            Status = conversation.Status.ToString(),
            StartedAt = conversation.StartedAt,
            EndedAt = conversation.EndedAt,
            PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : null,
            Messages = messages.Select(m => new MessageDto
            {
                Id = m.Id,
                Content = m.Content,
                Direction = m.Direction.ToString(),
                SenderType = m.SenderType.ToString(),
                SentAt = m.SentAt,
                IsRead = m.IsRead
            }).ToList()
        };

        return Ok(ApiResponse<ConversationDetailDto>.Ok(dto));
    }

    [HttpPut("{id:guid}/close")]
    public async Task<ActionResult<ApiResponse<object>>> Close(Guid id)
    {
        var conversation = await _unitOfWork.Conversations.GetByIdAsync(id);
        if (conversation == null)
            return NotFound(ApiResponse<object>.Fail("Conversación no encontrada"));

        conversation.Status = ConversationStatus.Closed;
        conversation.EndedAt = DateTime.UtcNow;
        _unitOfWork.Conversations.Update(conversation);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null!, "Conversación cerrada"));
    }

    private Guid? GetCompanyId()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == "companyId");
        if (claim != null && Guid.TryParse(claim.Value, out var companyId))
            return companyId;
        return null;
    }
}

public class ConversationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid? PatientId { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? PatientName { get; set; }
    public string? LastMessageContent { get; set; }
    public DateTime? LastMessageAt { get; set; }
}

public class ConversationDetailDto : ConversationDto
{
    public List<MessageDto> Messages { get; set; } = new();
}

public class MessageDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public string SenderType { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
}
