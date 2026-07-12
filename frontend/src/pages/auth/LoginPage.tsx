import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { z } from 'zod';

import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { useAuth } from '../../store/authStore';
import { authService } from '../../services/auth.service';

const loginSchema = z.object({
  email: z.string().email(),
  password: z.string().min(6),
});

type LoginForm = z.infer<typeof loginSchema>;

export function LoginPage() {
  const { login } = useAuth();
  const [error, setError] = useState('');
  const { register, handleSubmit, formState: { errors, isSubmitting } } = useForm<LoginForm>();

  const onSubmit = async (data: LoginForm) => {
    try {
      setError('');
      const result = await authService.login(data);
      login(result.token, result.refreshToken, result.user);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al iniciar sesión');
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-5">
        <div className="text-center">
          <h1 className="text-3xl font-bold text-gray-900">DentalBot AI</h1>
          <p className="mt-2 text-gray-600">Inicia sesión en tu cuenta</p>
        </div>
        <form onSubmit={handleSubmit(onSubmit)} className="mt-8 space-y-5 bg-white p-5 sm:p-8 rounded-xl shadow-sm border border-gray-200">
          {error && <div className="bg-red-50 text-red-700 p-3 rounded-lg text-sm">{error}</div>}
          <Input
            label="Correo electrónico"
            type="email"
            error={errors.email?.message}
            {...register('email', { required: 'El email es requerido' })}
          />
          <Input
            label="Contraseña"
            type="password"
            error={errors.password?.message}
            {...register('password', { required: 'La contraseña es requerida' })}
          />
          <Button type="submit" isLoading={isSubmitting} className="w-full">
            Iniciar Sesión
          </Button>
          <p className="text-center text-sm text-gray-600">
            ¿No tienes cuenta?{' '}
            <Link to="/register" className="text-blue-600 hover:text-blue-700 font-medium">
              Regístrate
            </Link>
          </p>
        </form>
      </div>
    </div>
  );
}
