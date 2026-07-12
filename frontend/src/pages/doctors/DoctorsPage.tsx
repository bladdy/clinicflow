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
import type { Doctor } from '../../types';
import type { CreateDoctorFormData, UpdateDoctorFormData } from './doctorSchema';
import { createDoctorSchema, updateDoctorSchema } from './doctorSchema';

export function DoctorsPage() {
  const [page, setPage] = useState(1);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [editingDoctor, setEditingDoctor] = useState<Doctor | null>(null);
  const [deletingDoctor, setDeletingDoctor] = useState<Doctor | null>(null);

  const { data, isLoading } = usePagedQuery<Doctor>('doctors', '/doctors', { page, pageSize: 20 });
  const createMutation = useCreateMutation<CreateDoctorFormData, Doctor>('doctors', '/doctors');
  const updateMutation = useUpdateMutation<UpdateDoctorFormData, Doctor>('doctors', '/doctors');
  const deleteMutation = useDeleteMutation('doctors', '/doctors');

  const createForm = useForm<CreateDoctorFormData>({
    resolver: zodResolver(createDoctorSchema),
    defaultValues: {
      email: '',
      firstName: '',
      lastName: '',
      phone: '',
      specialty: '',
      licenseNumber: '',
      bio: '',
      photoUrl: '',
      color: '',
    },
  });

  const editForm = useForm<UpdateDoctorFormData>({
    resolver: zodResolver(updateDoctorSchema),
  });

  const handleCreate = async (formData: CreateDoctorFormData) => {
    await createMutation.mutateAsync(formData);
    setShowCreateModal(false);
    createForm.reset();
  };

  const openEdit = (doctor: Doctor) => {
    setEditingDoctor(doctor);
    editForm.reset({
      specialty: doctor.specialty,
      licenseNumber: doctor.licenseNumber ?? '',
      bio: doctor.bio ?? '',
      photoUrl: doctor.photoUrl ?? '',
      color: doctor.color ?? '',
    });
    setShowEditModal(true);
  };

  const handleUpdate = async (formData: UpdateDoctorFormData) => {
    if (!editingDoctor) return;
    await updateMutation.mutateAsync({ id: editingDoctor.id, data: formData });
    setShowEditModal(false);
    setEditingDoctor(null);
  };

  const handleDelete = async () => {
    if (!deletingDoctor) return;
    await deleteMutation.mutateAsync(deletingDoctor.id);
    setShowDeleteConfirm(false);
    setDeletingDoctor(null);
  };

  const columns = [
    { key: 'fullName', header: 'Nombre', render: (d: Doctor) => d.fullName || '—' },
    { key: 'email', header: 'Email', render: (d: Doctor) => d.email || '—' },
    { key: 'specialty', header: 'Especialidad', render: (d: Doctor) => d.specialty || '—' },
    { key: 'licenseNumber', header: 'Cédula Profesional', render: (d: Doctor) => d.licenseNumber || '—' },
    { key: 'color', header: 'Color', render: (d: Doctor) => d.color ? (
      <span className="inline-flex items-center gap-1.5">
        <span className="h-3 w-3 rounded-full border border-gray-200" style={{ backgroundColor: d.color }} />
        {d.color}
      </span>
    ) : '—' },
    {
      key: 'actions',
      header: 'Acciones',
      render: (d: Doctor) => (
        <div className="flex gap-2">
          <Button variant="ghost" size="sm" onClick={() => openEdit(d)}>
            <Pencil className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="sm" onClick={() => { setDeletingDoctor(d); setShowDeleteConfirm(true); }}>
            <Trash2 className="h-4 w-4 text-red-600" />
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Doctores</h1>
        <Button onClick={() => setShowCreateModal(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Nuevo Doctor
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

      <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="Nuevo Doctor" size="lg">
        <form onSubmit={createForm.handleSubmit(handleCreate)} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input label="Nombre" error={createForm.formState.errors.firstName?.message} {...createForm.register('firstName')} />
            <Input label="Apellido" error={createForm.formState.errors.lastName?.message} {...createForm.register('lastName')} />
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input label="Email" type="email" error={createForm.formState.errors.email?.message} {...createForm.register('email')} />
            <Input label="Teléfono" {...createForm.register('phone')} />
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input label="Especialidad" error={createForm.formState.errors.specialty?.message} {...createForm.register('specialty')} />
            <Input label="Cédula Profesional" {...createForm.register('licenseNumber')} />
          </div>
          <Input label="Foto URL" {...createForm.register('photoUrl')} />
          <div className="space-y-1">
            <label className="block text-sm font-medium text-gray-700">Color del calendario</label>
            <input
              type="color"
              className="h-10 w-20 rounded-lg border border-gray-300 cursor-pointer"
              {...createForm.register('color')}
            />
          </div>
          <div className="space-y-1">
            <label className="block text-sm font-medium text-gray-700">Biografía</label>
            <textarea
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              rows={3}
              {...createForm.register('bio')}
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" type="button" onClick={() => setShowCreateModal(false)}>Cancelar</Button>
            <Button type="submit" isLoading={createMutation.isPending}>Guardar</Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={showEditModal} onClose={() => setShowEditModal(false)} title="Editar Doctor" size="lg">
        <div className="mb-4 text-sm text-gray-500">
          Editando: <strong>{editingDoctor?.fullName}</strong> ({editingDoctor?.email})
        </div>
        <form onSubmit={editForm.handleSubmit(handleUpdate)} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input label="Especialidad" error={editForm.formState.errors.specialty?.message} {...editForm.register('specialty')} />
            <Input label="Cédula Profesional" {...editForm.register('licenseNumber')} />
          </div>
          <Input label="Foto URL" {...editForm.register('photoUrl')} />
          <div className="space-y-1">
            <label className="block text-sm font-medium text-gray-700">Color del calendario</label>
            <input
              type="color"
              className="h-10 w-20 rounded-lg border border-gray-300 cursor-pointer"
              {...editForm.register('color')}
            />
          </div>
          <div className="space-y-1">
            <label className="block text-sm font-medium text-gray-700">Biografía</label>
            <textarea
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              rows={3}
              {...editForm.register('bio')}
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" type="button" onClick={() => setShowEditModal(false)}>Cancelar</Button>
            <Button type="submit" isLoading={updateMutation.isPending}>Actualizar</Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={showDeleteConfirm} onClose={() => setShowDeleteConfirm(false)} title="Eliminar Doctor" size="sm">
        <p className="text-sm text-gray-600 mb-4">
          ¿Estás seguro de que deseas eliminar al doctor <strong>{deletingDoctor?.fullName}</strong>? Esta acción no se puede deshacer.
        </p>
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setShowDeleteConfirm(false)}>Cancelar</Button>
          <Button variant="danger" onClick={handleDelete} isLoading={deleteMutation.isPending}>Eliminar</Button>
        </div>
      </Modal>
    </div>
  );
}
