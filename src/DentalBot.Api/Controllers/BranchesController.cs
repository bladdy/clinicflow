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
public class BranchesController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public BranchesController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<BranchDto>>>> GetAll([FromQuery] PaginationQuery query)
    {
        var (items, totalCount) = await _unitOfWork.Branches.GetPagedAsync(
            query.Page, query.PageSize,
            filter: b => !b.IsDeleted,
            orderBy: q => q.OrderBy(b => b.Name));

        var dtos = new List<BranchDto>();
        foreach (var b in items)
        {
            var company = await _unitOfWork.Companies.GetByIdAsync(b.CompanyId);
            dtos.Add(new BranchDto
            {
                Id = b.Id,
                CompanyId = b.CompanyId,
                Name = b.Name,
                Phone = b.Phone,
                Email = b.Email,
                Address = b.Address,
                IsMain = b.IsMain,
                CompanyName = company?.Name
            });
        }

        var result = new PagedResult<BranchDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };

        return Ok(ApiResponse<PagedResult<BranchDto>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BranchDto>>> GetById(Guid id)
    {
        var branch = await _unitOfWork.Branches.GetByIdAsync(id);
        if (branch == null || branch.IsDeleted)
            return NotFound(ApiResponse<BranchDto>.Fail("Sucursal no encontrada"));

        var company = await _unitOfWork.Companies.GetByIdAsync(branch.CompanyId);
        var dto = new BranchDto
        {
            Id = branch.Id,
            CompanyId = branch.CompanyId,
            Name = branch.Name,
            Phone = branch.Phone,
            Email = branch.Email,
            Address = branch.Address,
            IsMain = branch.IsMain,
            CompanyName = company?.Name
        };

        return Ok(ApiResponse<BranchDto>.Ok(dto));
    }

    [HttpGet("by-company/{companyId:guid}")]
    public async Task<ActionResult<ApiResponse<List<BranchDto>>>> GetByCompany(Guid companyId)
    {
        var branches = await _unitOfWork.Branches.FindAsync(b => b.CompanyId == companyId && !b.IsDeleted);
        var company = await _unitOfWork.Companies.GetByIdAsync(companyId);

        var dtos = branches.Select(b => new BranchDto
        {
            Id = b.Id,
            CompanyId = b.CompanyId,
            Name = b.Name,
            Phone = b.Phone,
            Email = b.Email,
            Address = b.Address,
            IsMain = b.IsMain,
            CompanyName = company?.Name
        }).ToList();

        return Ok(ApiResponse<List<BranchDto>>.Ok(dtos));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BranchDto>>> Create([FromBody] CreateBranchRequest request)
    {
        var company = await _unitOfWork.Companies.GetByIdAsync(request.CompanyId);
        if (company == null || company.IsDeleted)
            return BadRequest(ApiResponse<BranchDto>.Fail("Empresa no encontrada"));

        var branch = new Branch
        {
            CompanyId = request.CompanyId,
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            Address = request.Address,
            IsMain = request.IsMain
        };

        await _unitOfWork.Branches.AddAsync(branch);
        await _unitOfWork.SaveChangesAsync();

        var dto = new BranchDto
        {
            Id = branch.Id,
            CompanyId = branch.CompanyId,
            Name = branch.Name,
            Phone = branch.Phone,
            Email = branch.Email,
            Address = branch.Address,
            IsMain = branch.IsMain,
            CompanyName = company.Name
        };

        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, ApiResponse<BranchDto>.Ok(dto, "Sucursal creada exitosamente"));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<BranchDto>>> Update(Guid id, [FromBody] UpdateBranchRequest request)
    {
        var branch = await _unitOfWork.Branches.GetByIdAsync(id);
        if (branch == null || branch.IsDeleted)
            return NotFound(ApiResponse<BranchDto>.Fail("Sucursal no encontrada"));

        branch.Name = request.Name;
        branch.Phone = request.Phone;
        branch.Email = request.Email;
        branch.Address = request.Address;
        branch.IsMain = request.IsMain;
        branch.UpdatedAt = DateTime.UtcNow;

        _unitOfWork.Branches.Update(branch);
        await _unitOfWork.SaveChangesAsync();

        var company = await _unitOfWork.Companies.GetByIdAsync(branch.CompanyId);
        var dto = new BranchDto
        {
            Id = branch.Id,
            CompanyId = branch.CompanyId,
            Name = branch.Name,
            Phone = branch.Phone,
            Email = branch.Email,
            Address = branch.Address,
            IsMain = branch.IsMain,
            CompanyName = company?.Name
        };

        return Ok(ApiResponse<BranchDto>.Ok(dto, "Sucursal actualizada exitosamente"));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<object>>> Delete(Guid id)
    {
        var branch = await _unitOfWork.Branches.GetByIdAsync(id);
        if (branch == null || branch.IsDeleted)
            return NotFound(ApiResponse<object>.Fail("Sucursal no encontrada"));

        _unitOfWork.Branches.SoftDelete(branch);
        await _unitOfWork.SaveChangesAsync();

        return Ok(ApiResponse<object>.Ok(null!, "Sucursal eliminada exitosamente"));
    }
}
