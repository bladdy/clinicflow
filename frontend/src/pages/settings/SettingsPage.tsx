import { useForm } from 'react-hook-form';
import { useAuth } from '../../store/authStore';
import { useDetailQuery, useUpdateMutation } from '../../hooks/useApi';
import type { Company } from '../../types';
import { Card, CardHeader, CardContent } from '../../components/ui/Card';
import { Input } from '../../components/ui/Input';
import { Button } from '../../components/ui/Button';

export function SettingsPage() {
  const { user } = useAuth();
  const { data: company, isLoading: isLoadingCompany } = useDetailQuery<Company>('company', '/companies', user?.companyId ?? '');
  const updateMutation = useUpdateMutation<Partial<Company>, Company>('company', '/companies');

  const { register, handleSubmit, reset } = useForm<Company>({
    values: company,
  });

  const handleSave = async (formData: Company) => {
    if (!company) return;
    await updateMutation.mutateAsync({ id: company.id, data: formData });
    reset(formData);
  };

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Configuración</h1>

      <Card>
        <CardHeader>
          <h2 className="text-lg font-semibold">Perfil de Usuario</h2>
        </CardHeader>
        <CardContent>
          <dl className="space-y-4">
            <div>
              <dt className="text-sm text-gray-500">Nombre</dt>
              <dd className="text-sm font-medium text-gray-900">{user?.firstName} {user?.lastName}</dd>
            </div>
            <div>
              <dt className="text-sm text-gray-500">Email</dt>
              <dd className="text-sm font-medium text-gray-900">{user?.email}</dd>
            </div>
            <div>
              <dt className="text-sm text-gray-500">Rol</dt>
              <dd className="text-sm font-medium text-gray-900">{user?.role}</dd>
            </div>
            {user?.phone && (
              <div>
                <dt className="text-sm text-gray-500">Teléfono</dt>
                <dd className="text-sm font-medium text-gray-900">{user?.phone}</dd>
              </div>
            )}
          </dl>
        </CardContent>
      </Card>

      {user?.companyId && (
        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold">Información de la Empresa</h2>
          </CardHeader>
          <CardContent>
            {isLoadingCompany ? (
              <div className="flex items-center justify-center py-8">
                <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
              </div>
            ) : (
              <form onSubmit={handleSubmit(handleSave)} className="space-y-4">
                <Input label="Nombre" {...register('name')} />
                <Input label="Teléfono" {...register('phone')} />
                <Input label="Email" type="email" {...register('email')} />
                <Input label="Dirección" {...register('address')} />
                <Input label="Sitio Web" {...register('website')} />
                <div className="flex justify-end pt-2">
                  <Button type="submit" isLoading={updateMutation.isPending}>Guardar Cambios</Button>
                </div>
              </form>
            )}
          </CardContent>
        </Card>
      )}
    </div>
  );
}
