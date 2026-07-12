using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using DentalBot.Domain.Enums;
using DentalBot.Shared.DTOs.Appointments;
using DentalBot.Shared.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DentalBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public AppointmentsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<AppointmentDto>>>> GetAll(
        [FromQuery] PaginationQuery pagination,
        [FromQuery] Guid? doctorId, [FromQuery] Guid? patientId, [FromQuery] Guid? serviceId,
        [FromQuery] string? doctorName, [FromQuery] string? patientName, [FromQuery] string? serviceName,
        [FromQuery] DateTime? dateFrom, [FromQuery] DateTime? dateTo, [FromQuery] string? status)
    {
        var hasNameSearch = !string.IsNullOrWhiteSpace(patientName) ||
                            !string.IsNullOrWhiteSpace(doctorName) ||
                            !string.IsNullOrWhiteSpace(serviceName);

        if (hasNameSearch)
        {
            return await GetAllWithTextSearch(pagination, doctorId, patientId, serviceId,
                patientName, doctorName, serviceName, dateFrom, dateTo, status);
        }

        var (items, totalCount) = await _unitOfWork.Appointments.GetPagedAsync(
            pagination.Page,
            pagination.PageSize,
            filter: a =>
                (!doctorId.HasValue || a.DoctorId == doctorId.Value) &&
                (!patientId.HasValue || a.PatientId == patientId.Value) &&
                (!serviceId.HasValue || a.ServiceId == serviceId.Value) &&
                (!dateFrom.HasValue || a.AppointmentDate >= dateFrom.Value) &&
                (!dateTo.HasValue || a.AppointmentDate <= dateTo.Value) &&
                (string.IsNullOrEmpty(status) || a.Status.ToString() == status),
            orderBy: q => q.OrderByDescending(a => a.AppointmentDate)
        );

        var enriched = await EnrichAppointments(items);
        enriched = enriched.OrderBy(a => a.StartTime).ToList();
        var result = new PagedResult<AppointmentDto>
        {
            Items = enriched,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        };

        return Ok(ApiResponse<PagedResult<AppointmentDto>>.Ok(result));
    }

    [HttpGet("range")]
    public async Task<ActionResult<ApiResponse<List<AppointmentDto>>>> GetRange(
        [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] Guid? doctorId, [FromQuery] Guid? serviceId, [FromQuery] string? status)
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<List<AppointmentDto>>.Fail("No se encontró la empresa en el token"));

        var items = await _unitOfWork.Appointments.FindAsync(
            a => a.CompanyId == companyId.Value &&
                 a.AppointmentDate >= from &&
                 a.AppointmentDate <= to &&
                 (!doctorId.HasValue || a.DoctorId == doctorId.Value) &&
                 (!serviceId.HasValue || a.ServiceId == serviceId.Value) &&
                 (string.IsNullOrEmpty(status) || a.Status.ToString() == status));

        var enriched = await EnrichAppointments(items);
        enriched = enriched.OrderBy(a => a.AppointmentDate).ThenBy(a => a.StartTime).ToList();

        return Ok(ApiResponse<List<AppointmentDto>>.Ok(enriched));
    }

    private async Task<ActionResult<ApiResponse<PagedResult<AppointmentDto>>>> GetAllWithTextSearch(
        PaginationQuery pagination, Guid? doctorId, Guid? patientId, Guid? serviceId,
        string? patientName, string? doctorName, string? serviceName,
        DateTime? dateFrom, DateTime? dateTo, string? status)
    {
        List<Guid>? filterPatientIds = null;
        if (!string.IsNullOrWhiteSpace(patientName))
        {
            var matching = await _unitOfWork.Patients.FindAsync(
                p => p.FirstName.Contains(patientName) || p.LastName.Contains(patientName));
            filterPatientIds = matching.Select(p => p.Id).ToList();
            if (filterPatientIds.Count == 0)
                return Ok(ApiResponse<PagedResult<AppointmentDto>>.Ok(EmptyResult(pagination)));
        }

        List<Guid>? filterDoctorIds = null;
        if (!string.IsNullOrWhiteSpace(doctorName))
        {
            var users = await _unitOfWork.Users.FindAsync(
                u => (u.FirstName.Contains(doctorName) || u.LastName.Contains(doctorName)) && !u.IsDeleted);
            var userIds = users.Select(u => u.Id).ToList();
            var doctors = await _unitOfWork.Doctors.FindAsync(d => userIds.Contains(d.UserId));
            filterDoctorIds = doctors.Select(d => d.Id).ToList();
            if (filterDoctorIds.Count == 0)
                return Ok(ApiResponse<PagedResult<AppointmentDto>>.Ok(EmptyResult(pagination)));
        }

        List<Guid>? filterServiceIds = null;
        if (!string.IsNullOrWhiteSpace(serviceName))
        {
            var matching = await _unitOfWork.Services.FindAsync(s => s.Name.Contains(serviceName));
            filterServiceIds = matching.Select(s => s.Id).ToList();
            if (filterServiceIds.Count == 0)
                return Ok(ApiResponse<PagedResult<AppointmentDto>>.Ok(EmptyResult(pagination)));
        }

        var all = await _unitOfWork.Appointments.FindAsync(a =>
            (filterDoctorIds == null || filterDoctorIds.Contains(a.DoctorId)) &&
            (filterPatientIds == null || filterPatientIds.Contains(a.PatientId)) &&
            (filterServiceIds == null || filterServiceIds.Contains(a.ServiceId)) &&
            (!doctorId.HasValue || a.DoctorId == doctorId.Value) &&
            (!patientId.HasValue || a.PatientId == patientId.Value) &&
            (!serviceId.HasValue || a.ServiceId == serviceId.Value) &&
            (!dateFrom.HasValue || a.AppointmentDate >= dateFrom.Value) &&
            (!dateTo.HasValue || a.AppointmentDate <= dateTo.Value) &&
            (string.IsNullOrEmpty(status) || a.Status.ToString() == status));

        var ordered = all.OrderByDescending(a => a.AppointmentDate).ToList();
        var totalCount = ordered.Count;
        var paged = ordered.Skip((pagination.Page - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();
        var enriched = await EnrichAppointments(paged);
        enriched = enriched.OrderBy(a => a.StartTime).ToList();

        return Ok(ApiResponse<PagedResult<AppointmentDto>>.Ok(new PagedResult<AppointmentDto>
        {
            Items = enriched,
            TotalCount = totalCount,
            Page = pagination.Page,
            PageSize = pagination.PageSize
        }));
    }

    private static PagedResult<AppointmentDto> EmptyResult(PaginationQuery pagination) => new()
    {
        Items = [],
        TotalCount = 0,
        Page = pagination.Page,
        PageSize = pagination.PageSize
    };

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> GetById(Guid id)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
        if (appointment == null)
            return NotFound(ApiResponse<AppointmentDto>.Fail("Cita no encontrada"));

        var dto = (await EnrichAppointments(new[] { appointment })).First();
        return Ok(ApiResponse<AppointmentDto>.Ok(dto));
    }

    [HttpGet("available-slots")]
    public async Task<ActionResult<ApiResponse<List<AvailableSlotDto>>>> GetAvailableSlots([FromQuery] Guid doctorId, [FromQuery] DateTime date, [FromQuery] Guid? excludeAppointmentId)
    {
        var dayOfWeek = (DentalBot.Domain.Enums.DayOfWeek)(int)date.DayOfWeek;

        var businessHours = (await _unitOfWork.BusinessHours.FindAsync(
            bh => bh.DoctorId == doctorId && bh.DayOfWeek == dayOfWeek && !bh.IsDeleted)).FirstOrDefault();

        if (businessHours == null || businessHours.IsClosed)
            return Ok(ApiResponse<List<AvailableSlotDto>>.Ok(new List<AvailableSlotDto>()));

        var existingAppointments = await _unitOfWork.Appointments.FindAsync(
            a => a.DoctorId == doctorId &&
                 a.AppointmentDate.Date == date.Date &&
                 a.Status != AppointmentStatus.Cancelled &&
                 a.Status != AppointmentStatus.NoShow &&
                 (!excludeAppointmentId.HasValue || a.Id != excludeAppointmentId.Value));

        var slots = new List<AvailableSlotDto>();
        var current = businessHours.OpenTime;
        var slotDuration = TimeSpan.FromMinutes(30);

        while (current + slotDuration <= businessHours.CloseTime)
        {
            var end = current + slotDuration;
            var isBooked = existingAppointments.Any(a =>
                a.StartTime < end && a.EndTime > current);

            slots.Add(new AvailableSlotDto
            {
                StartTime = current.ToString(@"hh\:mm"),
                EndTime = end.ToString(@"hh\:mm"),
                IsAvailable = !isBooked
            });

            current = end;
        }

        return Ok(ApiResponse<List<AvailableSlotDto>>.Ok(slots));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> Create([FromBody] CreateAppointmentRequest request)
    {
        var startTime = TimeSpan.Parse(request.StartTime);
        var endTime = TimeSpan.Parse(request.EndTime);

        if (startTime >= endTime)
            return BadRequest(ApiResponse<AppointmentDto>.Fail("La hora de fin debe ser posterior a la hora de inicio"));

        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<AppointmentDto>.Fail("CompanyId no encontrado en el token"));

        var branchId = request.BranchId ?? GetBranchId();
        if (branchId == null)
            return BadRequest(ApiResponse<AppointmentDto>.Fail("No se pudo determinar la sucursal"));

        var sameDayAppointments = await _unitOfWork.Appointments.FindAsync(
            a => a.DoctorId == request.DoctorId &&
                 a.AppointmentDate.Date == request.AppointmentDate.Date);

        var conflict = sameDayAppointments.Any(a =>
            a.Status != AppointmentStatus.Cancelled &&
            a.Status != AppointmentStatus.NoShow &&
            a.StartTime < endTime && a.EndTime > startTime);

        if (conflict)
            return BadRequest(ApiResponse<AppointmentDto>.Fail("El doctor ya tiene una cita en ese horario"));

        var appointment = new Appointment
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId.Value,
            BranchId = branchId.Value,
            DoctorId = request.DoctorId,
            PatientId = request.PatientId,
            ServiceId = request.ServiceId,
            AppointmentDate = request.AppointmentDate,
            StartTime = startTime,
            EndTime = endTime,
            Status = AppointmentStatus.Scheduled,
            Notes = request.Notes,
            Reason = request.Reason
        };

        await _unitOfWork.Appointments.AddAsync(appointment);
        await _unitOfWork.SaveChangesAsync();

        var dto = (await EnrichAppointments(new[] { appointment })).First();
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<AppointmentDto>.Ok(dto, "Cita creada exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<AppointmentDto>>> Update(Guid id, [FromBody] UpdateAppointmentRequest request)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
        if (appointment == null)
            return NotFound(ApiResponse<AppointmentDto>.Fail("Cita no encontrada"));

        if (request.BranchId.HasValue) appointment.BranchId = request.BranchId.Value;
        if (request.DoctorId.HasValue) appointment.DoctorId = request.DoctorId.Value;
        if (request.PatientId.HasValue) appointment.PatientId = request.PatientId.Value;
        if (request.ServiceId.HasValue) appointment.ServiceId = request.ServiceId.Value;
        if (request.AppointmentDate.HasValue) appointment.AppointmentDate = request.AppointmentDate.Value;
        if (!string.IsNullOrEmpty(request.StartTime)) appointment.StartTime = TimeSpan.Parse(request.StartTime);
        if (!string.IsNullOrEmpty(request.EndTime)) appointment.EndTime = TimeSpan.Parse(request.EndTime);
        if (request.Notes != null) appointment.Notes = request.Notes;
        if (request.Reason != null) appointment.Reason = request.Reason;

        if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<AppointmentStatus>(request.Status, out var newStatus))
            appointment.Status = newStatus;

        _unitOfWork.Appointments.Update(appointment);
        await _unitOfWork.SaveChangesAsync();

        var dto = (await EnrichAppointments(new[] { appointment })).First();
        return Ok(ApiResponse<AppointmentDto>.Ok(dto, "Cita actualizada exitosamente"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var appointment = await _unitOfWork.Appointments.GetByIdAsync(id);
        if (appointment == null)
            return NotFound(ApiResponse<object>.Fail("Cita no encontrada"));

        _unitOfWork.Appointments.SoftDelete(appointment);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null!, "Cita eliminada exitosamente"));
    }

    private async Task<List<AppointmentDto>> EnrichAppointments(IReadOnlyList<Appointment> appointments)
    {
        var result = new List<AppointmentDto>();
        foreach (var a in appointments)
        {
            var patient = await _unitOfWork.Patients.GetByIdAsync(a.PatientId);
            var doctor = await _unitOfWork.Doctors.GetByIdAsync(a.DoctorId);
            var service = await _unitOfWork.Services.GetByIdAsync(a.ServiceId);
            var branch = await _unitOfWork.Branches.GetByIdAsync(a.BranchId);

            string? doctorName = null;
            if (doctor != null)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(doctor.UserId);
                doctorName = user != null ? $"{user.FirstName} {user.LastName}" : null;
            }

            result.Add(new AppointmentDto
            {
                Id = a.Id,
                CompanyId = a.CompanyId,
                BranchId = a.BranchId,
                DoctorId = a.DoctorId,
                PatientId = a.PatientId,
                ServiceId = a.ServiceId,
                AppointmentDate = a.AppointmentDate,
                StartTime = a.StartTime,
                EndTime = a.EndTime,
                Status = a.Status.ToString(),
                Notes = a.Notes,
                Reason = a.Reason,
                PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : null,
                DoctorName = doctorName,
                ServiceName = service?.Name,
                BranchName = branch?.Name,
                DoctorColor = doctor?.Color
            });
        }
        return result;
    }

    private Guid? GetCompanyId()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == "companyId");
        if (claim != null && Guid.TryParse(claim.Value, out var companyId))
            return companyId;
        return null;
    }

    private Guid? GetBranchId()
    {
        var claim = User.Claims.FirstOrDefault(c => c.Type == "branchId");
        if (claim != null && Guid.TryParse(claim.Value, out var branchId))
            return branchId;
        return null;
    }
}