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
import type { Patient } from '../../types';
import type { PatientFormData } from './patientSchema';
import { patientSchema } from './patientSchema';
import { formatPhone } from '../../utils/formatters';

export function PatientsPage() {
  const [page, setPage] = useState(1);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [editingPatient, setEditingPatient] = useState<Patient | null>(null);
  const [deletingPatient, setDeletingPatient] = useState<Patient | null>(null);

  const { data, isLoading } = usePagedQuery<Patient>('patients', '/patients', { page, pageSize: 20 });
  const createMutation = useCreateMutation<PatientFormData, Patient>('patients', '/patients');
  const updateMutation = useUpdateMutation<PatientFormData, Patient>('patients', '/patients');
  const deleteMutation = useDeleteMutation('patients', '/patients');

  const createForm = useForm<PatientFormData>({
    resolver: zodResolver(patientSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      phone: '',
      email: '',
      dateOfBirth: '',
      gender: '',
      address: '',
      notes: '',
      medicalHistory: '',
    },
  });

  const editForm = useForm<PatientFormData>({
    resolver: zodResolver(patientSchema),
  });

  const handleCreate = async (formData: PatientFormData) => {
    await createMutation.mutateAsync(formData);
    setShowCreateModal(false);
    createForm.reset();
  };

  const openEdit = (patient: Patient) => {
    setEditingPatient(patient);
    editForm.reset({
      firstName: patient.firstName,
      lastName: patient.lastName,
      phone: patient.phone,
      email: patient.email ?? '',
      dateOfBirth: patient.dateOfBirth ? patient.dateOfBirth.split('T')[0] : '',
      gender: patient.gender ?? '',
      address: patient.address ?? '',
      notes: patient.notes ?? '',
      medicalHistory: patient.medicalHistory ?? '',
    });
    setShowEditModal(true);
  };

  const handleUpdate = async (formData: PatientFormData) => {
    if (!editingPatient) return;
    await updateMutation.mutateAsync({ id: editingPatient.id, data: formData });
    setShowEditModal(false);
    setEditingPatient(null);
  };

  const handleDelete = async () => {
    if (!deletingPatient) return;
    await deleteMutation.mutateAsync(deletingPatient.id);
    setShowDeleteConfirm(false);
    setDeletingPatient(null);
  };

  const columns = [
    { key: 'firstName', header: 'Nombre', render: (p: Patient) => `${p.firstName} ${p.lastName}` },
    { key: 'phone', header: 'Teléfono', render: (p: Patient) => formatPhone(p.phone) },
    { key: 'email', header: 'Email', render: (p: Patient) => p.email || '—' },
    { key: 'gender', header: 'Género', render: (p: Patient) => p.gender === 'Male' ? 'Masculino' : p.gender === 'Female' ? 'Femenino' : p.gender === 'Other' ? 'Otro' : '—' },
    {
      key: 'actions',
      header: 'Acciones',
      render: (p: Patient) => (
        <div className="flex gap-2">
          <Button variant="ghost" size="sm" onClick={() => openEdit(p)}>
            <Pencil className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="sm" onClick={() => { setDeletingPatient(p); setShowDeleteConfirm(true); }}>
            <Trash2 className="h-4 w-4 text-red-600" />
          </Button>
        </div>
      ),
    },
  ];

  const formFields = (form: ReturnType<typeof useForm<PatientFormData>>) => (
    <div className="space-y-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Input label="Nombre" error={form.formState.errors.firstName?.message} {...form.register('firstName')} />
        <Input label="Apellido" error={form.formState.errors.lastName?.message} {...form.register('lastName')} />
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Input label="Teléfono" error={form.formState.errors.phone?.message} {...form.register('phone')} />
        <Input label="Email" type="email" error={form.formState.errors.email?.message} {...form.register('email')} />
      </div>
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Input label="Fecha de Nacimiento" type="date" {...form.register('dateOfBirth')} />
        <div className="space-y-1">
          <label className="block text-sm font-medium text-gray-700">Género</label>
          <select
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
            {...form.register('gender')}
          >
            <option value="">Seleccionar</option>
            <option value="Male">Masculino</option>
            <option value="Female">Femenino</option>
            <option value="Other">Otro</option>
          </select>
        </div>
      </div>
      <Input label="Dirección" {...form.register('address')} />
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <div className="space-y-1">
          <label className="block text-sm font-medium text-gray-700">Notas</label>
          <textarea
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
            rows={2}
            {...form.register('notes')}
          />
        </div>
        <div className="space-y-1">
          <label className="block text-sm font-medium text-gray-700">Historial Médico</label>
          <textarea
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
            rows={2}
            {...form.register('medicalHistory')}
          />
        </div>
      </div>
    </div>
  );

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Pacientes</h1>
        <Button onClick={() => setShowCreateModal(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Nuevo Paciente
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

      <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="Nuevo Paciente">
        <form onSubmit={createForm.handleSubmit(handleCreate)} className="space-y-4">
          {formFields(createForm)}
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" type="button" onClick={() => setShowCreateModal(false)}>Cancelar</Button>
            <Button type="submit" isLoading={createMutation.isPending}>Guardar</Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={showEditModal} onClose={() => setShowEditModal(false)} title="Editar Paciente">
        <form onSubmit={editForm.handleSubmit(handleUpdate)} className="space-y-4">
          {formFields(editForm)}
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" type="button" onClick={() => setShowEditModal(false)}>Cancelar</Button>
            <Button type="submit" isLoading={updateMutation.isPending}>Actualizar</Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={showDeleteConfirm} onClose={() => setShowDeleteConfirm(false)} title="Eliminar Paciente" size="sm">
        <p className="text-sm text-gray-600 mb-4">
          ¿Estás seguro de que deseas eliminar a <strong>{deletingPatient?.firstName} {deletingPatient?.lastName}</strong>? Esta acción no se puede deshacer.
        </p>
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setShowDeleteConfirm(false)}>Cancelar</Button>
          <Button variant="danger" onClick={handleDelete} isLoading={deleteMutation.isPending}>Eliminar</Button>
        </div>
      </Modal>
    </div>
  );
}
