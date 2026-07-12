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
public class DoctorsController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public DoctorsController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<DoctorDto>>>> GetAll([FromQuery] PaginationQuery query)
    {
        var (items, totalCount) = await _unitOfWork.Doctors.GetPagedAsync(
            query.Page, query.PageSize,
            filter: d => !d.IsDeleted,
            orderBy: q => q.OrderBy(d => d.User.LastName).ThenBy(d => d.User.FirstName));

        var dtos = new List<DoctorDto>();
        foreach (var d in items)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(d.UserId);
            dtos.Add(new DoctorDto
            {
                Id = d.Id,
                UserId = d.UserId,
                CompanyId = d.CompanyId,
                Specialty = d.Specialty,
                LicenseNumber = d.LicenseNumber,
                Bio = d.Bio,
                PhotoUrl = d.PhotoUrl,
                Color = d.Color,
                FullName = user != null ? $"{user.FirstName} {user.LastName}" : null,
                Email = user?.Email
            });
        }

        var result = new PagedResult<DoctorDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };

        return Ok(ApiResponse<PagedResult<DoctorDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> GetById(Guid id)
    {
        var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);
        if (doctor == null || doctor.IsDeleted)
            return NotFound(ApiResponse<DoctorDto>.Fail("Doctor no encontrado"));

        var user = await _unitOfWork.Users.GetByIdAsync(doctor.UserId);
        var dto = new DoctorDto
        {
            Id = doctor.Id,
            UserId = doctor.UserId,
            CompanyId = doctor.CompanyId,
            Specialty = doctor.Specialty,
            LicenseNumber = doctor.LicenseNumber,
            Bio = doctor.Bio,
            PhotoUrl = doctor.PhotoUrl,
            Color = doctor.Color,
            FullName = user != null ? $"{user.FirstName} {user.LastName}" : null,
            Email = user?.Email
        };

        return Ok(ApiResponse<DoctorDto>.Ok(dto));
    }

    [HttpGet("by-company/{companyId:guid}")]
    public async Task<ActionResult<ApiResponse<List<DoctorDto>>>> GetByCompany(Guid companyId)
    {
        var doctors = await _unitOfWork.Doctors.FindAsync(d => d.CompanyId == companyId && !d.IsDeleted);
        var dtos = new List<DoctorDto>();

        foreach (var d in doctors)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(d.UserId);
            dtos.Add(new DoctorDto
            {
                Id = d.Id,
                UserId = d.UserId,
                CompanyId = d.CompanyId,
                Specialty = d.Specialty,
                LicenseNumber = d.LicenseNumber,
                Bio = d.Bio,
                PhotoUrl = d.PhotoUrl,
                Color = d.Color,
                FullName = user != null ? $"{user.FirstName} {user.LastName}" : null,
                Email = user?.Email
            });
        }

        return Ok(ApiResponse<List<DoctorDto>>.Ok(dtos));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> Create([FromBody] CreateDoctorRequest request)
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<DoctorDto>.Fail("CompanyId no encontrado en el token"));

        var company = await _unitOfWork.Companies.GetByIdAsync(companyId.Value);
        if (company == null || company.IsDeleted)
            return BadRequest(ApiResponse<DoctorDto>.Fail("Empresa no encontrada"));

        var existingUser = (await _unitOfWork.Users.FindAsync(u => u.Email == request.Email && !u.IsDeleted)).FirstOrDefault();
        if (existingUser != null)
            return BadRequest(ApiResponse<DoctorDto>.Fail("Ya existe un usuario con ese correo electrónico"));

        var doctorRole = (await _unitOfWork.Roles.FindAsync(r => r.Name == RoleName.Doctor)).FirstOrDefault();
        if (doctorRole == null)
            return BadRequest(ApiResponse<DoctorDto>.Fail("Rol de doctor no encontrado"));

        var user = new User
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            RoleId = doctorRole.Id,
            CompanyId = companyId.Value,
            IsActive = true,
            PasswordHash = string.Empty
        };

        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        var doctor = new Doctor
        {
            UserId = user.Id,
            CompanyId = companyId.Value,
            Specialty = request.Specialty,
            LicenseNumber = request.LicenseNumber,
            Bio = request.Bio,
            PhotoUrl = request.PhotoUrl,
            Color = request.Color
        };

        await _unitOfWork.Doctors.AddAsync(doctor);
        await _unitOfWork.SaveChangesAsync();

        var dto = new DoctorDto
        {
            Id = doctor.Id,
            UserId = doctor.UserId,
            CompanyId = doctor.CompanyId,
            Specialty = doctor.Specialty,
            LicenseNumber = doctor.LicenseNumber,
            Bio = doctor.Bio,
            PhotoUrl = doctor.PhotoUrl,
            Color = doctor.Color,
            FullName = $"{user.FirstName} {user.LastName}",
            Email = user.Email
        };

        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<DoctorDto>.Ok(dto, "Doctor creado exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<DoctorDto>>> Update(Guid id, [FromBody] UpdateDoctorRequest request)
    {
        var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);
        if (doctor == null || doctor.IsDeleted)
            return NotFound(ApiResponse<DoctorDto>.Fail("Doctor no encontrado"));

        doctor.Specialty = request.Specialty;
        doctor.LicenseNumber = request.LicenseNumber;
        doctor.Bio = request.Bio;
        doctor.PhotoUrl = request.PhotoUrl;
        doctor.Color = request.Color;
        doctor.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Doctors.Update(doctor);
        await _unitOfWork.SaveChangesAsync();

        var user = await _unitOfWork.Users.GetByIdAsync(doctor.UserId);
        var dto = new DoctorDto
        {
            Id = doctor.Id,
            UserId = doctor.UserId,
            CompanyId = doctor.CompanyId,
            Specialty = doctor.Specialty,
            LicenseNumber = doctor.LicenseNumber,
            Bio = doctor.Bio,
            PhotoUrl = doctor.PhotoUrl,
            Color = doctor.Color,
            FullName = user != null ? $"{user.FirstName} {user.LastName}" : null,
            Email = user?.Email
        };

        return Ok(ApiResponse<DoctorDto>.Ok(dto, "Doctor actualizado exitosamente"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var doctor = await _unitOfWork.Doctors.GetByIdAsync(id);
        if (doctor == null || doctor.IsDeleted)
            return NotFound(ApiResponse<object>.Fail("Doctor no encontrado"));

        _unitOfWork.Doctors.SoftDelete(doctor);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null!, "Doctor eliminado exitosamente"));
    }

    private Guid? GetCompanyId()
    {
        var claim = User.FindFirst("companyId")?.Value;
        return Guid.TryParse(claim, out var companyId) ? companyId : null;
    }
}
