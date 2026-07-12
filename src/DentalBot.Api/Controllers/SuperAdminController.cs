using DentalBot.Application.Interfaces;
using DentalBot.Domain.Enums;
using DentalBot.Shared.DTOs.Common;
using DentalBot.Shared.DTOs.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalBot.Api.Controllers;

[ApiController]
[Route("api/super-admin")]
[Authorize(Roles = "Administrador")]
public class SuperAdminController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public SuperAdminController(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    [HttpGet("companies")]
    public async Task<ActionResult<ApiResponse<PagedResult<CompanyAdminDto>>>> GetCompanies([FromQuery] PaginationQuery pagination, [FromQuery] string? search)
    {
        var (items, totalCount) = await _unitOfWork.Companies.GetPagedAsync(
            pagination.Page, pagination.PageSize,
            filter: string.IsNullOrEmpty(search) ? null : c => c.Name.Contains(search) || c.Email.Contains(search),
            orderBy: q => q.OrderBy(c => c.Name));

        var dtos = new List<CompanyAdminDto>();
        foreach (var c in items)
        {
            var sub = (await _unitOfWork.CompanySubscriptions.FindAsync(
                s => s.CompanyId == c.Id && s.Status == SubscriptionStatus.Active)).FirstOrDefault();
            var plan = sub != null ? await _unitOfWork.Plans.GetByIdAsync(sub.PlanId) : null;
            var doctors = (await _unitOfWork.Doctors.FindAsync(d => d.CompanyId == c.Id && !d.IsDeleted)).Count;
            var patients = (await _unitOfWork.Patients.FindAsync(p => p.CompanyId == c.Id && !p.IsDeleted)).Count;
            var appointments = (await _unitOfWork.Appointments.FindAsync(a => a.CompanyId == c.Id && !a.IsDeleted)).Count;

            dtos.Add(new CompanyAdminDto
            {
                Id = c.Id, Name = c.Name, Email = c.Email, Phone = c.Phone,
                PlanName = plan?.Name ?? "Sin plan", SubscriptionStatus = sub?.Status.ToString() ?? "Ninguna",
                TotalDoctors = doctors, TotalPatients = patients, TotalAppointments = appointments,
                CreatedAt = c.CreatedAt
            });
        }

        return Ok(ApiResponse<PagedResult<CompanyAdminDto>>.Ok(new PagedResult<CompanyAdminDto>
        {
            Items = dtos, TotalCount = totalCount, Page = pagination.Page, PageSize = pagination.PageSize
        }));
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<SuperAdminStatsDto>>> GetStats()
    {
        var companies = (await _unitOfWork.Companies.GetAllAsync()).Count;
        var activeSubscriptions = (await _unitOfWork.CompanySubscriptions.FindAsync(
            s => s.Status == SubscriptionStatus.Active)).Count;
        var totalDoctors = (await _unitOfWork.Doctors.GetAllAsync()).Count;
        var totalPatients = (await _unitOfWork.Patients.GetAllAsync()).Count;
        var totalAppointments = (await _unitOfWork.Appointments.GetAllAsync()).Count;

        return Ok(ApiResponse<SuperAdminStatsDto>.Ok(new SuperAdminStatsDto
        {
            TotalCompanies = companies,
            ActiveSubscriptions = activeSubscriptions,
            TotalDoctors = totalDoctors,
            TotalPatients = totalPatients,
            TotalAppointments = totalAppointments
        }));
    }

    [HttpPost("companies/{id:guid}/toggle-active")]
    public async Task<ActionResult<ApiResponse<object>>> ToggleCompanyActive(Guid id)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id);
        if (company == null) return NotFound(ApiResponse<object>.Fail("Empresa no encontrada"));

        company.IsDeleted = !company.IsDeleted;
        company.DeletedAt = company.IsDeleted ? DateTime.UtcNow : null;
        _unitOfWork.Companies.Update(company);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null!, company.IsDeleted ? "Empresa desactivada" : "Empresa activada"));
    }
}

public class CompanyAdminDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public string SubscriptionStatus { get; set; } = string.Empty;
    public int TotalDoctors { get; set; }
    public int TotalPatients { get; set; }
    public int TotalAppointments { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class SuperAdminStatsDto
{
    public int TotalCompanies { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalPatients { get; set; }
    public int TotalAppointments { get; set; }
}
