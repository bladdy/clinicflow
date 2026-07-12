import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { useAuth } from '../../store/authStore';
import { authService } from '../../services/auth.service';

const registerSchema = z.object({
  firstName: z.string().min(1),
  lastName: z.string().min(1),
  email: z.string().email(),
  phone: z.string().optional(),
  password: z.string().min(6),
  confirmPassword: z.string(),
}).refine((data) => data.password === data.confirmPassword, {
  message: 'Las contraseñas no coinciden',
  path: ['confirmPassword'],
});

type RegisterForm = z.infer<typeof registerSchema>;

export function RegisterPage() {
  const { login } = useAuth();
  const [error, setError] = useState('');
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<RegisterForm>();

  const onSubmit = async (data: RegisterForm) => {
    try {
      setError('');
      const result = await authService.register(data);
      login(result.token, result.refreshToken, result.user);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al registrarse');
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-5">
        <div className="text-center">
          <h1 className="text-3xl font-bold text-gray-900">DentalBot AI</h1>
          <p className="mt-2 text-gray-600">Crea tu cuenta</p>
        </div>
        <form onSubmit={handleSubmit(onSubmit)} className="mt-8 space-y-5 bg-white p-5 sm:p-8 rounded-xl shadow-sm border border-gray-200">
          {error && <div className="bg-red-50 text-red-700 p-3 rounded-lg text-sm">{error}</div>}
          <div className="grid grid-cols-2 gap-4">
            <Input label="Nombre" error={errors.firstName?.message} {...register('firstName', { required: true })} />
            <Input label="Apellido" error={errors.lastName?.message} {...register('lastName', { required: true })} />
          </div>
          <Input label="Correo electrónico" type="email" error={errors.email?.message} {...register('email', { required: true })} />
          <Input label="Teléfono" type="tel" {...register('phone')} />
          <Input label="Contraseña" type="password" error={errors.password?.message} {...register('password', { required: true, minLength: 6 })} />
          <Input label="Confirmar Contraseña" type="password" error={errors.confirmPassword?.message} {...register('confirmPassword', { required: true })} />
          <Button type="submit" isLoading={isSubmitting} className="w-full">
            Crear Cuenta
          </Button>
          <p className="text-center text-sm text-gray-600">
            ¿Ya tienes cuenta?{' '}
            <Link to="/login" className="text-blue-600 hover:text-blue-700 font-medium">
              Inicia sesión
            </Link>
          </p>
        </form>
      </div>
    </div>
  );
}
