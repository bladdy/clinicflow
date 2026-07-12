import { z } from 'zod';

export const serviceSchema = z.object({
  name: z.string().min(1, 'El nombre es requerido'),
  description: z.string().optional(),
  durationMinutes: z.number().min(15, 'Mínimo 15 minutos').max(480, 'Máximo 8 horas'),
  price: z.number().min(0, 'El precio no puede ser negativo'),
  category: z.string().optional(),
});

export type ServiceFormData = z.infer<typeof serviceSchema>;
