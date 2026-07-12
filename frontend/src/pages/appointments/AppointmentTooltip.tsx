import type { Appointment } from '../../types';
import { statusColors, statusLabels, formatTime } from '../../utils/formatters';

interface AppointmentTooltipProps {
  appointment: Appointment;
  position?: { x: number; y: number };
}

const statusDotColors: Record<string, string> = {
  Scheduled: 'bg-blue-500',
  Confirmed: 'bg-green-500',
  InProgress: 'bg-yellow-500',
  Completed: 'bg-gray-400',
  Cancelled: 'bg-red-500',
  NoShow: 'bg-orange-500',
};

export function AppointmentTooltip({ appointment }: AppointmentTooltipProps) {
  return (
    <div className="bg-white rounded-lg shadow-lg border border-gray-200 p-3 min-w-[220px] max-w-[280px]">
      <div className="flex items-center gap-2 mb-2">
        <span className={`w-2 h-2 rounded-full ${statusDotColors[appointment.status] ?? 'bg-gray-400'}`} />
        <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${statusColors[appointment.status] ?? ''}`}>
          {statusLabels[appointment.status] ?? appointment.status}
        </span>
      </div>
      <div className="text-sm font-semibold text-gray-900 mb-1">
        {appointment.patientName || 'Sin paciente'}
      </div>
      <div className="text-xs text-gray-500 space-y-1">
        <div className="flex items-center gap-1.5">
          <span className="font-medium text-gray-700">Doctor:</span>
          <span>{appointment.doctorName || '—'}</span>
        </div>
        <div className="flex items-center gap-1.5">
          <span className="font-medium text-gray-700">Servicio:</span>
          <span>{appointment.serviceName || '—'}</span>
        </div>
        <div className="flex items-center gap-1.5">
          <span className="font-medium text-gray-700">Hora:</span>
          <span>{formatTime(appointment.startTime)} - {formatTime(appointment.endTime)}</span>
        </div>
        {appointment.reason && (
          <div className="flex items-start gap-1.5">
            <span className="font-medium text-gray-700">Motivo:</span>
            <span className="line-clamp-2">{appointment.reason}</span>
          </div>
        )}
      </div>
    </div>
  );
}
