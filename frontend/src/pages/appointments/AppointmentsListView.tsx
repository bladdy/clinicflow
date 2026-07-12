import { useState } from 'react';
import { Pencil, Trash2 } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { DataTable } from '../../components/ui/DataTable';
import { Modal } from '../../components/ui/Modal';
import { usePagedQuery, useUpdateMutation, useDeleteMutation } from '../../hooks/useApi';
import { useDebounce } from '../../hooks/useDebounce';
import type { Appointment, Patient, Doctor, Service } from '../../types';
import { statusColors, statusLabels, formatDate, formatTime } from '../../utils/formatters';

interface ListViewProps {
  doctors: Doctor[];
  patients: Patient[];
  services: Service[];
  onEdit?: (appointment: Appointment) => void;
}

export function AppointmentsListView({ doctors, patients, services, onEdit }: ListViewProps) {
  const [page, setPage] = useState(1);
  const [filterDoctor, setFilterDoctor] = useState('');
  const [searchPatient, setSearchPatient] = useState('');
  const [filterService, setFilterService] = useState('');
  const [filterStatus, setFilterStatus] = useState('');
  const [showEditModal, setShowEditModal] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [editingAppointment, setEditingAppointment] = useState<Appointment | null>(null);
  const [deletingAppointment, setDeletingAppointment] = useState<Appointment | null>(null);

  const debouncedPatient = useDebounce(searchPatient);

  const { data, isLoading } = usePagedQuery<Appointment>('appointments', '/appointments', {
    page,
    pageSize: 10,
    doctorId: filterDoctor || undefined,
    patientName: debouncedPatient || undefined,
    serviceId: filterService || undefined,
    status: filterStatus || undefined,
  });

  const updateMutation = useUpdateMutation<Partial<Appointment>, Appointment>('appointments', '/appointments');
  const deleteMutation = useDeleteMutation('appointments', '/appointments');

  const editForm = useForm<{ status: string; notes: string }>({
    defaultValues: { status: '', notes: '' },
  });

  const openEdit = (appointment: Appointment) => {
    setEditingAppointment(appointment);
    editForm.reset({
      status: appointment.status,
      notes: appointment.notes ?? '',
    });
    setShowEditModal(true);
  };

  const handleUpdate = async (formData: { status: string; notes: string }) => {
    if (!editingAppointment) return;
    await updateMutation.mutateAsync({ id: editingAppointment.id, data: formData });
    setShowEditModal(false);
    setEditingAppointment(null);
  };

  const handleDelete = async () => {
    if (!deletingAppointment) return;
    await deleteMutation.mutateAsync(deletingAppointment.id);
    setShowDeleteConfirm(false);
    setDeletingAppointment(null);
  };

  const hasFilters = filterDoctor || searchPatient || filterService || filterStatus;
  const clearFilters = () => {
    setFilterDoctor('');
    setSearchPatient('');
    setFilterService('');
    setFilterStatus('');
    setPage(1);
  };

  const columns = [
    { key: 'appointmentDate', header: 'Fecha', render: (a: Appointment) => formatDate(a.appointmentDate) },
    { key: 'startTime', header: 'Hora', render: (a: Appointment) => `${formatTime(a.startTime)} - ${formatTime(a.endTime)}` },
    { key: 'patient', header: 'Paciente', render: (a: Appointment) => a.patientName || '—' },
    { key: 'doctor', header: 'Doctor', render: (a: Appointment) => a.doctorName || '—' },
    { key: 'service', header: 'Servicio', render: (a: Appointment) => a.serviceName || '—' },
    { key: 'status', header: 'Estado', render: (a: Appointment) => (
      <span className={`inline-flex px-2 py-1 rounded-full text-xs font-medium ${statusColors[a.status] ?? ''}`}>
        {statusLabels[a.status] ?? a.status}
      </span>
    )},
    {
      key: 'actions',
      header: 'Acciones',
      render: (a: Appointment) => (
        <div className="flex gap-2">
          <Button variant="ghost" size="sm" onClick={() => openEdit(a)}>
            <Pencil className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="sm" onClick={() => { setDeletingAppointment(a); setShowDeleteConfirm(true); }}>
            <Trash2 className="h-4 w-4 text-red-600" />
          </Button>
        </div>
      ),
    },
  ];

  return (
    <>
      <Card>
        <div className="px-6 py-4 grid grid-cols-2 sm:grid-cols-4 gap-4">
          <div className="space-y-1">
            <label className="block text-xs font-medium text-gray-500">Buscar paciente</label>
            <input
              type="text"
              placeholder="Nombre del paciente..."
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              value={searchPatient}
              onChange={(e) => { setSearchPatient(e.target.value); setPage(1); }}
            />
          </div>
          <div className="space-y-1">
            <label className="block text-xs font-medium text-gray-500">Doctor</label>
            <select
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              value={filterDoctor}
              onChange={(e) => { setFilterDoctor(e.target.value); setPage(1); }}
            >
              <option value="">Todos</option>
              {doctors.map((d) => (
                <option key={d.id} value={d.id}>{d.fullName}</option>
              ))}
            </select>
          </div>
          <div className="space-y-1">
            <label className="block text-xs font-medium text-gray-500">Servicio</label>
            <select
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              value={filterService}
              onChange={(e) => { setFilterService(e.target.value); setPage(1); }}
            >
              <option value="">Todos</option>
              {services.map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
          </div>
          <div className="space-y-1">
            <label className="block text-xs font-medium text-gray-500">Estado</label>
            <select
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              value={filterStatus}
              onChange={(e) => { setFilterStatus(e.target.value); setPage(1); }}
            >
              <option value="">Todos</option>
              <option value="Scheduled">Programada</option>
              <option value="Confirmed">Confirmada</option>
              <option value="InProgress">En Progreso</option>
              <option value="Completed">Completada</option>
              <option value="Cancelled">Cancelada</option>
              <option value="NoShow">No Asistió</option>
            </select>
          </div>
        </div>
        {hasFilters && (
          <div className="px-6 pb-4">
            <Button variant="ghost" size="sm" onClick={clearFilters}>
              Limpiar filtros
            </Button>
          </div>
        )}
      </Card>
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

      <Modal isOpen={showEditModal} onClose={() => setShowEditModal(false)} title="Editar Cita">
        <form onSubmit={editForm.handleSubmit(handleUpdate)} className="space-y-4">
          <div className="space-y-1">
            <label className="block text-sm font-medium text-gray-700">Estado</label>
            <select
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              {...editForm.register('status')}
            >
              <option value="Scheduled">Programada</option>
              <option value="Confirmed">Confirmada</option>
              <option value="InProgress">En Progreso</option>
              <option value="Completed">Completada</option>
              <option value="Cancelled">Cancelada</option>
              <option value="NoShow">No Asistió</option>
            </select>
          </div>
          <div className="space-y-1">
            <label className="block text-sm font-medium text-gray-700">Notas</label>
            <textarea
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              rows={2}
              {...editForm.register('notes')}
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" type="button" onClick={() => setShowEditModal(false)}>Cancelar</Button>
            <Button type="submit" isLoading={updateMutation.isPending}>Actualizar</Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={showDeleteConfirm} onClose={() => setShowDeleteConfirm(false)} title="Eliminar Cita" size="sm">
        <p className="text-sm text-gray-600 mb-4">
          ¿Estás seguro de que deseas eliminar esta cita? Esta acción no se puede deshacer.
        </p>
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setShowDeleteConfirm(false)}>Cancelar</Button>
          <Button variant="danger" onClick={handleDelete} isLoading={deleteMutation.isPending}>Eliminar</Button>
        </div>
      </Modal>
    </>
  );
}
