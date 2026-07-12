import { z } from 'zod';

export const createDoctorSchema = z.object({
  email: z.string().email('Email inválido'),
  firstName: z.string().min(1, 'El nombre es requerido'),
  lastName: z.string().min(1, 'El apellido es requerido'),
  phone: z.string().optional(),
  specialty: z.string().min(1, 'La especialidad es requerida'),
  licenseNumber: z.string().optional(),
  bio: z.string().optional(),
  photoUrl: z.string().optional(),
  color: z.string().optional(),
});

export const updateDoctorSchema = z.object({
  specialty: z.string().min(1, 'La especialidad es requerida'),
  licenseNumber: z.string().optional(),
  bio: z.string().optional(),
  photoUrl: z.string().optional(),
  color: z.string().optional(),
});

export type CreateDoctorFormData = z.infer<typeof createDoctorSchema>;
export type UpdateDoctorFormData = z.infer<typeof updateDoctorSchema>;
