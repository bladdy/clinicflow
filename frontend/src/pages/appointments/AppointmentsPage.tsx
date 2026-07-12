import { useState, useMemo, useCallback } from 'react';
import { Plus, ChevronLeft, ChevronRight, Calendar, List, CalendarDays, CalendarRange, X } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { Modal } from '../../components/ui/Modal';
import { useCreateMutation, useUpdateMutation } from '../../hooks/useApi';
import { useAppointmentsView, useAppointmentDoctors, useAppointmentPatients, useAppointmentServices } from '../../hooks/useAppointments';
import { AppointmentsListView } from './AppointmentsListView';
import { AppointmentsCalendarView } from './AppointmentsCalendarView';
import type { AppointmentFormData } from './appointmentSchema';
import { appointmentSchema } from './appointmentSchema';
import type { Appointment, CalendarView } from '../../types';

const viewConfig: Record<CalendarView, { icon: typeof Calendar; label: string; fcView?: 'timeGridDay' | 'timeGridWeek' | 'dayGridMonth' | 'listWeek' }> = {
  list: { icon: List, label: 'Lista', fcView: undefined },
  day: { icon: CalendarDays, label: 'Día', fcView: 'timeGridDay' },
  week: { icon: CalendarRange, label: 'Semana', fcView: 'timeGridWeek' },
  month: { icon: Calendar, label: 'Mes', fcView: 'dayGridMonth' },
};

function navigateDate(current: Date, view: CalendarView, direction: -1 | 1): Date {
  const d = new Date(current);
  if (view === 'day') d.setDate(d.getDate() + direction);
  else if (view === 'week') d.setDate(d.getDate() + direction * 7);
  else if (view === 'month') d.setMonth(d.getMonth() + direction);
  return d;
}

