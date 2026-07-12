using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using DentalBot.Shared.DTOs.Common;
using DentalBot.Shared.DTOs.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PlansController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public PlansController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PlanDto>>>> GetAll([FromQuery] PaginationQuery pagination)
    {
        var (items, totalCount) = await _unitOfWork.Plans.GetPagedAsync(
            pagination.Page, pagination.PageSize,
            orderBy: q => q.OrderBy(p => p.SortOrder));

        var dtos = items.Select(p => MapToDto(p)).ToList();
        return Ok(ApiResponse<PagedResult<PlanDto>>.Ok(new PagedResult<PlanDto>
        {
            Items = dtos, TotalCount = totalCount, Page = pagination.Page, PageSize = pagination.PageSize
        }));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PlanDto>>> GetById(Guid id)
    {
        var plan = await _unitOfWork.Plans.GetByIdAsync(id);
        if (plan == null) return NotFound(ApiResponse<PlanDto>.Fail("Plan no encontrado"));
        return Ok(ApiResponse<PlanDto>.Ok(MapToDto(plan)));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PlanDto>>> Create([FromBody] CreatePlanRequest request)
    {
        var plan = new Plan
        {
            Id = Guid.NewGuid(),
            Name = request.Name, Description = request.Description,
            MonthlyPrice = request.MonthlyPrice, AnnualPrice = request.AnnualPrice,
            MaxBranches = request.MaxBranches, MaxDoctors = request.MaxDoctors, MaxPatients = request.MaxPatients,
            MaxAppointmentsPerMonth = request.MaxAppointmentsPerMonth, MaxConversationsPerMonth = request.MaxConversationsPerMonth,
            HasAI = request.HasAI, HasWhatsAppIntegration = request.HasWhatsAppIntegration,
            HasAdvancedReports = request.HasAdvancedReports, HasPrioritySupport = request.HasPrioritySupport,
            IsActive = true, SortOrder = (await _unitOfWork.Plans.GetAllAsync()).Count
        };
        await _unitOfWork.Plans.AddAsync(plan);
        await _unitOfWork.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = plan.Id }, ApiResponse<PlanDto>.Ok(MapToDto(plan), "Plan creado"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PlanDto>>> Update(Guid id, [FromBody] UpdatePlanRequest request)
    {
        var plan = await _unitOfWork.Plans.GetByIdAsync(id);
        if (plan == null) return NotFound(ApiResponse<PlanDto>.Fail("Plan no encontrado"));
        if (request.Name != null) plan.Name = request.Name;
        if (request.Description != null) plan.Description = request.Description;
        if (request.MonthlyPrice.HasValue) plan.MonthlyPrice = request.MonthlyPrice.Value;
        if (request.AnnualPrice.HasValue) plan.AnnualPrice = request.AnnualPrice.Value;
        if (request.MaxBranches.HasValue) plan.MaxBranches = request.MaxBranches.Value;
        if (request.MaxDoctors.HasValue) plan.MaxDoctors = request.MaxDoctors.Value;
        if (request.MaxPatients.HasValue) plan.MaxPatients = request.MaxPatients.Value;
        if (request.MaxAppointmentsPerMonth.HasValue) plan.MaxAppointmentsPerMonth = request.MaxAppointmentsPerMonth.Value;
        if (request.MaxConversationsPerMonth.HasValue) plan.MaxConversationsPerMonth = request.MaxConversationsPerMonth.Value;
        if (request.HasAI.HasValue) plan.HasAI = request.HasAI.Value;
        if (request.HasWhatsAppIntegration.HasValue) plan.HasWhatsAppIntegration = request.HasWhatsAppIntegration.Value;
        if (request.HasAdvancedReports.HasValue) plan.HasAdvancedReports = request.HasAdvancedReports.Value;
        if (request.HasPrioritySupport.HasValue) plan.HasPrioritySupport = request.HasPrioritySupport.Value;
        _unitOfWork.Plans.Update(plan);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<PlanDto>.Ok(MapToDto(plan), "Plan actualizado"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var plan = await _unitOfWork.Plans.GetByIdAsync(id);
        if (plan == null) return NotFound(ApiResponse<object>.Fail("Plan no encontrado"));
        _unitOfWork.Plans.SoftDelete(plan);
        await _unitOfWork.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null!, "Plan eliminado"));
    }

    private static PlanDto MapToDto(Plan p) => new()
    {
        Id = p.Id, Name = p.Name, Description = p.Description,
        MonthlyPrice = p.MonthlyPrice, AnnualPrice = p.AnnualPrice,
        MaxBranches = p.MaxBranches, MaxDoctors = p.MaxDoctors, MaxPatients = p.MaxPatients,
        MaxAppointmentsPerMonth = p.MaxAppointmentsPerMonth, MaxConversationsPerMonth = p.MaxConversationsPerMonth,
        HasAI = p.HasAI, HasWhatsAppIntegration = p.HasWhatsAppIntegration,
        HasAdvancedReports = p.HasAdvancedReports, HasPrioritySupport = p.HasPrioritySupport,
        IsActive = p.IsActive
    };
}
