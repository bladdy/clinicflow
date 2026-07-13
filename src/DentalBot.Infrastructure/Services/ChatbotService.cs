using System.Text.Json;
using DentalBot.Application.Interfaces;
using DentalBot.Domain.Entities;
using DentalBot.Domain.Enums;
using Microsoft.Extensions.Logging;
using BookingDayOfWeek = DentalBot.Domain.Enums.DayOfWeek;

namespace DentalBot.Infrastructure.Services;

public class ChatbotService : IChatbotService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAIService _aiService;
    private readonly ILogger<ChatbotService> _logger;

    public ChatbotService(IUnitOfWork unitOfWork, IAIService aiService, ILogger<ChatbotService> logger)
    {
        _unitOfWork = unitOfWork;
        _aiService = aiService;
        _logger = logger;
    }

    public async Task<string> HandleMessageAsync(Conversation conversation, string userMessage, Guid companyId)
    {
        var state = LoadState(conversation.BookingState);
        var lowerMessage = userMessage.Trim().ToLower();

        _logger.LogInformation("Chatbot: Phase={Phase}, Message={Message}", state.Phase, userMessage);

        if (IsCancel(lowerMessage))
        {
            ResetState(conversation);
            return "La conversación se ha reiniciado. ¿En qué puedo ayudarte?";
        }

        if (IsGreeting(lowerMessage) && state.Phase == "idle")
        {
            if (conversation.PatientId.HasValue)
            {
                var patient = await _unitOfWork.Patients.GetByIdAsync(conversation.PatientId.Value);
                if (patient != null)
                {
                    state.PatientFirstName = patient.FirstName;
                    state.PatientLastName = patient.LastName;
                    state.PatientPhone = patient.Phone;
                    state.PatientEmail = patient.Email;
                    state.ExistingPatientId = patient.Id;

                    var existingAppointment = await CheckExistingAppointments(companyId, patient.Id);
                    if (existingAppointment != null)
                    {
                        state.EditingAppointmentId = existingAppointment.Id;
                        state.Phase = "modifying_appointment";
                        SaveState(conversation, state);
                        return $"¡Hola {patient.FirstName}! 😊\n\n" + await FormatExistingAppointmentMessage(existingAppointment);
                    }
                }
            }

            return "¡Hola! Soy el asistente virtual de la clínica dental. 😊\n\nPuedo ayudarte con:\n• *Agendar una cita*\n• *Consultar servicios y precios*\n• *Conocer nuestros doctores*\n\n¿Qué te gustaría hacer?";
        }

        return state.Phase switch
        {
            "idle" => await HandleIdle(conversation, state, userMessage, companyId, lowerMessage),
            "collecting_name" => await HandleCollectingName(conversation, state, userMessage, companyId),
            "collecting_phone" => await HandleCollectingPhone(conversation, state, userMessage, companyId),
            "selecting_service" => await HandleSelectingService(conversation, state, userMessage, companyId),
            "selecting_date" => await HandleSelectingDate(conversation, state, userMessage, companyId),
            "selecting_doctor" => await HandleSelectingDoctor(conversation, state, userMessage, companyId),
            "selecting_time" => await HandleSelectingTime(conversation, state, userMessage, companyId),
            "confirming" => await HandleConfirming(conversation, state, userMessage, companyId),
            "modifying_appointment" => await HandleModifyingAppointment(conversation, state, userMessage, companyId),
            "confirm_cancellation" => await HandleConfirmCancellation(conversation, state, userMessage, companyId),
            _ => await HandleIdle(conversation, state, userMessage, companyId, lowerMessage)
        };
    }

    private async Task<string> HandleIdle(Conversation conversation, BookingStateData state, string userMessage, Guid companyId, string lower)
    {
        if (ContainsAny(lower, "cita", "agendar", "agendar cita", "quiero una cita", "reservar", "reservar cita"))
        {
            if (conversation.PatientId.HasValue)
            {
                var patient = await _unitOfWork.Patients.GetByIdAsync(conversation.PatientId.Value);
                if (patient != null)
                {
                    state.PatientFirstName = patient.FirstName;
                    state.PatientLastName = patient.LastName;
                    state.PatientPhone = patient.Phone;
                    state.PatientEmail = patient.Email;
                    state.ExistingPatientId = patient.Id;

                    var existingAppointment = await CheckExistingAppointments(companyId, patient.Id);
                    if (existingAppointment != null)
                    {
                        state.EditingAppointmentId = existingAppointment.Id;
                        state.Phase = "modifying_appointment";
                        SaveState(conversation, state);
                        return await FormatExistingAppointmentMessage(existingAppointment);
                    }

                    state.Phase = "selecting_service";
                    SaveState(conversation, state);
                    return $"¡Hola {patient.FirstName}! Veo que ya eres paciente.\n\n¿Qué servicio te gustaría agendar?";
                }
            }

            state.Phase = "collecting_name";
            SaveState(conversation, state);
            return "¡Perfecto! Te ayudo a agendar una cita.\n\n¿Cuál es tu *nombre*?";
        }

        if (ContainsAny(lower, "servicio", "servicios", "qué ofrecen", "qué hacen", "tratamiento", "tratamientos", "precio", "precios"))
        {
            return await GetServicesList(companyId);
        }

        if (ContainsAny(lower, "doctor", "doctores", "médico", "médicos", "quién me atiende", "especialista"))
        {
            return await GetDoctorsList(companyId);
        }

        if (ContainsAny(lower, "horario", "horarios", "hora", "horas", "qué hora", "abren", "cierran"))
        {
            return await GetBusinessHours(companyId);
        }

        if (ContainsAny(lower, "urgent", "urgencia", "dolor", "duele", "sangrado", "hinchazón", "fiebre", "trauma"))
        {
            return "Detecto que puede ser una urgencia. 🚨\n\nSi es una emergencia grave, por favor acude al servicio de urgencias más cercano o llama al *911*.\n\nSi deseas, puedo agendar una cita para que un doctor te revise a la brevedad. ¿Te gustaría eso?";
        }

        var aiSettings = (await _unitOfWork.AISettings.FindAsync(
            a => a.CompanyId == companyId)).FirstOrDefault();

        if (aiSettings != null && aiSettings.IsEnabled)
        {
            var recentMessages = (await _unitOfWork.Messages.FindAsync(
                m => m.ConversationId == conversation.Id))
                .OrderBy(m => m.SentAt)
                .TakeLast(10)
                .ToList();

            var context = string.Join("\n", recentMessages.Select(m =>
                $"{(m.Direction == MessageDirection.Incoming ? "Paciente" : "Bot")}: {m.Content}"));

            return await _aiService.GenerateResponseAsync(userMessage, context, aiSettings.SystemPrompt ?? string.Empty, []);
        }

        return "No estoy seguro de entender. Puedo ayudarte a:\n• *Agendar una cita* 📅\n• *Ver servicios y precios* 💰\n• *Conocer a nuestros doctores* 👨‍⚕️\n\n¿Qué te gustaría hacer?";
    }

    private async Task<string> HandleCollectingName(Conversation conversation, BookingStateData state, string userMessage, Guid companyId)
    {
        var name = userMessage.Trim();
        if (name.Split(' ').Length >= 2)
        {
            state.PatientFirstName = name.Split(' ')[0];
            state.PatientLastName = string.Join(" ", name.Split(' ').Skip(1));
        }
        else
        {
            state.PatientFirstName = name;
            state.PatientLastName = "";
        }

        state.Phase = "collecting_phone";
        SaveState(conversation, state);
        return $"¡Mucho gusto, {state.PatientFirstName}! 😊\n\n¿Cuál es tu *número de teléfono*? (Ejemplo: 8091234567)";
    }

    private async Task<string> HandleCollectingPhone(Conversation conversation, BookingStateData state, string userMessage, Guid companyId)
    {
        var phone = userMessage.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

        if (phone.Length < 8 || !phone.All(char.IsDigit))
        {
            return "Por favor, ingresa un número de teléfono válido (solo números, mínimo 8 dígitos).";
        }

        state.PatientPhone = phone;

        var existingPatient = (await _unitOfWork.Patients.FindAsync(
            p => p.CompanyId == companyId && p.Phone == phone)).FirstOrDefault();

        if (existingPatient != null)
        {
            state.ExistingPatientId = existingPatient.Id;
            state.PatientFirstName = existingPatient.FirstName;
            state.PatientLastName = existingPatient.LastName;
            conversation.PatientId = existingPatient.Id;

            var existingAppointment = await CheckExistingAppointments(companyId, existingPatient.Id);
            if (existingAppointment != null)
            {
                state.EditingAppointmentId = existingAppointment.Id;
                state.Phase = "modifying_appointment";
                SaveState(conversation, state);
                return await FormatExistingAppointmentMessage(existingAppointment);
            }

            state.Phase = "selecting_service";
            SaveState(conversation, state);
            return $"¡Hola de nuevo, {existingPatient.FirstName}! Encontré tu registro.\n\n¿Qué servicio te gustaría agendar?";
        }

        state.Phase = "selecting_service";
        SaveState(conversation, state);
        return $"Gracias, {state.PatientFirstName}. 😊\n\n¿Qué servicio te gustaría agendar?";
    }

    private async Task<string> HandleSelectingService(Conversation conversation, BookingStateData state, string userMessage, Guid companyId)
    {
        var services = (await _unitOfWork.Services.FindAsync(
            s => s.CompanyId == companyId && s.IsActive)).ToList();

        var lower = userMessage.Trim().ToLower();
        var selected = services.FirstOrDefault(s => lower.Contains(s.Name.ToLower()) || s.Name.ToLower().Contains(lower));

        if (selected == null && int.TryParse(lower, out var num) && num >= 1 && num <= services.Count)
        {
            selected = services[num - 1];
        }

        if (selected == null)
        {
            var numbered = services.Select((s, i) => $"{i + 1}. *{s.Name}* - ${s.Price} ({s.DurationMinutes} min)").ToList();
            return $"No encontré ese servicio. Por favor, elige uno de la lista:\n\n{string.Join("\n", numbered)}\n\nEscribe el *nombre* o el *número* del servicio.";
        }

        state.SelectedServiceId = selected.Id;
        state.SelectedServiceName = selected.Name;
        state.SelectedServiceDuration = selected.DurationMinutes;

        if (state.EditingAppointmentId.HasValue && state.ModifyTarget == "service")
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(state.EditingAppointmentId.Value);
            var timeStr = FormatTime12(appointment?.StartTime ?? new TimeSpan(9, 0, 0));
            state.Phase = "confirming";
            SaveState(conversation, state);
            return $"📋 *Resumen de los cambios:*\n\n" +
                   $"• Paciente: *{state.PatientFirstName} {state.PatientLastName}*\n" +
                   $"• Teléfono: {state.PatientPhone}\n" +
                   $"• Servicio: *{selected.Name}* ({selected.DurationMinutes} min - ${selected.Price})\n" +
                   $"• Fecha: *{state.SelectedDate!.Value:dddd dd/MM/yyyy}*\n" +
                   $"• Hora: *{timeStr}*\n" +
                   $"• Doctor: *Dr(a). {state.SelectedDoctorName}*\n\n" +
                   $"¿Confirmas los cambios? Responde *sí* para confirmar o *no* para cancelar.";
        }

        state.Phase = "selecting_date";
        SaveState(conversation, state);

        return $"Servicio seleccionado: *{selected.Name}* ({selected.DurationMinutes} min - ${selected.Price})\n\n¿Para qué *fecha* te gustaría la cita? (Ejemplo: 15 de julio, lunes, etc.)";
    }

    private async Task<string> HandleSelectingDate(Conversation conversation, BookingStateData state, string userMessage, Guid companyId)
    {
        var date = ParseDate(userMessage.Trim());

        if (date == null)
        {
            return "No pude entender la fecha. Por favor, ingresa una fecha válida como:\n• *15 de julio*\n• *lunes*\n• *próximo lunes*\n• *2026-07-15*";
        }

        if (date.Value.Date < DateTime.Today)
        {
            return "La fecha no puede ser en el pasado. Por favor, elige una fecha futura.";
        }

        var branch = (await _unitOfWork.Branches.FindAsync(
            b => b.CompanyId == companyId && b.IsMain)).FirstOrDefault();
        if (branch == null)
            return "No pudimos encontrar la sucursal. Por favor, contacta a la clínica.";

        var schedule = (await _unitOfWork.BusinessSchedules.FindAsync(
            s => s.BranchId == branch.Id && !s.IsDeleted)).FirstOrDefault();
        if (schedule == null)
            return "No hay horarios configurados. Por favor, contacta a la clínica.";

        var dayOfWeekEsp = MapDayOfWeek(date.Value.DayOfWeek);
        var scheduleDay = (await _unitOfWork.ScheduleDays.FindAsync(
            d => d.BusinessScheduleId == schedule.Id && d.DayOfWeek == dayOfWeekEsp && !d.IsDeleted)).FirstOrDefault();

        if (scheduleDay == null || !scheduleDay.IsOpen)
        {
            return "Lo siento, no atendemos ese día. Por favor, elige otro día.";
        }

        var holiday = (await _unitOfWork.Holidays.FindAsync(
            h => h.CompanyId == companyId && h.Date.Date == date.Value.Date)).FirstOrDefault();

        if (holiday != null)
        {
            return $"El día {date.Value:dd/MM/yyyy} es festivo (*{holiday.Name}*) y no atendemos. Por favor, elige otra fecha.";
        }

        state.SelectedDate = date.Value;
        state.Phase = "selecting_doctor";
        SaveState(conversation, state);

        var doctors = await GetAvailableDoctors(companyId, date.Value);
        if (string.IsNullOrEmpty(doctors))
        {
            state.Phase = "selecting_date";
            SaveState(conversation, state);
            return "No hay doctores disponibles para esa fecha. Por favor, elige otra fecha.";
        }

        return $"Fecha seleccionada: *{date.Value:dddd dd/MM/yyyy}* 📅\n\nDoctores disponibles:\n{doctors}\n\nEscribe el *nombre* o el *número* del doctor.";
    }

    private async Task<string> HandleSelectingDoctor(Conversation conversation, BookingStateData state, string userMessage, Guid companyId)
    {
        var doctors = (await _unitOfWork.Doctors.FindAsync(
            d => d.CompanyId == companyId)).ToList();

        var users = (await _unitOfWork.Users.FindAsync(
            u => doctors.Select(d => d.UserId).Contains(u.Id))).ToList();

        var lower = userMessage.Trim().ToLower();
        Doctor? selected = null;

        if (int.TryParse(userMessage.Trim(), out var num) && num >= 1 && num <= doctors.Count)
        {
            selected = doctors[num - 1];
        }
        else
        {
            foreach (var doc in doctors)
            {
                var user = users.FirstOrDefault(u => u.Id == doc.UserId);
                var fullName = $"{user?.FirstName} {user?.LastName}".ToLower();
                if (lower.Contains(user?.FirstName?.ToLower() ?? "") || lower.Contains(fullName) || lower.Contains(doc.Specialty.ToLower()))
                {
                    selected = doc;
                    break;
                }
            }
        }

        if (selected == null)
        {
            var doctorList = await GetAvailableDoctors(companyId, state.SelectedDate!.Value);
            return $"No encontré ese doctor. Por favor, elige de la lista:\n\n{doctorList}";
        }

        var selectedUser = users.FirstOrDefault(u => u.Id == selected.UserId);
        state.SelectedDoctorId = selected.Id;
        state.SelectedDoctorName = $"{selectedUser?.FirstName} {selectedUser?.LastName}";
        state.Phase = "selecting_time";
        SaveState(conversation, state);

        var hoursRange = await GetBusinessHoursRange(selected.Id, state.SelectedDate!.Value);
        if (string.IsNullOrEmpty(hoursRange))
        {
            state.Phase = "selecting_date";
            SaveState(conversation, state);
            return $"El Dr(a). {state.SelectedDoctorName} no tiene horarios disponibles para esa fecha. Por favor, elige otra fecha.";
        }

        return $"Doctor seleccionado: *Dr(a). {state.SelectedDoctorName}* 👨‍⚕️\n\n{hoursRange}";
    }

    private async Task<string> HandleSelectingTime(Conversation conversation, BookingStateData state, string userMessage, Guid companyId)
    {
        var parsedTime = ParseTime(userMessage.Trim());
        var durationMinutes = state.SelectedServiceDuration ?? 30;

        var (available, error) = await IsTimeSlotAvailable(companyId, state.SelectedDoctorId!.Value, state.SelectedDate!.Value, parsedTime, durationMinutes, state.EditingAppointmentId);
        if (!available)
        {
            return error!;
        }

        state.SelectedTime = $"{parsedTime.Hours:D2}:{parsedTime.Minutes:D2}";
        state.Phase = "confirming";
        SaveState(conversation, state);

        var dateStr = state.SelectedDate!.Value.ToString("dddd dd/MM/yyyy");
        var timeStr = FormatTime12(parsedTime);
        var isEditing = state.EditingAppointmentId.HasValue;
        var title = isEditing ? "📋 *Resumen de los cambios:*" : "📋 *Resumen de tu cita:*";
        var confirmText = isEditing
            ? "¿Confirmas los cambios? Responde *sí* para confirmar o *no* para cancelar."
            : "¿Confirmas esta cita? Responde *sí* para confirmar o *no* para cancelar.";

        return $"{title}\n\n" +
               $"• Paciente: *{state.PatientFirstName} {state.PatientLastName}*\n" +
               $"• Teléfono: {state.PatientPhone}\n" +
               $"• Servicio: *{state.SelectedServiceName}*\n" +
               $"• Fecha: *{dateStr}*\n" +
               $"• Hora: *{timeStr}*\n" +
               $"• Doctor: *Dr(a). {state.SelectedDoctorName}*\n\n" +
               $"{confirmText}";
    }

    private async Task<string> HandleConfirming(Conversation conversation, BookingStateData state, string userMessage, Guid companyId)
    {
        var lower = userMessage.Trim().ToLower();

        if (ContainsAny(lower, "sí", "si", "yes", "confirmo", "confirmar", "ok", "dale", "confirmo"))
        {
            Guid patientId;

            if (state.ExistingPatientId.HasValue)
            {
                patientId = state.ExistingPatientId.Value;
            }
            else
            {
                var newPatient = new Patient
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    FirstName = state.PatientFirstName ?? "",
                    LastName = state.PatientLastName ?? "",
                    Phone = state.PatientPhone,
                    Email = state.PatientEmail
                };
                await _unitOfWork.Patients.AddAsync(newPatient);
                await _unitOfWork.SaveChangesAsync();
                patientId = newPatient.Id;
                conversation.PatientId = patientId;
            }

            var startTime = ParseTime(state.SelectedTime!);
            var endTime = startTime.Add(TimeSpan.FromMinutes(state.SelectedServiceDuration ?? 30));

            if (state.EditingAppointmentId.HasValue)
            {
                var appointment = await _unitOfWork.Appointments.GetByIdAsync(state.EditingAppointmentId.Value);
                if (appointment == null)
                {
                    ResetState(conversation);
                    return "No encontré la cita a modificar. Por favor, agenda una nueva cita.";
                }

                appointment.ServiceId = state.SelectedServiceId!.Value;
                appointment.DoctorId = state.SelectedDoctorId!.Value;
                appointment.AppointmentDate = state.SelectedDate!.Value.Date;
                appointment.StartTime = startTime;
                appointment.EndTime = endTime;
                appointment.Reason = state.SelectedServiceName;
                appointment.PatientId = patientId;

                _unitOfWork.Appointments.Update(appointment);
                await _unitOfWork.SaveChangesAsync();

                ResetState(conversation);

                return $"✅ *¡Cita actualizada exitosamente!*\n\n" +
                       $"• Servicio: *{state.SelectedServiceName}*\n" +
                       $"• Fecha: *{state.SelectedDate:dddd dd/MM/yyyy}*\n" +
                       $"• Hora: *{FormatTime12(TimeSpan.Parse(state.SelectedTime))}*\n" +
                       $"• Doctor: *Dr(a). {state.SelectedDoctorName}*\n\n" +
                       $"Si necesitas más cambios, avísanos. 😊";
            }

            var mainBranch = (await _unitOfWork.Branches.FindAsync(
                b => b.CompanyId == companyId && b.IsMain)).FirstOrDefault();

            var newAppointment = new Appointment
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                BranchId = mainBranch?.Id ?? Guid.Empty,
                DoctorId = state.SelectedDoctorId!.Value,
                PatientId = patientId,
                ServiceId = state.SelectedServiceId!.Value,
                AppointmentDate = state.SelectedDate!.Value.Date,
                StartTime = startTime,
                EndTime = endTime,
                Status = AppointmentStatus.Scheduled,
                Reason = state.SelectedServiceName
            };

            await _unitOfWork.Appointments.AddAsync(newAppointment);
            await _unitOfWork.SaveChangesAsync();

            ResetState(conversation);

            return $"✅ *¡Cita agendada exitosamente!*\n\n" +
                   $"• Paciente: *{state.PatientFirstName} {state.PatientLastName}*\n" +
                   $"• Servicio: *{state.SelectedServiceName}*\n" +
                   $"• Fecha: *{state.SelectedDate:dddd dd/MM/yyyy}*\n" +
                   $"• Hora: *{FormatTime12(TimeSpan.Parse(state.SelectedTime))}*\n" +
                   $"• Doctor: *Dr(a). {state.SelectedDoctorName}*\n\n" +
                   $"Te esperamos puntualmente. Si necesitas reprogramar, avísanos con anticipación. 😊";
        }

        if (ContainsAny(lower, "no", "cancelar", "cancel", "nada"))
        {
            if (state.EditingAppointmentId.HasValue)
            {
                ResetState(conversation);
                return "La modificación ha sido cancelada. Tu cita original se mantiene intacta. Si necesitas algo más, avísanos. 😊";
            }

            ResetState(conversation);
            return "La cita ha sido cancelada. Si deseas agendar otra, solo dime. 😊";
        }

        return "¿Deseas *confirmar* la cita? Responde *sí* o *no*.";
    }

    private async Task<Appointment?> CheckExistingAppointments(Guid companyId, Guid patientId)
    {
        return (await _unitOfWork.Appointments.FindAsync(
            a => a.PatientId == patientId &&
                 a.CompanyId == companyId &&
                 (a.Status == AppointmentStatus.Scheduled || a.Status == AppointmentStatus.Confirmed) &&
                 a.AppointmentDate.Date >= DateTime.Today))
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .FirstOrDefault();
    }

    private async Task<string> FormatExistingAppointmentMessage(Appointment appointment)
    {
        var doctor = await _unitOfWork.Doctors.GetByIdAsync(appointment.DoctorId);
        string doctorName = "N/A";
        if (doctor != null)
        {
            var user = await _unitOfWork.Users.GetByIdAsync(doctor.UserId);
            doctorName = $"{user?.FirstName} {user?.LastName}";
        }

        var service = await _unitOfWork.Services.GetByIdAsync(appointment.ServiceId);
        var serviceName = service?.Name ?? "N/A";
        var servicePrice = service?.Price ?? 0;
        var serviceDuration = service?.DurationMinutes ?? 30;

        var dateStr = appointment.AppointmentDate.ToString("dddd dd/MM/yyyy");
        var timeStr = FormatTime12(appointment.StartTime);

        return $"📋 *Tienes una cita programada:*\n\n" +
               $"📅 Fecha: *{dateStr}*\n" +
               $"🕐 Hora: *{timeStr}*\n" +
               $"👨‍⚕️ Doctor: *Dr(a). {doctorName}*\n" +
               $"💰 Servicio: *{serviceName}* (${servicePrice} - {serviceDuration} min)\n\n" +
               $"¿Qué te gustaría hacer?\n" +
               $"1. *Cambiar la fecha*\n" +
               $"2. *Cambiar el doctor*\n" +
               $"3. *Cambiar el servicio*\n" +
               $"4. *Cambiar la hora*\n" +
               $"5. *Cancelar esta cita*\n" +
               $"6. *Agendar una nueva cita*\n\n" +
               $"Escribe el *número* de la opción.";
    }

    private async Task<string> HandleModifyingAppointment(Conversation conversation, BookingStateData state, string userMessage, Guid companyId)
    {
        var lower = userMessage.Trim().ToLower();

        var existingAppointment = await _unitOfWork.Appointments.GetByIdAsync(state.EditingAppointmentId!.Value);
        if (existingAppointment == null)
        {
            ResetState(conversation);
            return "No encontré la cita. Por favor, agenda una nueva cita.";
        }

        var service = await _unitOfWork.Services.GetByIdAsync(existingAppointment.ServiceId);
        var doctor = await _unitOfWork.Doctors.GetByIdAsync(existingAppointment.DoctorId);

        var hasTimeKeyword = lower.Contains("am") || lower.Contains("pm") ||
            lower.Contains("tarde") || lower.Contains("mañana") || lower.Contains("manana") ||
            lower.Contains("mediodía") || lower.Contains("medio dia") || lower.Contains("medianoche");
        var hasTimePattern = System.Text.RegularExpressions.Regex.IsMatch(lower, @"\d+\s*(de\s+la(s)?)?\s*(tarde|mañana|manana|am|pm)");
        var looksLikeTime = hasTimeKeyword || hasTimePattern;

        if (looksLikeTime)
        {
            state.SelectedServiceId = existingAppointment.ServiceId;
            state.SelectedServiceName = service?.Name;
            state.SelectedServiceDuration = service?.DurationMinutes ?? 30;
            state.SelectedDate = existingAppointment.AppointmentDate;
            state.SelectedDoctorId = existingAppointment.DoctorId;
            if (doctor != null)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(doctor.UserId);
                state.SelectedDoctorName = $"{user?.FirstName} {user?.LastName}";
            }
            state.ModifyTarget = "time";
            state.Phase = "selecting_time";
            SaveState(conversation, state);
            return await HandleSelectingTime(conversation, state, userMessage, companyId);
        }

        if (ContainsAny(lower, "1", "fecha", "date", "cambiar fecha", "cambiar la fecha"))
        {
            state.SelectedServiceId = existingAppointment.ServiceId;
            state.SelectedServiceName = service?.Name;
            state.SelectedServiceDuration = service?.DurationMinutes ?? 30;
            state.SelectedDoctorId = existingAppointment.DoctorId;
            if (doctor != null)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(doctor.UserId);
                state.SelectedDoctorName = $"{user?.FirstName} {user?.LastName}";
            }
            state.ModifyTarget = "date";
            state.Phase = "selecting_date";
            SaveState(conversation, state);
            return $"Entendido. Vamos a cambiar la fecha de tu cita de *{service?.Name}*.\n\n¿Para qué *nueva fecha* te gustaría la cita? (Ejemplo: 15 de julio, lunes, etc.)";
        }

        if (ContainsAny(lower, "2", "doctor", "cambiar doctor", "cambiar el doctor"))
        {
            state.SelectedServiceId = existingAppointment.ServiceId;
            state.SelectedServiceName = service?.Name;
            state.SelectedServiceDuration = service?.DurationMinutes ?? 30;
            state.SelectedDate = existingAppointment.AppointmentDate;
            state.ModifyTarget = "doctor";
            state.Phase = "selecting_doctor";
            SaveState(conversation, state);
            var doctors = await GetAvailableDoctors(companyId, existingAppointment.AppointmentDate);
            return $"Entendido. Vamos a cambiar el doctor para tu cita del *{existingAppointment.AppointmentDate:dddd dd/MM/yyyy}*.\n\nDoctores disponibles:\n{doctors}\n\nEscribe el *nombre* o el *número* del doctor.";
        }

        if (ContainsAny(lower, "3", "servicio", "service", "cambiar servicio", "cambiar el servicio"))
        {
            state.SelectedDate = existingAppointment.AppointmentDate;
            state.SelectedDoctorId = existingAppointment.DoctorId;
            if (doctor != null)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(doctor.UserId);
                state.SelectedDoctorName = $"{user?.FirstName} {user?.LastName}";
            }
            state.ModifyTarget = "service";
            state.Phase = "selecting_service";
            SaveState(conversation, state);
            return $"Entendido. Vamos a cambiar el servicio de tu cita.\n\n¿Qué *nuevo servicio* te gustaría agendar?";
        }

        if (ContainsAny(lower, "4", "hora", "time", "cambiar hora", "cambiar la hora"))
        {
            state.SelectedServiceId = existingAppointment.ServiceId;
            state.SelectedServiceName = service?.Name;
            state.SelectedServiceDuration = service?.DurationMinutes ?? 30;
            state.SelectedDate = existingAppointment.AppointmentDate;
            state.SelectedDoctorId = existingAppointment.DoctorId;
            if (doctor != null)
            {
                var user = await _unitOfWork.Users.GetByIdAsync(doctor.UserId);
                state.SelectedDoctorName = $"{user?.FirstName} {user?.LastName}";
            }
            state.ModifyTarget = "time";
            SaveState(conversation, state);

            var hoursRange = await GetBusinessHoursRange(companyId, existingAppointment.AppointmentDate);
            if (string.IsNullOrEmpty(hoursRange))
            {
                state.Phase = "modifying_appointment";
                SaveState(conversation, state);
                return "Lo siento, no hay horarios disponibles para esa fecha con ese doctor.\n\n¿Qué opción prefieres?\n1. *Cambiar la fecha*\n2. *Cambiar el doctor*\n5. *Cancelar esta cita*\n6. *Agendar una nueva cita*";
            }

            state.Phase = "selecting_time";
            SaveState(conversation, state);
            return $"Entendido. Vamos a cambiar la hora de tu cita del *{existingAppointment.AppointmentDate:dddd dd/MM/yyyy}*.\n\n{hoursRange}";
        }

        if (ContainsAny(lower, "5", "cancelar", "cancel"))
        {
            state.Phase = "confirm_cancellation";
            SaveState(conversation, state);
            return $"¿Estás seguro de que quieres cancelar tu cita del *{existingAppointment.AppointmentDate:dddd dd/MM/yyyy}* con *Dr(a).{(doctor != null ? $" {(await _unitOfWork.Users.GetByIdAsync(doctor.UserId))?.FirstName}" : "")}*?\n\nResponde *sí* para cancelar o *no* para mantener la cita.";
        }

        if (ContainsAny(lower, "6", "nueva", "new", "agendar nueva", "otra cita"))
        {
            state.EditingAppointmentId = null;
            state.ModifyTarget = null;
            state.Phase = "selecting_service";
            SaveState(conversation, state);
            return "¡Perfecto! Vamos a agendar una nueva cita.\n\n¿Qué servicio te gustaría?";
        }

        return "Por favor, elige una opción del *1 al 6*:\n" +
               "1. *Cambiar la fecha*\n" +
               "2. *Cambiar el doctor*\n" +
               "3. *Cambiar el servicio*\n" +
               "4. *Cambiar la hora*\n" +
               "5. *Cancelar esta cita*\n" +
               "6. *Agendar una nueva cita*";
    }

    private async Task<string> HandleConfirmCancellation(Conversation conversation, BookingStateData state, string userMessage, Guid companyId)
    {
        var lower = userMessage.Trim().ToLower();

        if (ContainsAny(lower, "sí", "si", "yes", "confirmo", "confirmar", "ok", "dale"))
        {
            var appointment = await _unitOfWork.Appointments.GetByIdAsync(state.EditingAppointmentId!.Value);
            if (appointment != null)
            {
                appointment.Status = AppointmentStatus.Cancelled;
                _unitOfWork.Appointments.Update(appointment);
                await _unitOfWork.SaveChangesAsync();
            }

            ResetState(conversation);
            return "✅ Tu cita ha sido cancelada exitosamente. Si necesitas agendar otra, solo dime. 😊";
        }

        if (ContainsAny(lower, "no", "nada", "mantener", "conservar"))
        {
            ResetState(conversation);
            return "¡Genial! Tu cita se mantiene intacta. Si necesitas algo más, avísanos. 😊";
        }

        return "¿Deseas *confirmar* la cancelación? Responde *sí* para cancelar o *no* para mantener tu cita.";
    }

    private async Task<string> GetServicesList(Guid companyId)
    {
        var services = (await _unitOfWork.Services.FindAsync(
            s => s.CompanyId == companyId && s.IsActive)).ToList();

        if (services.Count == 0)
            return "Actualmente no tenemos servicios disponibles. Por favor, contacta a la clínica directamente.";

        var list = services.Select(s => $"• *{s.Name}* - ${s.Price} ({s.DurationMinutes} min)\n  {s.Description}").ToList();
        return $"nuestros servicios: 💰\n\n{string.Join("\n\n", list)}\n\n¿Te gustaría agendar una cita para alguno?";
    }

    private async Task<string> GetDoctorsList(Guid companyId)
    {
        var doctors = (await _unitOfWork.Doctors.FindAsync(
            d => d.CompanyId == companyId)).ToList();

        if (doctors.Count == 0)
            return "Actualmente no tenemos doctores registrados. Por favor, contacta a la clínica.";

        var userIds = doctors.Select(d => d.UserId).ToList();
        var users = (await _unitOfWork.Users.FindAsync(
            u => userIds.Contains(u.Id))).ToList();

        var list = doctors.Select(d =>
        {
            var user = users.FirstOrDefault(u => u.Id == d.UserId);
            return $"• *Dr(a). {user?.FirstName} {user?.LastName}*\n  Especialidad: {d.Specialty}";
        }).ToList();

        return $"Nuestros doctores: 👨‍⚕️\n\n{string.Join("\n\n", list)}\n\n¿Te gustaría agendar una cita?";
    }

    private async Task<string> GetBusinessHours(Guid companyId)
    {
        var branch = (await _unitOfWork.Branches.FindAsync(
            b => b.CompanyId == companyId && b.IsMain)).FirstOrDefault();

        if (branch == null)
            return "No pudimos encontrar los horarios. Por favor, contacta a la clínica.";

        var schedule = (await _unitOfWork.BusinessSchedules.FindAsync(
            s => s.BranchId == branch.Id && !s.IsDeleted)).FirstOrDefault();

        if (schedule == null)
            return "Nuestro horario de atención es de *lunes a viernes de 9:00 AM a 5:00 PM*.\n\n¿Te gustaría agendar una cita?";

        var days = (await _unitOfWork.ScheduleDays.FindAsync(
            d => d.BusinessScheduleId == schedule.Id && !d.IsDeleted)).ToList();

        if (days.Count == 0 || days.All(d => !d.IsOpen))
            return "Nuestro horario de atención es de *lunes a viernes de 9:00 AM a 5:00 PM*.\n\n¿Te gustaría agendar una cita?";

        var lines = days.Where(d => d.IsOpen).OrderBy(d => d.DayOfWeek)
            .Select(d => $"• {d.DayOfWeek}: {FormatTime12(d.OpenTime)} - {FormatTime12(d.CloseTime)}").ToList();

        return $"Horarios de atención: 🕐\n\n{string.Join("\n", lines)}\n\n¿Te gustaría agendar una cita?";
    }

    private async Task<string> GetAvailableDoctors(Guid companyId, DateTime date)
    {
        var allDoctors = (await _unitOfWork.Doctors.FindAsync(d => d.CompanyId == companyId)).ToList();
        if (allDoctors.Count == 0) return "";

        var userIds = allDoctors.Select(d => d.UserId).ToList();
        var allUsers = (await _unitOfWork.Users.FindAsync(u => userIds.Contains(u.Id))).ToList();

        var lines = allDoctors.Select((d, i) =>
        {
            var u = allUsers.FirstOrDefault(x => x.Id == d.UserId);
            return $"{i + 1}. *Dr(a). {u?.FirstName} {u?.LastName}* - {d.Specialty}";
        });

        return string.Join("\n", lines);
    }

    private async Task<string> GetBusinessHoursRange(Guid companyId, DateTime date)
    {
        var branch = (await _unitOfWork.Branches.FindAsync(b => b.CompanyId == companyId && b.IsMain)).FirstOrDefault();
        if (branch == null) return "";

        var schedule = (await _unitOfWork.BusinessSchedules.FindAsync(
            s => s.BranchId == branch.Id && !s.IsDeleted)).FirstOrDefault();
        if (schedule == null) return "";

        var dayOfWeekEsp = MapDayOfWeek(date.DayOfWeek);
        var day = (await _unitOfWork.ScheduleDays.FindAsync(
            d => d.BusinessScheduleId == schedule.Id && d.DayOfWeek == dayOfWeekEsp && !d.IsDeleted)).FirstOrDefault();

        if (day == null || !day.IsOpen) return "";

        return $"📅 Horario disponible: *{FormatTime12(day.OpenTime)}* a *{FormatTime12(day.CloseTime)}*\nPuedes elegir cualquier hora dentro de ese horario (ej: 2 de la tarde, 11 de la mañana, 3 PM, etc.)";
    }

    private async Task<(bool available, string? error)> IsTimeSlotAvailable(Guid companyId, Guid doctorId, DateTime date, TimeSpan startTime, int durationMinutes, Guid? excludeAppointmentId = null)
    {
        var branch = (await _unitOfWork.Branches.FindAsync(
            b => b.CompanyId == companyId && b.IsMain)).FirstOrDefault();
        if (branch == null)
            return (false, "No se encontró la sucursal.");

        var schedule = (await _unitOfWork.BusinessSchedules.FindAsync(
            s => s.BranchId == branch.Id && !s.IsDeleted)).FirstOrDefault();
        if (schedule == null)
            return (false, "No hay horarios configurados.");

        var dayOfWeekEsp = MapDayOfWeek(date.DayOfWeek);
        var day = (await _unitOfWork.ScheduleDays.FindAsync(
            d => d.BusinessScheduleId == schedule.Id && d.DayOfWeek == dayOfWeekEsp && !d.IsDeleted)).FirstOrDefault();

        if (day == null || !day.IsOpen)
            return (false, "No hay horarios de atención para ese día.");

        var openTime = day.OpenTime;
        var closeTime = day.CloseTime;
        var endTime = startTime.Add(TimeSpan.FromMinutes(durationMinutes));

        if (startTime < openTime || endTime > closeTime)
        {
            var range = $"{FormatTime12(openTime)} a {FormatTime12(closeTime)}";
            var latestStart = FormatTime12(closeTime.Add(TimeSpan.FromMinutes(-durationMinutes)));
            return (false, $"La hora debe estar dentro del horario de atención: *{range}*.\nTu cita dura {durationMinutes} minutos, así que la hora de inicio debe ser como máximo *{latestStart}*.");
        }

        var breaks = (await _unitOfWork.BreakPeriods.FindAsync(
            b => b.BusinessScheduleId == schedule.Id && !b.IsDeleted)).ToList();

        foreach (var b in breaks)
        {
            if (startTime < b.EndTime && endTime > b.StartTime)
            {
                return (false, $"⚠️ En ese horario hay un descanso (*{b.Name}*) de {FormatTime12(b.StartTime)} a {FormatTime12(b.EndTime)}. Por favor, elige otra hora.");
            }
        }

        var lunch = (await _unitOfWork.LunchConfigs.FindAsync(
            l => l.BusinessScheduleId == schedule.Id && !l.IsDeleted)).FirstOrDefault();

        if (lunch != null)
        {
            var lunchEnd = lunch.EndTime;
            if (startTime < lunchEnd && endTime > lunch.StartTime)
            {
                return (false, $"⚠️ En ese horario es la hora de comer ({FormatTime12(lunch.StartTime)} a {FormatTime12(lunchEnd)}). Por favor, elige otra hora.");
            }
        }

        var existingAppointments = (await _unitOfWork.Appointments.FindAsync(
            a => a.DoctorId == doctorId && a.AppointmentDate.Date == date.Date &&
                 a.Status != AppointmentStatus.Cancelled &&
                 (excludeAppointmentId == null || a.Id != excludeAppointmentId))).ToList();

        var overlaps = existingAppointments.Any(a =>
            a.StartTime < endTime && a.EndTime > startTime);

        if (overlaps)
        {
            var range = $"{FormatTime12(openTime)} a {FormatTime12(closeTime)}";
            return (false, $"⚠️ Esa hora ya está ocupada. Elige otra dentro del horario *{range}*.");
        }

        return (true, null);
    }

    private static Domain.Enums.DayOfWeek MapDayOfWeek(System.DayOfWeek day) => day switch
    {
        System.DayOfWeek.Monday => Domain.Enums.DayOfWeek.Lunes,
        System.DayOfWeek.Tuesday => Domain.Enums.DayOfWeek.Martes,
        System.DayOfWeek.Wednesday => Domain.Enums.DayOfWeek.Miercoles,
        System.DayOfWeek.Thursday => Domain.Enums.DayOfWeek.Jueves,
        System.DayOfWeek.Friday => Domain.Enums.DayOfWeek.Viernes,
        System.DayOfWeek.Saturday => Domain.Enums.DayOfWeek.Sabado,
        System.DayOfWeek.Sunday => Domain.Enums.DayOfWeek.Domingo,
        _ => Domain.Enums.DayOfWeek.Lunes
    };

    private static DateTime? ParseDate(string input)
    {
        var lower = input.ToLower().Trim();

        if (lower == "hoy") return DateTime.Today;
        if (lower == "mañana" || lower == "manana") return DateTime.Today.AddDays(1);

        var relativeMatch = System.Text.RegularExpressions.Regex.Match(lower, @"pr[oó]ximo\s+(\w+)");
        if (relativeMatch.Success)
        {
            var dayName = relativeMatch.Groups[1].Value;
            var targetDay = dayName switch
            {
                "lunes" => System.DayOfWeek.Monday,
                "martes" => System.DayOfWeek.Tuesday,
                "miércoles" or "miercoles" => System.DayOfWeek.Wednesday,
                "jueves" => System.DayOfWeek.Thursday,
                "viernes" => System.DayOfWeek.Friday,
                _ => (System.DayOfWeek?)null
            };
            if (targetDay.HasValue)
            {
                var daysUntil = ((int)targetDay.Value - (int)DateTime.Today.DayOfWeek + 7) % 7;
                if (daysUntil == 0) daysUntil = 7;
                return DateTime.Today.AddDays(daysUntil);
            }
        }

        var dayOnly = lower switch
        {
            "lunes" => System.DayOfWeek.Monday,
            "martes" => System.DayOfWeek.Tuesday,
            "miércoles" or "miercoles" => System.DayOfWeek.Wednesday,
            "jueves" => System.DayOfWeek.Thursday,
            "viernes" => System.DayOfWeek.Friday,
            "sábado" or "sabado" => System.DayOfWeek.Saturday,
            "domingo" => System.DayOfWeek.Sunday,
            _ => (System.DayOfWeek?)null
        };

        if (dayOnly.HasValue)
        {
            var daysUntil = ((int)dayOnly.Value - (int)DateTime.Today.DayOfWeek + 7) % 7;
            if (daysUntil == 0) daysUntil = 7;
            return DateTime.Today.AddDays(daysUntil);
        }

        var monthMap = new Dictionary<string, int>
        {
            {"enero",1},{"febrero",2},{"marzo",3},{"abril",4},{"mayo",5},{"junio",6},
            {"julio",7},{"agosto",8},{"septiembre",9},{"octubre",10},{"noviembre",11},{"diciembre",12}
        };

        var dateMatch = System.Text.RegularExpressions.Regex.Match(lower, @"(\d{1,2})\s+(?:de\s+)?(\w+)(?:\s+(?:de\s+)?(\d{4}))?");
        if (dateMatch.Success)
        {
            var day = int.Parse(dateMatch.Groups[1].Value);
            var monthStr = dateMatch.Groups[2].Value;
            if (monthMap.TryGetValue(monthStr, out var month))
            {
                var year = dateMatch.Groups[3].Success ? int.Parse(dateMatch.Groups[3].Value) : DateTime.Today.Year;
                try { return new DateTime(year, month, day); } catch { }
            }
        }

        if (DateTime.TryParse(input, out var parsed))
            return parsed;

        return null;
    }

    private static TimeSpan ParseTime(string input)
    {
        var lower = input.ToLower().Trim();

        if (lower.Contains("mediodía") || lower.Contains("medio dia") || lower.Contains("mediodia"))
            return new TimeSpan(12, 0, 0);

        if (lower.Contains("medianoche"))
            return new TimeSpan(0, 0, 0);

        var isTarde = lower.Contains("tarde");
        var isManana = lower.Contains("mañana") || lower.Contains("manana");

        var clean = lower.Replace(".", "").Replace(",", "").Replace("las", "").Replace("la", "").Replace("el", "").Trim();

        if (clean.Contains("am") || clean.Contains("pm"))
        {
            var isPm = clean.Contains("pm");
            clean = clean.Replace("am", "").Replace("pm", "").Trim();
            var numOnly = new string(clean.Where(c => char.IsDigit(c) || c == ':').ToArray());
            var parts = numOnly.Split(':');
            if (parts.Length == 2 && int.TryParse(parts[0], out var h) && int.TryParse(parts[1], out var m))
            {
                if (isPm && h != 12) h += 12;
                if (!isPm && h == 12) h = 0;
                return new TimeSpan(h, m, 0);
            }
            if (parts.Length == 1 && int.TryParse(parts[0], out var hOnly))
            {
                if (isPm && hOnly != 12) hOnly += 12;
                if (!isPm && hOnly == 12) hOnly = 0;
                return new TimeSpan(hOnly, 0, 0);
            }
        }

        if (isTarde || isManana)
        {
            var numOnly = new string(clean.Where(c => char.IsDigit(c) || c == ':').ToArray());
            if (int.TryParse(numOnly, out var hNat))
            {
                if (isTarde && hNat < 12) hNat += 12;
                if (isManana && hNat == 12) hNat = 0;
                return new TimeSpan(hNat, 0, 0);
            }
            if (isTarde) return new TimeSpan(14, 0, 0);
            return new TimeSpan(9, 0, 0);
        }

        var colonParts = clean.Split(':');
        if (colonParts.Length == 2 && int.TryParse(colonParts[0], out var hh) && int.TryParse(colonParts[1], out var mm))
        {
            return new TimeSpan(hh, mm, 0);
        }

        if (int.TryParse(new string(clean.Where(char.IsDigit).ToArray()), out var plainHour))
        {
            if (plainHour >= 1 && plainHour <= 7)
                return new TimeSpan(plainHour + 12, 0, 0);
            if (plainHour >= 8 && plainHour <= 12)
                return new TimeSpan(plainHour == 12 ? 0 : plainHour, 0, 0);
            return new TimeSpan(plainHour, 0, 0);
        }

        return new TimeSpan(9, 0, 0);
    }

    private static string FormatTime12(TimeSpan time)
    {
        var hours = time.Hours;
        var ampm = hours >= 12 ? "PM" : "AM";
        var h12 = hours % 12;
        if (h12 == 0) h12 = 12;
        return $"{h12}:{time.Minutes:D2} {ampm}";
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        return keywords.Any(k => text.Contains(k));
    }

    private static bool IsGreeting(string text)
    {
        return ContainsAny(text, "hola", "buenos dias", "buenas tardes", "buenas noches", "hello", "hi", "que tal", "qué tal");
    }

    private static bool IsCancel(string text)
    {
        return ContainsAny(text, "cancelar", "cancel", "salir", "exit", "reiniciar", "reset");
    }

    private static BookingStateData LoadState(string? json)
    {
        if (string.IsNullOrEmpty(json))
            return new BookingStateData();

        try
        {
            return JsonSerializer.Deserialize<BookingStateData>(json) ?? new BookingStateData();
        }
        catch
        {
            return new BookingStateData();
        }
    }

    private static void SaveState(Conversation conversation, BookingStateData state)
    {
        conversation.BookingState = JsonSerializer.Serialize(state);
    }

    private static void ResetState(Conversation conversation)
    {
        conversation.BookingState = null;
    }
}
