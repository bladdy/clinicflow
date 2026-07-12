using System.Security.Claims;
using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using DentalBot.Shared.DTOs.Clinics;
using DentalBot.Shared.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public ServicesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<ServiceDto>>>> GetAll([FromQuery] PaginationQuery query)
    {
        var (items, totalCount) = await _unitOfWork.Services.GetPagedAsync(
            query.Page, query.PageSize,
            filter: s => !s.IsDeleted,
            orderBy: q => q.OrderBy(s => s.Name));

        var dtos = items.Select(s => new ServiceDto
        {
            Id = s.Id,
            CompanyId = s.CompanyId,
            Name = s.Name,
            Description = s.Description,
            DurationMinutes = s.DurationMinutes,
            Price = s.Price,
            Category = s.Category,
            IsActive = s.IsActive
        }).ToList();

        var result = new PagedResult<ServiceDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };

        return Ok(ApiResponse<PagedResult<ServiceDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> GetById(Guid id)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(id);
        if (service == null || service.IsDeleted)
            return NotFound(ApiResponse<ServiceDto>.Fail("Servicio no encontrado"));

        var dto = new ServiceDto
        {
            Id = service.Id,
            CompanyId = service.CompanyId,
            Name = service.Name,
            Description = service.Description,
            DurationMinutes = service.DurationMinutes,
            Price = service.Price,
            Category = service.Category,
            IsActive = service.IsActive
        };

        return Ok(ApiResponse<ServiceDto>.Ok(dto));
    }

    [HttpGet("by-company/{companyId:guid}")]
    public async Task<ActionResult<ApiResponse<List<ServiceDto>>>> GetByCompany(Guid companyId)
    {
        var services = await _unitOfWork.Services.FindAsync(s => s.CompanyId == companyId && !s.IsDeleted);

        var dtos = services.Select(s => new ServiceDto
        {
            Id = s.Id,
            CompanyId = s.CompanyId,
            Name = s.Name,
            Description = s.Description,
            DurationMinutes = s.DurationMinutes,
            Price = s.Price,
            Category = s.Category,
            IsActive = s.IsActive
        }).ToList();

        return Ok(ApiResponse<List<ServiceDto>>.Ok(dtos));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> Create([FromBody] CreateServiceRequest request)
    {
        var companyId = GetCompanyId();
        if (companyId == null)
            return BadRequest(ApiResponse<ServiceDto>.Fail("CompanyId no encontrado en el token"));

        var service = new Service
        {
            CompanyId = companyId.Value,
            Name = request.Name,
            Description = request.Description,
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            Category = request.Category,
            IsActive = true
        };

        await _unitOfWork.Services.AddAsync(service);
        await _unitOfWork.SaveChangesAsync();

        var dto = new ServiceDto
        {
            Id = service.Id,
            CompanyId = service.CompanyId,
            Name = service.Name,
            Description = service.Description,
            DurationMinutes = service.DurationMinutes,
            Price = service.Price,
            Category = service.Category,
            IsActive = service.IsActive
        };

        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<ServiceDto>.Ok(dto, "Servicio creado exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<ServiceDto>>> Update(Guid id, [FromBody] UpdateServiceRequest request)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(id);
        if (service == null || service.IsDeleted)
            return NotFound(ApiResponse<ServiceDto>.Fail("Servicio no encontrado"));

        service.Name = request.Name;
        service.Description = request.Description;
        service.DurationMinutes = request.DurationMinutes;
        service.Price = request.Price;
        service.Category = request.Category;
        service.IsActive = request.IsActive;
        service.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Services.Update(service);
        await _unitOfWork.SaveChangesAsync();

        var dto = new ServiceDto
        {
            Id = service.Id,
            CompanyId = service.CompanyId,
            Name = service.Name,
            Description = service.Description,
            DurationMinutes = service.DurationMinutes,
            Price = service.Price,
            Category = service.Category,
            IsActive = service.IsActive
        };

        return Ok(ApiResponse<ServiceDto>.Ok(dto, "Servicio actualizado exitosamente"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var service = await _unitOfWork.Services.GetByIdAsync(id);
        if (service == null || service.IsDeleted)
            return NotFound(ApiResponse<object>.Fail("Servicio no encontrado"));

        _unitOfWork.Services.SoftDelete(service);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null!, "Servicio eliminado exitosamente"));
    }

    private Guid? GetCompanyId()
    {
        var claim = User.FindFirst("companyId")?.Value;
        return Guid.TryParse(claim, out var companyId) ? companyId : null;
    }
}
