namespace DentalBot.Shared.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int TodayAppointments { get; set; }
    public int TotalPatients { get; set; }
    public int ActiveConversations { get; set; }
    public int PendingAppointments { get; set; }
}
