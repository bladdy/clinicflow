using System.Security.Claims;
using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using DentalBot.Domain.Enums;
using DentalBot.Shared.DTOs.Clinics;
using DentalBot.Shared.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PatientsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public PatientsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<PatientDto>>>> GetAll([FromQuery] PaginationQuery query)
    {
        var (items, totalCount) = await _unitOfWork.Patients.GetPagedAsync(
            query.Page, query.PageSize,
            filter: p => !p.IsDeleted,
            orderBy: q => q.OrderBy(p => p.LastName).ThenBy(p => p.FirstName));

        var dtos = items.Select(MapToDto).ToList();

        var result = new PagedResult<PatientDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };

        return Ok(ApiResponse<PagedResult<PatientDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> GetById(Guid id)
    {
        var patient = await _unitOfWork.Patients.GetByIdAsync(id);
        if (patient == null || patient.IsDeleted)
            return NotFound(ApiResponse<PatientDto>.Fail("Paciente no encontrado"));

        return Ok(ApiResponse<PatientDto>.Ok(MapToDto(patient)));
    }

    [HttpGet("by-company/{companyId:guid}")]
    public async Task<ActionResult<ApiResponse<List<PatientDto>>>> GetByCompany(Guid companyId)
    {
        var patients = await _unitOfWork.Patients.FindAsync(p => p.CompanyId == companyId && !p.IsDeleted);
        var dtos = patients.Select(MapToDto).ToList();

        return Ok(ApiResponse<List<PatientDto>>.Ok(dtos));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PatientDto>>> Create([FromBody] CreatePatientRequest request)
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<PatientDto>.Fail("CompanyId no encontrado en el token"));

        var patient = new Patient
        {
            CompanyId = companyId.Value,
            BranchId = request.BranchId,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            DateOfBirth = request.DateOfBirth,
            Gender = ParseGender(request.Gender),
            Address = request.Address,
            Notes = request.Notes,
            MedicalHistory = request.MedicalHistory
        };

        await _unitOfWork.Patients.AddAsync(patient);
        await _unitOfWork.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = patient.Id }, ApiResponse<PatientDto>.Ok(MapToDto(patient), "Paciente creado exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<PatientDto>>> Update(Guid id, [FromBody] UpdatePatientRequest request)
    {
        var patient = await _unitOfWork.Patients.GetByIdAsync(id);
        if (patient == null || patient.IsDeleted)
            return NotFound(ApiResponse<PatientDto>.Fail("Paciente no encontrado"));

        patient.FirstName = request.FirstName;
        patient.LastName = request.LastName;
        patient.Email = request.Email;
        patient.Phone = request.Phone;
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = ParseGender(request.Gender);
        patient.Address = request.Address;
        patient.Notes = request.Notes;
        patient.MedicalHistory = request.MedicalHistory;
        patient.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Patients.Update(patient);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<PatientDto>.Ok(MapToDto(patient), "Paciente actualizado exitosamente"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var patient = await _unitOfWork.Patients.GetByIdAsync(id);
        if (patient == null || patient.IsDeleted)
            return NotFound(ApiResponse<object>.Fail("Paciente no encontrado"));

        _unitOfWork.Patients.SoftDelete(patient);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null!, "Paciente eliminado exitosamente"));
    }

    private static PatientDto MapToDto(Patient p) => new()
    {
        Id = p.Id,
        CompanyId = p.CompanyId,
        BranchId = p.BranchId,
        FirstName = p.FirstName,
        LastName = p.LastName,
        Email = p.Email,
        Phone = p.Phone,
        DateOfBirth = p.DateOfBirth,
        Gender = p.Gender?.ToString(),
        Address = p.Address,
        Notes = p.Notes,
        MedicalHistory = p.MedicalHistory
    };

    private static Gender? ParseGender(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;
        return Enum.TryParse<Gender>(value, true, out var gender) ? gender : null;
    }

    private Guid? GetCompanyId()
    {
        var claim = User.FindFirst("companyId")?.Value;
        return Guid.TryParse(claim, out var companyId) ? companyId : null;
    }
}
