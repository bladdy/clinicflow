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
public class CompaniesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public CompaniesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<CompanyDto>>>> GetAll([FromQuery] PaginationQuery query)
    {
        var (items, totalCount) = await _unitOfWork.Companies.GetPagedAsync(
            query.Page, query.PageSize,
            filter: c => !c.IsDeleted,
            orderBy: q => q.OrderBy(c => c.Name));

        var dtos = items.Select(c => new CompanyDto
        {
            Id = c.Id,
            Name = c.Name,
            Phone = c.Phone,
            Email = c.Email,
            Address = c.Address,
            LogoUrl = c.LogoUrl,
            Website = c.Website,
            TaxId = c.TaxId
        }).ToList();

        var result = new PagedResult<CompanyDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };

        return Ok(ApiResponse<PagedResult<CompanyDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> GetById(Guid id)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id);
        if (company == null || company.IsDeleted)
            return NotFound(ApiResponse<CompanyDto>.Fail("Empresa no encontrada"));

        var dto = new CompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            Phone = company.Phone,
            Email = company.Email,
            Address = company.Address,
            LogoUrl = company.LogoUrl,
            Website = company.Website,
            TaxId = company.TaxId
        };

        return Ok(ApiResponse<CompanyDto>.Ok(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> Create([FromBody] CreateCompanyRequest request)
    {
        var company = new Company
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            LogoUrl = request.LogoUrl,
            Website = request.Website,
            TaxId = request.TaxId
        };

        await _unitOfWork.Companies.AddAsync(company);
        await _unitOfWork.SaveChangesAsync();

        var dto = new CompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            Phone = company.Phone,
            Email = company.Email,
            Address = company.Address,
            LogoUrl = company.LogoUrl,
            Website = company.Website,
            TaxId = company.TaxId
        };

        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<CompanyDto>.Ok(dto, "Empresa creada exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<CompanyDto>>> Update(Guid id, [FromBody] UpdateCompanyRequest request)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id);
        if (company == null || company.IsDeleted)
            return NotFound(ApiResponse<CompanyDto>.Fail("Empresa no encontrada"));

        company.Name = request.Name;
        company.Phone = request.Phone;
        company.Email = request.Email;
        company.Address = request.Address;
        company.LogoUrl = request.LogoUrl;
        company.Website = request.Website;
        company.TaxId = request.TaxId;
        company.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Companies.Update(company);
        await _unitOfWork.SaveChangesAsync();

        var dto = new CompanyDto
        {
            Id = company.Id,
            Name = company.Name,
            Phone = company.Phone,
            Email = company.Email,
            Address = company.Address,
            LogoUrl = company.LogoUrl,
            Website = company.Website,
            TaxId = company.TaxId
        };

        return Ok(ApiResponse<CompanyDto>.Ok(dto, "Empresa actualizada exitosamente"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(id);
        if (company == null || company.IsDeleted)
            return NotFound(ApiResponse<object>.Fail("Empresa no encontrada"));

        _unitOfWork.Companies.SoftDelete(company);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null!, "Empresa eliminada exitosamente"));
    }
}
