using DentalBot.Application.Interfaces;
using DentalBot.Domain.Enums;
using DentalBot.Shared.DTOs.Common;
using DentalBot.Shared.DTOs.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DentalBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<DashboardStatsDto>>> GetStats()
    {
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var todayAppointments = (await _unitOfWork.Appointments.FindAsync(
            a => a.AppointmentDate >= today && a.AppointmentDate < tomorrow && !a.IsDeleted)).Count;

        var totalPatients = (await _unitOfWork.Patients.GetAllAsync()).Count;

        var activeConversations = (await _unitOfWork.Conversations.FindAsync(
            c => c.Status == ConversationStatus.Open && !c.IsDeleted)).Count;

        var pendingAppointments = (await _unitOfWork.Appointments.FindAsync(
            a => a.Status == AppointmentStatus.Scheduled && a.AppointmentDate >= today && !a.IsDeleted)).Count;

        var stats = new DashboardStatsDto
        {
            TodayAppointments = todayAppointments,
            TotalPatients = totalPatients,
            ActiveConversations = activeConversations,
            PendingAppointments = pendingAppointments
        };

        return Ok(ApiResponse<DashboardStatsDto>.Ok(stats));
    }
}
