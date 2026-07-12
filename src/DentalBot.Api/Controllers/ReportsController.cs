using DentalBot.Application.Interfaces;
using DentalBot.Domain.Enums;
using DentalBot.Shared.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ReportsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<ApiResponse<ReportsOverviewDto>>> GetOverview([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<ReportsOverviewDto>.Fail("CompanyId no encontrado"));

        var from = dateFrom ?? DateTime.UtcNow.Date.AddDays(-30);
        var to = dateTo ?? DateTime.UtcNow.Date.AddDays(1);

        var appointments = await _unitOfWork.Appointments.FindAsync(
            a => a.CompanyId == companyId.Value &&
                 a.AppointmentDate >= from &&
                 a.AppointmentDate < to &&
                 !a.IsDeleted);

        var patients = await _unitOfWork.Patients.FindAsync(p => p.CompanyId == companyId.Value && !p.IsDeleted);
        var services = await _unitOfWork.Services.FindAsync(s => s.CompanyId == companyId.Value && !s.IsDeleted);

        var totalRevenue = appointments
            .Where(a => a.Status == AppointmentStatus.Completed)
            .Sum(a =>
            {
                var service = services.FirstOrDefault(s => s.Id == a.ServiceId);
                return service?.Price ?? 0;
            });

        var overview = new ReportsOverviewDto
        {
            TotalAppointments = appointments.Count,
            CompletedAppointments = appointments.Count(a => a.Status == AppointmentStatus.Completed),
            CancelledAppointments = appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
            NoShowAppointments = appointments.Count(a => a.Status == AppointmentStatus.NoShow),
            TotalPatients = patients.Count,
            TotalRevenue = totalRevenue,
            DateFrom = from,
            DateTo = to
        };

        return Ok(ApiResponse<ReportsOverviewDto>.Ok(overview));
    }

    [HttpGet("appointments-by-service")]
    public async Task<ActionResult<ApiResponse<List<ServiceReportDto>>>> AppointmentsByService([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<List<ServiceReportDto>>.Fail("CompanyId no encontrado"));

        var from = dateFrom ?? DateTime.UtcNow.Date.AddDays(-30);
        var to = dateTo ?? DateTime.UtcNow.Date.AddDays(1);

        var appointments = await _unitOfWork.Appointments.FindAsync(
            a => a.CompanyId == companyId.Value && a.AppointmentDate >= from && a.AppointmentDate < to && !a.IsDeleted);
        var services = await _unitOfWork.Services.FindAsync(s => s.CompanyId == companyId.Value && !s.IsDeleted);

        var report = services.Select(s => new ServiceReportDto
        {
            ServiceId = s.Id,
            ServiceName = s.Name,
            TotalAppointments = appointments.Count(a => a.ServiceId == s.Id),
            CompletedAppointments = appointments.Count(a => a.ServiceId == s.Id && a.Status == AppointmentStatus.Completed),
            Revenue = appointments
                .Where(a => a.ServiceId == s.Id && a.Status == AppointmentStatus.Completed)
                .Sum(_ => s.Price)
        }).OrderByDescending(r => r.TotalAppointments).ToList();

        return Ok(ApiResponse<List<ServiceReportDto>>.Ok(report));
    }

    [HttpGet("appointments-by-doctor")]
    public async Task<ActionResult<ApiResponse<List<DoctorReportDto>>>> AppointmentsByDoctor([FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo)
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<List<DoctorReportDto>>.Fail("CompanyId no encontrado"));

        var from = dateFrom ?? DateTime.UtcNow.Date.AddDays(-30);
        var to = dateTo ?? DateTime.UtcNow.Date.AddDays(1);

        var appointments = await _unitOfWork.Appointments.FindAsync(
            a => a.CompanyId == companyId.Value && a.AppointmentDate >= from && a.AppointmentDate < to && !a.IsDeleted);
        var doctors = await _unitOfWork.Doctors.FindAsync(d => d.CompanyId == companyId.Value && !d.IsDeleted);

        var report = new List<DoctorReportDto>();
        foreach (var doctor in doctors)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(doctor.UserId);
            var doctorAppointments = appointments.Where(a => a.DoctorId == doctor.Id).ToList();

            report.Add(new DoctorReportDto
            {
                DoctorId = doctor.Id,
                DoctorName = user != null ? $"{user.FirstName} {user.LastName}" : "N/A",
                Specialty = doctor.Specialty,
                TotalAppointments = doctorAppointments.Count,
                CompletedAppointments = doctorAppointments.Count(a => a.Status == AppointmentStatus.Completed),
                NoShowAppointments = doctorAppointments.Count(a => a.Status == AppointmentStatus.NoShow)
            });
        }

        return Ok(ApiResponse<List<DoctorReportDto>>.Ok(report.OrderByDescending(r => r.TotalAppointments).ToList()));
    }

    [HttpGet("monthly-trend")]
    public async Task<ActionResult<ApiResponse<List<MonthlyTrendDto>>>> MonthlyTrend([FromQuery] int months = 6)
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<List<MonthlyTrendDto>>.Fail("CompanyId no encontrado"));

        var startDate = DateTime.UtcNow.Date.AddMonths(-months);

        var appointments = await _unitOfWork.Appointments.FindAsync(
            a => a.CompanyId == companyId.Value && a.AppointmentDate >= startDate && !a.IsDeleted);

        var services = await _unitOfWork.Services.FindAsync(s => s.CompanyId == companyId.Value && !s.IsDeleted);

        var trend = new List<MonthlyTrendDto>();
        for (var i = months - 1; i >= 0; i--)
        {
            var monthStart = DateTime.UtcNow.Date.AddMonths(-i).AddDays(1 - DateTime.UtcNow.Date.Day);
            var monthEnd = monthStart.AddMonths(1);

            var monthAppointments = appointments.Where(a =>
                a.AppointmentDate >= monthStart && a.AppointmentDate < monthEnd).ToList();

            var revenue = monthAppointments
                .Where(a => a.Status == AppointmentStatus.Completed)
                .Sum(a =>
                {
                    var service = services.FirstOrDefault(s => s.Id == a.ServiceId);
                    return service?.Price ?? 0;
                });

            trend.Add(new MonthlyTrendDto
            {
                Month = monthStart.ToString("MMM yyyy"),
                TotalAppointments = monthAppointments.Count,
                CompletedAppointments = monthAppointments.Count(a => a.Status == AppointmentStatus.Completed),
                Revenue = revenue
            });
        }

        return Ok(ApiResponse<List<MonthlyTrendDto>>.Ok(trend));
    }

    [HttpGet("patient-retention")]
    public async Task<ActionResult<ApiResponse<PatientRetentionDto>>> PatientRetention()
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<PatientRetentionDto>.Fail("CompanyId no encontrado"));

        var patients = await _unitOfWork.Patients.FindAsync(p => p.CompanyId == companyId.Value && !p.IsDeleted);
        var appointments = await _unitOfWork.Appointments.FindAsync(
            a => a.CompanyId == companyId.Value && !a.IsDeleted);

        var totalPatients = patients.Count;
        var patientsWithAppointments = patients.Count(p =>
            appointments.Any(a => a.PatientId == p.Id));
        var returningPatients = patients.Count(p =>
            appointments.Count(a => a.PatientId == p.Id) > 1);

        var retention = new PatientRetentionDto
        {
            TotalPatients = totalPatients,
            PatientsWithAppointments = patientsWithAppointments,
            ReturningPatients = returningPatients,
            FirstTimePatients = patientsWithAppointments - returningPatients,
            RetentionRate = totalPatients > 0 ? Math.Round((double)returningPatients / totalPatients * 100, 1) : 0
        };

        return Ok(ApiResponse<PatientRetentionDto>.Ok(retention));
    }

    private Guid? GetCompanyId()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == "companyId");
        if (claim != null && Guid.TryParse(claim.Value, out var companyId))
            return companyId;
        return null;
    }
}

public class ReportsOverviewDto
{
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int NoShowAppointments { get; set; }
    public int TotalPatients { get; set; }
    public decimal TotalRevenue { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}

public class ServiceReportDto
{
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public decimal Revenue { get; set; }
}

public class DoctorReportDto
{
    public Guid DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public int NoShowAppointments { get; set; }
}

public class MonthlyTrendDto
{
    public string Month { get; set; } = string.Empty;
    public int TotalAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public decimal Revenue { get; set; }
}

public class PatientRetentionDto
{
    public int TotalPatients { get; set; }
    public int PatientsWithAppointments { get; set; }
    public int ReturningPatients { get; set; }
    public int FirstTimePatients { get; set; }
    public double RetentionRate { get; set; }
}
