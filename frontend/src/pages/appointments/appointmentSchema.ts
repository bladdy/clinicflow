import { z } from 'zod';

export const appointmentSchema = z.object({
  patientId: z.string().min(1, 'Selecciona un paciente'),
  doctorId: z.string().min(1, 'Selecciona un doctor'),
  serviceId: z.string().min(1, 'Selecciona un servicio'),
  appointmentDate: z.string().min(1, 'La fecha es requerida'),
  startTime: z.string().min(1, 'La hora de inicio es requerida'),
  endTime: z.string().min(1, 'La hora de fin es requerida'),
  notes: z.string().optional(),
  reason: z.string().optional(),
});

export type AppointmentFormData = z.infer<typeof appointmentSchema>;
