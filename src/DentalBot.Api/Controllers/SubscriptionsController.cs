using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using DentalBot.Domain.Enums;
using DentalBot.Shared.DTOs.Common;
using DentalBot.Shared.DTOs.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public SubscriptionsController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    [HttpGet("current")]
    public async Task<ActionResult<ApiResponse<CompanySubscriptionDto>>> GetCurrent()
    {
        var companyId = GetCompanyId();
        if (companyId == null) return BadRequest(ApiResponse<CompanySubscriptionDto>.Fail("CompanyId no encontrado"));

        var sub = (await _unitOfWork.CompanySubscriptions.FindAsync(
            s => s.CompanyId == companyId.Value && s.Status == SubscriptionStatus.Active))
            .FirstOrDefault();

        if (sub == null)
            return Ok(ApiResponse<CompanySubscriptionDto>.Fail("No hay suscripción activa"));

        var plan = await _unitOfWork.Plans.GetByIdAsync(sub.PlanId);
        var company = await _unitOfWork.Companies.GetByIdAsync(companyId.Value);

        return Ok(ApiResponse<CompanySubscriptionDto>.Ok(MapToDto(sub, plan?.Name ?? "", company?.Name ?? "")));
    }

    [HttpGet("all")]
    public async Task<ActionResult<ApiResponse<PagedResult<CompanySubscriptionDto>>>> GetAll([FromQuery] PaginationQuery pagination)
    {
        var (items, totalCount) = await _unitOfWork.CompanySubscriptions.GetPagedAsync(
            pagination.Page, pagination.PageSize,
            orderBy: q => q.OrderByDescending(s => s.StartDate));

        var dtos = new List<CompanySubscriptionDto>();
        foreach (var s in items)
        {
            var plan = await _unitOfWork.Plans.GetByIdAsync(s.PlanId);
            var company = await _unitOfWork.Companies.GetByIdAsync(s.CompanyId);
            dtos.Add(MapToDto(s, plan?.Name ?? "", company?.Name ?? ""));
        }

        return Ok(ApiResponse<PagedResult<CompanySubscriptionDto>>.Ok(new PagedResult<CompanySubscriptionDto>
        {
            Items = dtos, TotalCount = totalCount, Page = pagination.Page, PageSize = pagination.PageSize
        }));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CompanySubscriptionDto>>> Create([FromBody] CreateSubscriptionRequest request)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(request.CompanyId);
        if (company == null) return BadRequest(ApiResponse<CompanySubscriptionDto>.Fail("Empresa no encontrada"));

        var plan = await _unitOfWork.Plans.GetByIdAsync(request.PlanId);
        if (plan == null) return BadRequest(ApiResponse<CompanySubscriptionDto>.Fail("Plan no encontrado"));

        var existing = (await _unitOfWork.CompanySubscriptions.FindAsync(
            s => s.CompanyId == request.CompanyId && s.Status == SubscriptionStatus.Active)).FirstOrDefault();
        if (existing != null)
            return BadRequest(ApiResponse<CompanySubscriptionDto>.Fail("La empresa ya tiene una suscripción activa"));

        var sub = new CompanySubscription
        {
            Id = Guid.NewGuid(),
            CompanyId = request.CompanyId,
            PlanId = request.PlanId,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow,
            EndDate = request.IsAnnual ? DateTime.UtcNow.AddYears(1) : DateTime.UtcNow.AddMonths(1),
            IsAnnual = request.IsAnnual
        };

        await _unitOfWork.CompanySubscriptions.AddAsync(sub);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetCurrent), ApiResponse<CompanySubscriptionDto>.Ok(MapToDto(sub, plan.Name, company.Name), "Suscripción creada"));
    }

    [HttpPost("{id:guid}/change-plan")]
    public async Task<ActionResult<ApiResponse<CompanySubscriptionDto>>> ChangePlan(Guid id, [FromBody] ChangePlanRequest request)
    {
        var sub = await _unitOfWork.CompanySubscriptions.GetByIdAsync(id);
        if (sub == null) return NotFound(ApiResponse<CompanySubscriptionDto>.Fail("Suscripción no encontrada"));

        var newPlan = await _unitOfWork.Plans.GetByIdAsync(request.NewPlanId);
        if (newPlan == null) return BadRequest(ApiResponse<CompanySubscriptionDto>.Fail("Nuevo plan no encontrado"));

        sub.PlanId = request.NewPlanId;
        _unitOfWork.CompanySubscriptions.Update(sub);
        await _unitOfWork.SaveChangesAsync();

        var company = await _unitOfWork.Companies.GetByIdAsync(sub.CompanyId);
        return Ok(ApiResponse<CompanySubscriptionDto>.Ok(MapToDto(sub, newPlan.Name, company?.Name ?? ""), "Plan actualizado"));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ApiResponse<CompanySubscriptionDto>>> Cancel(Guid id)
    {
        var sub = await _unitOfWork.CompanySubscriptions.GetByIdAsync(id);
        if (sub == null) return NotFound(ApiResponse<CompanySubscriptionDto>.Fail("Suscripción no encontrada"));

        sub.Status = SubscriptionStatus.Cancelled;
        sub.EndDate = DateTime.UtcNow;
        _unitOfWork.CompanySubscriptions.Update(sub);
        await _unitOfWork.SaveChangesAsync();

        var plan = await _unitOfWork.Plans.GetByIdAsync(sub.PlanId);
        var company = await _unitOfWork.Companies.GetByIdAsync(sub.CompanyId);
        return Ok(ApiResponse<CompanySubscriptionDto>.Ok(MapToDto(sub, plan?.Name ?? "", company?.Name ?? ""), "Suscripción cancelada"));
    }

    [HttpGet("check-limits")]
    public async Task<ActionResult<ApiResponse<SubscriptionLimitsDto>>> CheckLimits()
    {
        var companyId = GetCompanyId();
        if (companyId == null) return BadRequest(ApiResponse<SubscriptionLimitsDto>.Fail("CompanyId no encontrado"));

        var sub = (await _unitOfWork.CompanySubscriptions.FindAsync(
            s => s.CompanyId == companyId.Value && s.Status == SubscriptionStatus.Active)).FirstOrDefault();

        if (sub == null)
            return Ok(ApiResponse<SubscriptionLimitsDto>.Ok(new SubscriptionLimitsDto
            {
                HasActiveSubscription = false, CanAddDoctor = false, CanAddPatient = false,
                CanAddBranch = false, CanCreateAppointment = false, CanUseAI = false
            }));

        var plan = await _unitOfWork.Plans.GetByIdAsync(sub.PlanId);
        if (plan == null) return BadRequest(ApiResponse<SubscriptionLimitsDto>.Fail("Plan no encontrado"));

        var limits = new SubscriptionLimitsDto
        {
            HasActiveSubscription = true,
            CanAddDoctor = sub.CurrentDoctors < plan.MaxDoctors,
            CanAddPatient = sub.CurrentPatients < plan.MaxPatients,
            CanAddBranch = sub.CurrentBranches < plan.MaxBranches,
            CanCreateAppointment = sub.AppointmentsThisMonth < plan.MaxAppointmentsPerMonth,
            CanUseAI = plan.HasAI,
            CanUseWhatsApp = plan.HasWhatsAppIntegration,
            DoctorsRemaining = Math.Max(0, plan.MaxDoctors - sub.CurrentDoctors),
            PatientsRemaining = Math.Max(0, plan.MaxPatients - sub.CurrentPatients),
            BranchesRemaining = Math.Max(0, plan.MaxBranches - sub.CurrentBranches),
            AppointmentsRemaining = Math.Max(0, plan.MaxAppointmentsPerMonth - sub.AppointmentsThisMonth),
            PlanName = plan.Name
        };

        return Ok(ApiResponse<SubscriptionLimitsDto>.Ok(limits));
    }

    private Guid? GetCompanyId()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == "companyId");
        if (claim != null && Guid.TryParse(claim.Value, out var companyId)) return companyId;
        return null;
    }

    private static CompanySubscriptionDto MapToDto(CompanySubscription s, string planName, string companyName) => new()
    {
        Id = s.Id, CompanyId = s.CompanyId, CompanyName = companyName,
        PlanId = s.PlanId, PlanName = planName, Status = s.Status.ToString(),
        StartDate = s.StartDate, EndDate = s.EndDate, IsAnnual = s.IsAnnual,
        CurrentDoctors = s.CurrentDoctors, CurrentPatients = s.CurrentPatients, CurrentBranches = s.CurrentBranches,
        AppointmentsThisMonth = s.AppointmentsThisMonth, ConversationsThisMonth = s.ConversationsThisMonth
    };
}

public class SubscriptionLimitsDto
{
    public bool HasActiveSubscription { get; set; }
    public bool CanAddDoctor { get; set; }
    public bool CanAddPatient { get; set; }
    public bool CanAddBranch { get; set; }
    public bool CanCreateAppointment { get; set; }
    public bool CanUseAI { get; set; }
    public bool CanUseWhatsApp { get; set; }
    public int DoctorsRemaining { get; set; }
    public int PatientsRemaining { get; set; }
    public int BranchesRemaining { get; set; }
    public int AppointmentsRemaining { get; set; }
    public string PlanName { get; set; } = string.Empty;
}