function formatDateHeader(d: Date, view: CalendarView): string {
  if (view === 'day') {
    return d.toLocaleDateString('es-MX', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' });
  }
  if (view === 'week') {
    const start = new Date(d);
    start.setDate(start.getDate() - start.getDay());
    const end = new Date(start);
    end.setDate(end.getDate() + 6);
    const opts: Intl.DateTimeFormatOptions = { month: 'short', day: 'numeric' };
    return `${start.toLocaleDateString('es-MX', opts)} - ${end.toLocaleDateString('es-MX', { ...opts, year: 'numeric' })}`;
  }
  if (view === 'month') {
    return d.toLocaleDateString('es-MX', { year: 'numeric', month: 'long' });
  }
  return '';
}

export function AppointmentsPage() {
  const { view, changeView } = useAppointmentsView();
  const [currentDate, setCurrentDate] = useState(new Date());
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [editingAppointment, setEditingAppointment] = useState<Appointment | null>(null);
  const [filterDoctor, setFilterDoctor] = useState('');
  const [filterService, setFilterService] = useState('');
  const [filterStatus, setFilterStatus] = useState('');

  const { data: doctors = [] } = useAppointmentDoctors();
  const { data: patients = [] } = useAppointmentPatients();
  const { data: services = [] } = useAppointmentServices();
  const createMutation = useCreateMutation<AppointmentFormData, Appointment>('appointments', '/appointments');
  const updateMutation = useUpdateMutation<Partial<AppointmentFormData>, Appointment>('appointments', '/appointments');

  const calendarFilters = useMemo(() => ({
    doctorId: filterDoctor || undefined,
    serviceId: filterService || undefined,
    status: filterStatus || undefined,
  }), [filterDoctor, filterService, filterStatus]);

  const hasCalendarFilters = filterDoctor || filterService || filterStatus;
  const clearCalendarFilters = () => {
    setFilterDoctor('');
    setFilterService('');
    setFilterStatus('');
  };

  const createForm = useForm<AppointmentFormData>({
    resolver: zodResolver(appointmentSchema),
    defaultValues: {
      patientId: '',
      doctorId: '',
      serviceId: '',
      appointmentDate: '',
      startTime: '',
      endTime: '',
      notes: '',
      reason: '',
    },
  });

  const handleCreate = async (formData: AppointmentFormData) => {
    if (editingAppointment) {
      await updateMutation.mutateAsync({ id: editingAppointment.id, data: formData });
    } else {
      await createMutation.mutateAsync(formData);
    }
    setShowCreateModal(false);
    setEditingAppointment(null);
    createForm.reset();
  };

  const handlePrev = useCallback(() => setCurrentDate((d) => navigateDate(d, view, -1)), [view]);
  const handleNext = useCallback(() => setCurrentDate((d) => navigateDate(d, view, 1)), [view]);
  const handleToday = useCallback(() => setCurrentDate(new Date()), []);

  const handleDateClick = useCallback((date: Date) => {
    setEditingAppointment(null);
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    const dateStr = `${y}-${m}-${d}`;
    createForm.reset({
      appointmentDate: dateStr,
      startTime: `${String(date.getHours()).padStart(2, '0')}:${String(date.getMinutes()).padStart(2, '0')}`,
      endTime: '',
      patientId: '',
      doctorId: '',
      serviceId: '',
      notes: '',
      reason: '',
    });
    setShowCreateModal(true);
  }, [createForm]);

  const handleEventClick = useCallback((appointment: Appointment) => {
    const dateStr = appointment.appointmentDate.split('T')[0];
    setEditingAppointment(appointment);
    createForm.reset({
      patientId: appointment.patientId,
      doctorId: appointment.doctorId,
      serviceId: appointment.serviceId,
      appointmentDate: dateStr,
      startTime: appointment.startTime,
      endTime: appointment.endTime,
      notes: appointment.notes ?? '',
      reason: appointment.reason ?? '',
    });
    setShowCreateModal(true);
  }, [createForm]);

  const dateHeader = useMemo(() => {
    if (view === 'list') return null;
    return formatDateHeader(currentDate, view);
  }, [currentDate, view]);

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Citas</h1>
        <div className="flex items-center gap-3">
          <div className="flex rounded-lg border border-gray-200 overflow-hidden">
            {(Object.keys(viewConfig) as CalendarView[]).map((v) => {
              const cfg = viewConfig[v];
              const Icon = cfg.icon;
              return (
                <button
                  key={v}
                  onClick={() => { changeView(v); setCurrentDate(new Date()); }}
                  className={`flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium transition-colors ${
                    view === v
                      ? 'bg-blue-600 text-white'
                      : 'bg-white text-gray-600 hover:bg-gray-50'
                  }`}
                >
                  <Icon className="h-3.5 w-3.5" />
                  {cfg.label}
                </button>
              );
            })}
          </div>
          <Button onClick={() => { setEditingAppointment(null); createForm.reset(); setShowCreateModal(true); }}>
            <Plus className="h-4 w-4 mr-2" />
            Nueva Cita
          </Button>
        </div>
      </div>

      {view !== 'list' && (
        <>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Button variant="ghost" size="sm" onClick={handlePrev}>
                <ChevronLeft className="h-4 w-4" />
              </Button>
              <Button variant="ghost" size="sm" onClick={handleToday}>Hoy</Button>
              <Button variant="ghost" size="sm" onClick={handleNext}>
                <ChevronRight className="h-4 w-4" />
              </Button>
              <input
                type="date"
                value={`${currentDate.getFullYear()}-${String(currentDate.getMonth() + 1).padStart(2, '0')}-${String(currentDate.getDate()).padStart(2, '0')}`}
                onChange={(e) => {
                  if (e.target.value) setCurrentDate(new Date(e.target.value + 'T00:00:00'));
                }}
                className="rounded-lg border border-gray-300 px-2 py-1 text-xs focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              />
            </div>
            <h2 className="text-sm font-semibold text-gray-700 capitalize">{dateHeader}</h2>
          </div>
          <div className="flex items-center gap-3 flex-wrap">
            <select
              className="rounded-lg border border-gray-300 px-3 py-1.5 text-xs focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              value={filterDoctor}
              onChange={(e) => setFilterDoctor(e.target.value)}
            >
              <option value="">Doctor: Todos</option>
              {doctors.map((d) => (
                <option key={d.id} value={d.id}>{d.fullName}</option>
              ))}
            </select>
            <select
              className="rounded-lg border border-gray-300 px-3 py-1.5 text-xs focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              value={filterService}
              onChange={(e) => setFilterService(e.target.value)}
            >
              <option value="">Servicio: Todos</option>
              {services.map((s) => (
                <option key={s.id} value={s.id}>{s.name}</option>
              ))}
            </select>
            <select
              className="rounded-lg border border-gray-300 px-3 py-1.5 text-xs focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              value={filterStatus}
              onChange={(e) => setFilterStatus(e.target.value)}
            >
              <option value="">Estado: Todos</option>
              <option value="Scheduled">Programada</option>
              <option value="Confirmed">Confirmada</option>
              <option value="InProgress">En Progreso</option>
              <option value="Completed">Completada</option>
              <option value="Cancelled">Cancelada</option>
              <option value="NoShow">No Asistió</option>
            </select>
            {hasCalendarFilters && (
              <button
                onClick={clearCalendarFilters}
                className="flex items-center gap-1 text-xs text-gray-500 hover:text-gray-700"
              >
                <X className="h-3 w-3" />
                Limpiar filtros
              </button>
            )}
          </div>
        </>
      )}

      {view === 'list' ? (
        <AppointmentsListView
          doctors={doctors}
          patients={patients}
          services={services}
        />
      ) : (
        <div className="bg-white rounded-lg border border-gray-200 p-4">
          <AppointmentsCalendarView
            viewType={viewConfig[view].fcView!}
            currentDate={currentDate}
            filters={calendarFilters}
            onDateClick={handleDateClick}
            onEventClick={handleEventClick}
          />
        </div>
      )}

      <Modal isOpen={showCreateModal} onClose={() => { setShowCreateModal(false); setEditingAppointment(null); }} title={editingAppointment ? 'Editar Cita' : 'Nueva Cita'} size="xl">
        <form onSubmit={createForm.handleSubmit(handleCreate)} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <div className="space-y-1">
              <label className="block text-sm font-medium text-gray-700">Paciente</label>
              <select
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                {...createForm.register('patientId')}
              >
                <option value="">Seleccionar paciente</option>
                {patients.map((p) => (
                  <option key={p.id} value={p.id}>{p.firstName} {p.lastName}</option>
                ))}
              </select>
              {createForm.formState.errors.patientId && (
                <p className="text-sm text-red-600">{createForm.formState.errors.patientId.message}</p>
              )}
            </div>
            <div className="space-y-1">
              <label className="block text-sm font-medium text-gray-700">Doctor</label>
              <select
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                {...createForm.register('doctorId')}
              >
                <option value="">Seleccionar doctor</option>
                {doctors.map((d) => (
                  <option key={d.id} value={d.id}>{d.fullName}</option>
                ))}
              </select>
              {createForm.formState.errors.doctorId && (
                <p className="text-sm text-red-600">{createForm.formState.errors.doctorId.message}</p>
              )}
            </div>
            <div className="space-y-1">
              <label className="block text-sm font-medium text-gray-700">Servicio</label>
              <select
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                {...createForm.register('serviceId')}
              >
                <option value="">Seleccionar servicio</option>
                {services.map((s) => (
                  <option key={s.id} value={s.id}>{s.name}</option>
                ))}
              </select>
              {createForm.formState.errors.serviceId && (
                <p className="text-sm text-red-600">{createForm.formState.errors.serviceId.message}</p>
              )}
            </div>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
            <Input label="Fecha" type="date" error={createForm.formState.errors.appointmentDate?.message} {...createForm.register('appointmentDate')} />
            <Input label="Hora inicio" type="time" error={createForm.formState.errors.startTime?.message} {...createForm.register('startTime')} />
            <Input label="Hora fin" type="time" error={createForm.formState.errors.endTime?.message} {...createForm.register('endTime')} />
          </div>
          <Input label="Motivo" {...createForm.register('reason')} />
          <div className="space-y-1">
            <label className="block text-sm font-medium text-gray-700">Notas</label>
            <textarea
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
              rows={2}
              {...createForm.register('notes')}
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" type="button" onClick={() => setShowCreateModal(false)}>Cancelar</Button>
            <Button type="submit" isLoading={createMutation.isPending || updateMutation.isPending}>{editingAppointment ? 'Actualizar' : 'Guardar'}</Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}
