namespace DentalBot.Application.Interfaces;

public interface INotificationService
{
    Task SendAppointmentReminderAsync(Guid appointmentId);
    Task SendAppointmentConfirmationAsync(Guid appointmentId);
    Task SendPostAppointmentSurveyAsync(Guid appointmentId);
    Task ProcessRemindersAsync();
    Task ProcessPostAppointmentSurveysAsync();
}
