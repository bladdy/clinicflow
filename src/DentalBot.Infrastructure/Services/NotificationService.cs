using DentalBot.Application.Interfaces;
using DentalBot.Domain.Enums;
using DentalBot.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DentalBot.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ApplicationDbContext context, IWhatsAppService whatsAppService, ILogger<NotificationService> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _logger = logger;
    }

    public async Task SendAppointmentReminderAsync(Guid appointmentId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .Include(a => a.Service)
            .Include(a => a.Branch).ThenInclude(b => b!.WhatsAppInstances)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && !a.IsDeleted);

        if (appointment?.Patient == null || appointment.Status == AppointmentStatus.Cancelled)
            return;

        var instance = appointment.Branch?.WhatsAppInstances?.FirstOrDefault(w => w.IsActive);
        if (instance == null)
        {
            _logger.LogWarning("No active WhatsApp instance for branch {BranchId}", appointment.BranchId);
            return;
        }

        var doctorName = appointment.Doctor?.User != null
            ? $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}"
            : "su doctor";

        var message = $"Recordatorio: Tiene una cita programada para mañana {appointment.StartTime:hh\\:mm} con {doctorName} " +
                       $"para {appointment.Service?.Name}. " +
                       $"Ubicación: {appointment.Branch?.Name}. " +
                       $"Si necesita reprogramar, responda a este mensaje.";

        await _whatsAppService.SendMessageAsync(instance.Id, appointment.Patient.Phone, message);

        _logger.LogInformation("Reminder sent for appointment {AppointmentId} to {Phone}", appointmentId, appointment.Patient.Phone);
    }

    public async Task SendAppointmentConfirmationAsync(Guid appointmentId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Doctor).ThenInclude(d => d!.User)
            .Include(a => a.Service)
            .Include(a => a.Branch).ThenInclude(b => b!.WhatsAppInstances)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && !a.IsDeleted);

        if (appointment?.Patient == null || appointment.Status == AppointmentStatus.Cancelled)
            return;

        var instance = appointment.Branch?.WhatsAppInstances?.FirstOrDefault(w => w.IsActive);
        if (instance == null) return;

        var doctorName = appointment.Doctor?.User != null
            ? $"Dr. {appointment.Doctor.User.FirstName} {appointment.Doctor.User.LastName}"
            : "su doctor";

        var message = $"Su cita ha sido confirmada para el {appointment.AppointmentDate:dd/MM/yyyy} a las {appointment.StartTime:hh\\:mm} " +
                       $"con {doctorName} para {appointment.Service?.Name}. " +
                       $"Ubicación: {appointment.Branch?.Name}. " +
                       $"Le esperamos puntualmente.";

        await _whatsAppService.SendMessageAsync(instance.Id, appointment.Patient.Phone, message);

        _logger.LogInformation("Confirmation sent for appointment {AppointmentId}", appointmentId);
    }

    public async Task SendPostAppointmentSurveyAsync(Guid appointmentId)
    {
        var appointment = await _context.Appointments
            .Include(a => a.Patient)
            .Include(a => a.Branch).ThenInclude(b => b!.WhatsAppInstances)
            .FirstOrDefaultAsync(a => a.Id == appointmentId && !a.IsDeleted);

        if (appointment?.Patient == null)
            return;

        var instance = appointment.Branch?.WhatsAppInstances?.FirstOrDefault(w => w.IsActive);
        if (instance == null) return;

        var message = "Gracias por visitarnos. Nos gustaría conocer su experiencia. " +
                       "¿Podría calificar su atención del 1 al 5? (1 = Mala, 5 = Excelente)";

        await _whatsAppService.SendMessageAsync(instance.Id, appointment.Patient.Phone, message);

        _logger.LogInformation("Survey sent for appointment {AppointmentId}", appointmentId);
    }

    public async Task ProcessRemindersAsync()
    {
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        var dayAfterTomorrow = tomorrow.AddDays(1);

        var appointments = await _context.Appointments
            .Where(a => !a.IsDeleted &&
                        a.AppointmentDate >= tomorrow &&
                        a.AppointmentDate < dayAfterTomorrow &&
                        a.Status == AppointmentStatus.Scheduled)
            .ToListAsync();

        _logger.LogInformation("Processing {Count} appointment reminders", appointments.Count);

        foreach (var appointment in appointments)
        {
            try
            {
                await SendAppointmentReminderAsync(appointment.Id);
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send reminder for appointment {AppointmentId}", appointment.Id);
            }
        }
    }

    public async Task ProcessPostAppointmentSurveysAsync()
    {
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        var today = DateTime.UtcNow.Date;

        var appointments = await _context.Appointments
            .Where(a => !a.IsDeleted &&
                        a.AppointmentDate >= yesterday &&
                        a.AppointmentDate < today &&
                        a.Status == AppointmentStatus.Completed)
            .ToListAsync();

        _logger.LogInformation("Processing {Count} post-appointment surveys", appointments.Count);

        foreach (var appointment in appointments)
        {
            try
            {
                await SendPostAppointmentSurveyAsync(appointment.Id);
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send survey for appointment {AppointmentId}", appointment.Id);
            }
        }
    }
}
