import { useState } from 'react';
import { Plus, Pencil, Trash2 } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { DataTable } from '../../components/ui/DataTable';
import { Input } from '../../components/ui/Input';
import { Modal } from '../../components/ui/Modal';
import { usePagedQuery, useCreateMutation, useUpdateMutation, useDeleteMutation } from '../../hooks/useApi';
import type { Service } from '../../types';
import type { ServiceFormData } from './serviceSchema';
import { serviceSchema } from './serviceSchema';
import { formatCurrency } from '../../utils/formatters';

export function ServicesPage() {
  const [page, setPage] = useState(1);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [editingService, setEditingService] = useState<Service | null>(null);
  const [deletingService, setDeletingService] = useState<Service | null>(null);

  const { data, isLoading } = usePagedQuery<Service>('services', '/services', { page, pageSize: 10 });
  const createMutation = useCreateMutation<ServiceFormData, Service>('services', '/services');
  const updateMutation = useUpdateMutation<ServiceFormData, Service>('services', '/services');
  const deleteMutation = useDeleteMutation('services', '/services');

  const createForm = useForm<ServiceFormData>({
    resolver: zodResolver(serviceSchema),
    defaultValues: {
      name: '',
      description: '',
      durationMinutes: 30,
      price: 0,
      category: '',
    },
  });

  const editForm = useForm<ServiceFormData>({
    resolver: zodResolver(serviceSchema),
  });

  const handleCreate = async (formData: ServiceFormData) => {
    await createMutation.mutateAsync(formData);
    setShowCreateModal(false);
    createForm.reset();
  };

  const openEdit = (service: Service) => {
    setEditingService(service);
    editForm.reset({
      name: service.name,
      description: service.description ?? '',
      durationMinutes: service.durationMinutes,
      price: service.price,
      category: service.category ?? '',
    });
    setShowEditModal(true);
  };

  const handleUpdate = async (formData: ServiceFormData) => {
    if (!editingService) return;
    await updateMutation.mutateAsync({ id: editingService.id, data: formData });
    setShowEditModal(false);
    setEditingService(null);
  };

  const handleDelete = async () => {
    if (!deletingService) return;
    await deleteMutation.mutateAsync(deletingService.id);
    setShowDeleteConfirm(false);
    setDeletingService(null);
  };

  const columns = [
    { key: 'name', header: 'Nombre' },
    { key: 'durationMinutes', header: 'Duración', render: (s: Service) => `${s.durationMinutes} min` },
    { key: 'price', header: 'Precio', render: (s: Service) => formatCurrency(s.price) },
    { key: 'category', header: 'Categoría', render: (s: Service) => s.category || '—' },
    { key: 'isActive', header: 'Estado', render: (s: Service) => (
      <span className={`inline-flex px-2 py-1 rounded-full text-xs font-medium ${s.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}`}>
        {s.isActive ? 'Activo' : 'Inactivo'}
      </span>
    )},
    {
      key: 'actions',
      header: 'Acciones',
      render: (s: Service) => (
        <div className="flex gap-2">
          <Button variant="ghost" size="sm" onClick={() => openEdit(s)}>
            <Pencil className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="sm" onClick={() => { setDeletingService(s); setShowDeleteConfirm(true); }}>
            <Trash2 className="h-4 w-4 text-red-600" />
          </Button>
        </div>
      ),
    },
  ];

  const formFields = (form: ReturnType<typeof useForm<ServiceFormData>>) => (
    <div className="space-y-4">
      <Input label="Nombre" error={form.formState.errors.name?.message} {...form.register('name')} />
      <Input label="Descripción" {...form.register('description')} />
      <Input label="Duración (minutos)" type="number" error={form.formState.errors.durationMinutes?.message} {...form.register('durationMinutes', { valueAsNumber: true })} />
      <Input label="Precio" type="number" step="0.01" error={form.formState.errors.price?.message} {...form.register('price', { valueAsNumber: true })} />
      <Input label="Categoría" {...form.register('category')} />
    </div>
  );

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Servicios</h1>
        <Button onClick={() => setShowCreateModal(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Nuevo Servicio
        </Button>
      </div>
      <Card>
        <DataTable
          columns={columns}
          data={data?.items ?? []}
          page={data?.page ?? 1}
          totalPages={data?.totalPages ?? 1}
          onPageChange={setPage}
          isLoading={isLoading}
        />
      </Card>

      <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="Nuevo Servicio">
        <form onSubmit={createForm.handleSubmit(handleCreate)} className="space-y-4">
          {formFields(createForm)}
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" type="button" onClick={() => setShowCreateModal(false)}>Cancelar</Button>
            <Button type="submit" isLoading={createMutation.isPending}>Guardar</Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={showEditModal} onClose={() => setShowEditModal(false)} title="Editar Servicio">
        <form onSubmit={editForm.handleSubmit(handleUpdate)} className="space-y-4">
          {formFields(editForm)}
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" type="button" onClick={() => setShowEditModal(false)}>Cancelar</Button>
            <Button type="submit" isLoading={updateMutation.isPending}>Actualizar</Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={showDeleteConfirm} onClose={() => setShowDeleteConfirm(false)} title="Eliminar Servicio" size="sm">
        <p className="text-sm text-gray-600 mb-4">
          ¿Estás seguro de que deseas eliminar el servicio <strong>{deletingService?.name}</strong>? Esta acción no se puede deshacer.
        </p>
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setShowDeleteConfirm(false)}>Cancelar</Button>
          <Button variant="danger" onClick={handleDelete} isLoading={deleteMutation.isPending}>Eliminar</Button>
        </div>
      </Modal>
    </div>
  );
}
