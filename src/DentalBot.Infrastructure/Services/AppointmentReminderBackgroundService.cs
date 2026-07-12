using DentalBot.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DentalBot.Infrastructure.Services;

public class AppointmentReminderBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AppointmentReminderBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public AppointmentReminderBackgroundService(IServiceProvider serviceProvider, ILogger<AppointmentReminderBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Appointment Reminder Background Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                await notificationService.ProcessRemindersAsync();
                await notificationService.ProcessPostAppointmentSurveysAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in reminder background service");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Appointment Reminder Background Service stopped");
    }
}
