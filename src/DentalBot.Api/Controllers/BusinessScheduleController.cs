using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using DentalBot.Shared.DTOs.Clinics;
using DentalBot.Shared.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalBot.Api.Controllers;

[ApiController]
[Route("api/business-schedule")]
[Authorize]
public class BusinessScheduleController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public BusinessScheduleController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("{branchId:guid}")]
    public async Task<ActionResult<ApiResponse<BusinessScheduleDto>>> GetByBranch(Guid branchId)
    {
        var schedule = await GetOrCreateSchedule(branchId);
        var dto = MapToDto(schedule);
        return Ok(ApiResponse<BusinessScheduleDto>.Ok(dto));
    }

    [HttpPut("{branchId:guid}")]
    public async Task<ActionResult<ApiResponse<BusinessScheduleDto>>> Save(Guid branchId, [FromBody] SaveBusinessScheduleRequest request)
    {
        var schedule = await GetOrCreateSchedule(branchId);

        var existingDays = (await _unitOfWork.ScheduleDays.FindAsync(
            d => d.BusinessScheduleId == schedule.Id && !d.IsDeleted)).ToList();
        var existingBreaks = (await _unitOfWork.BreakPeriods.FindAsync(
            b => b.BusinessScheduleId == schedule.Id && !b.IsDeleted)).ToList();
        var existingLunch = (await _unitOfWork.LunchConfigs.FindAsync(
            l => l.BusinessScheduleId == schedule.Id && !l.IsDeleted)).FirstOrDefault();

        foreach (var dayEntry in request.Days)
        {
            var dayOfWeek = (Domain.Enums.DayOfWeek)dayEntry.DayOfWeek;
            var existing = existingDays.FirstOrDefault(d => d.DayOfWeek == dayOfWeek);

            if (existing != null)
            {
                existing.IsOpen = dayEntry.IsOpen;
                existing.OpenTime = ParseTimeSpan(dayEntry.OpenTime);
                existing.CloseTime = ParseTimeSpan(dayEntry.CloseTime);
                existing.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.ScheduleDays.Update(existing);
            }
            else
            {
                var newDay = new ScheduleDay
                {
                    BusinessScheduleId = schedule.Id,
                    DayOfWeek = dayOfWeek,
                    IsOpen = dayEntry.IsOpen,
                    OpenTime = ParseTimeSpan(dayEntry.OpenTime),
                    CloseTime = ParseTimeSpan(dayEntry.CloseTime)
                };
                await _unitOfWork.ScheduleDays.AddAsync(newDay);
            }
        }

        foreach (var existing in existingDays)
        {
            var stillPresent = request.Days.Any(d => (Domain.Enums.DayOfWeek)d.DayOfWeek == existing.DayOfWeek);
            if (!stillPresent)
                _unitOfWork.ScheduleDays.SoftDelete(existing);
        }

        foreach (var existing in existingBreaks)
            _unitOfWork.BreakPeriods.SoftDelete(existing);

        int sort = 0;
        foreach (var breakEntry in request.Breaks)
        {
            var newBreak = new BreakPeriod
            {
                BusinessScheduleId = schedule.Id,
                Name = breakEntry.Name,
                StartTime = ParseTimeSpan(breakEntry.StartTime),
                EndTime = ParseTimeSpan(breakEntry.EndTime),
                SortOrder = sort++
            };
            await _unitOfWork.BreakPeriods.AddAsync(newBreak);
        }

        if (existingLunch != null)
        {
            if (request.LunchConfig != null)
            {
                existingLunch.DurationMinutes = request.LunchConfig.DurationMinutes;
                existingLunch.StartTime = ParseTimeSpan(request.LunchConfig.StartTime);
                existingLunch.UpdatedAt = DateTime.UtcNow;
                _unitOfWork.LunchConfigs.Update(existingLunch);
            }
            else
            {
                _unitOfWork.LunchConfigs.SoftDelete(existingLunch);
            }
        }
        else if (request.LunchConfig != null)
        {
            var lunch = new LunchConfig
            {
                BusinessScheduleId = schedule.Id,
                DurationMinutes = request.LunchConfig.DurationMinutes,
                StartTime = ParseTimeSpan(request.LunchConfig.StartTime)
            };
            await _unitOfWork.LunchConfigs.AddAsync(lunch);
        }

        await _unitOfWork.SaveChangesAsync();

        var updated = await GetOrCreateSchedule(branchId);
        var dto = MapToDto(updated);
        return Ok(ApiResponse<BusinessScheduleDto>.Ok(dto, "Horarios guardados exitosamente"));
    }

    private async Task<BusinessSchedule> GetOrCreateSchedule(Guid branchId)
    {
        var existing = (await _unitOfWork.BusinessSchedules.FindAsync(
            s => s.BranchId == branchId && !s.IsDeleted)).FirstOrDefault();

        if (existing != null)
        {
            existing.Days = (await _unitOfWork.ScheduleDays.FindAsync(
                d => d.BusinessScheduleId == existing.Id && !d.IsDeleted)).ToList();
            existing.Breaks = (await _unitOfWork.BreakPeriods.FindAsync(
                b => b.BusinessScheduleId == existing.Id && !b.IsDeleted)).ToList();
            existing.LunchConfig = (await _unitOfWork.LunchConfigs.FindAsync(
                l => l.BusinessScheduleId == existing.Id && !l.IsDeleted)).FirstOrDefault();
            return existing;
        }

        var schedule = new BusinessSchedule { BranchId = branchId };
        await _unitOfWork.BusinessSchedules.AddAsync(schedule);
        await _unitOfWork.SaveChangesAsync();

        var days = Enum.GetValues<Domain.Enums.DayOfWeek>().Select(dow => new ScheduleDay
        {
            BusinessScheduleId = schedule.Id,
            DayOfWeek = dow,
            IsOpen = dow <= Domain.Enums.DayOfWeek.Viernes,
            OpenTime = new TimeSpan(9, 0, 0),
            CloseTime = new TimeSpan(17, 0, 0)
        }).ToList();

        foreach (var day in days)
            await _unitOfWork.ScheduleDays.AddAsync(day);

        await _unitOfWork.SaveChangesAsync();
        var created = (await _unitOfWork.BusinessSchedules.FindAsync(s => s.BranchId == branchId && !s.IsDeleted)).First();
        created.Days = (await _unitOfWork.ScheduleDays.FindAsync(d => d.BusinessScheduleId == created.Id && !d.IsDeleted)).ToList();
        created.Breaks = (await _unitOfWork.BreakPeriods.FindAsync(b => b.BusinessScheduleId == created.Id && !b.IsDeleted)).ToList();
        created.LunchConfig = (await _unitOfWork.LunchConfigs.FindAsync(l => l.BusinessScheduleId == created.Id && !l.IsDeleted)).FirstOrDefault();
        return created;
    }

    private static BusinessScheduleDto MapToDto(BusinessSchedule schedule)
    {
        var days = schedule.Days
            .Where(d => !d.IsDeleted)
            .OrderBy(d => d.DayOfWeek)
            .Select(d => new ScheduleDayDto
            {
                Id = d.Id,
                DayOfWeek = (int)d.DayOfWeek,
                DayName = d.DayOfWeek.ToString(),
                IsOpen = d.IsOpen,
                OpenTime = FormatTime(d.OpenTime),
                CloseTime = FormatTime(d.CloseTime)
            }).ToList();

        var breaks = schedule.Breaks
            .Where(b => !b.IsDeleted)
            .OrderBy(b => b.SortOrder)
            .Select(b => new BreakPeriodDto
            {
                Id = b.Id,
                Name = b.Name,
                StartTime = FormatTime(b.StartTime),
                EndTime = FormatTime(b.EndTime),
                DurationMinutes = (int)(b.EndTime - b.StartTime).TotalMinutes,
                SortOrder = b.SortOrder
            }).ToList();

        var lunch = schedule.LunchConfig;
        LunchConfigDto? lunchDto = null;
        if (lunch != null && !lunch.IsDeleted)
        {
            lunchDto = new LunchConfigDto
            {
                Id = lunch.Id,
                DurationMinutes = lunch.DurationMinutes,
                StartTime = FormatTime(lunch.StartTime),
                EndTime = FormatTime(lunch.EndTime)
            };
        }

        int totalMinutes = days.Where(d => d.IsOpen).Sum(d =>
        {
            var open = ParseTime(d.OpenTime);
            var close = ParseTime(d.CloseTime);
            return (int)(close - open).TotalMinutes;
        });

        int breakMinutes = breaks.Sum(b => b.DurationMinutes);
        if (lunchDto != null)
            breakMinutes += lunchDto.DurationMinutes;

        return new BusinessScheduleDto
        {
            Id = schedule.Id,
            BranchId = schedule.BranchId,
            Days = days,
            Breaks = breaks,
            LunchConfig = lunchDto,
            TotalProductiveMinutes = Math.Max(0, totalMinutes - breakMinutes)
        };
    }

    private static TimeSpan ParseTimeSpan(string time)
    {
        if (TimeSpan.TryParse(time, out var result))
            return result;

        var parts = time.Split(':');
        if (parts.Length == 2 && int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m))
            return new TimeSpan(h, m, 0);

        return new TimeSpan(9, 0, 0);
    }

    private static string FormatTime(TimeSpan time) =>
        $"{(int)time.TotalHours:D2}:{time.Minutes:D2}";

    private static TimeSpan ParseTime(string time) => ParseTimeSpan(time);
}
