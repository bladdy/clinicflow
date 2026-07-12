import { z } from 'zod';

export const patientSchema = z.object({
  firstName: z.string().min(1, 'El nombre es requerido'),
  lastName: z.string().min(1, 'El apellido es requerido'),
  email: z.string().email('Email inválido').optional().or(z.literal('')),
  phone: z.string().min(10, 'El teléfono debe tener 10 dígitos'),
  dateOfBirth: z.string().optional(),
  gender: z.string().optional(),
  address: z.string().optional(),
  notes: z.string().optional(),
  medicalHistory: z.string().optional(),
  branchId: z.string().optional(),
});

export type PatientFormData = z.infer<typeof patientSchema>;
