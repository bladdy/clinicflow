import { useState, useCallback, useMemo, useRef, useEffect } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import FullCalendar from '@fullcalendar/react';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import interactionPlugin from '@fullcalendar/interaction';
import listPlugin from '@fullcalendar/list';
import type { DateClickArg, EventDropArg, EventResizeArg } from '@fullcalendar/interaction';
import type { EventInput, EventContentArg } from '@fullcalendar/core';
import { useRangeAppointments, useMoveAppointment } from '../../hooks/useAppointments';
import type { CalendarFilters } from '../../hooks/useAppointments';
import { AppointmentTooltip } from './AppointmentTooltip';
import { formatTime } from '../../utils/formatters';
import type { Appointment } from '../../types';
import './appointments.css';

const statusDotColors: Record<string, string> = {
  Scheduled: 'bg-blue-500',
  Confirmed: 'bg-green-500',
  InProgress: 'bg-yellow-500',
  Completed: 'bg-gray-400',
  Cancelled: 'bg-red-500',
  NoShow: 'bg-orange-500',
};

function hexToRgba(hex: string, alpha: number): string {
  let h = hex.replace('#', '');
  if (h.length === 3) h = h[0] + h[0] + h[1] + h[1] + h[2] + h[2];
  const r = parseInt(h.substring(0, 2), 16);
  const g = parseInt(h.substring(2, 4), 16);
  const b = parseInt(h.substring(4, 6), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

function toTimeStr(h: number, m: number): string {
  return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
}

interface Props {
  viewType: 'timeGridDay' | 'timeGridWeek' | 'dayGridMonth' | 'listWeek';
  currentDate: Date;
  filters?: CalendarFilters;
  onDateClick?: (date: Date) => void;
  onEventClick?: (appointment: Appointment) => void;
}

export function AppointmentsCalendarView({ viewType, currentDate, filters, onDateClick, onEventClick }: Props) {
  const [hoveredEvent, setHoveredEvent] = useState<Appointment | null>(null);
  const [tooltipPos, setTooltipPos] = useState<{ x: number; y: number }>({ x: 0, y: 0 });
  const queryClient = useQueryClient();
  const moveMutation = useMoveAppointment();
  const calendarRef = useRef<InstanceType<typeof FullCalendar> | null>(null);

  useEffect(() => {
    calendarRef.current?.getApi().changeView(viewType);
  }, [viewType]);

  useEffect(() => {
    calendarRef.current?.getApi().gotoDate(currentDate);
  }, [currentDate]);

  const range = useMemo(() => {
    const from = new Date(currentDate);
    const to = new Date(currentDate);
    if (viewType === 'timeGridDay' || viewType === 'listWeek') {
      from.setHours(0, 0, 0, 0);
      to.setHours(23, 59, 59, 999);
    } else if (viewType === 'timeGridWeek') {
      const day = from.getDay();
      from.setDate(from.getDate() - day);
      from.setHours(0, 0, 0, 0);
      to.setDate(to.getDate() + (6 - day));
      to.setHours(23, 59, 59, 999);
    } else {
      from.setDate(1);
      from.setHours(0, 0, 0, 0);
      to.setMonth(to.getMonth() + 1, 0);
      to.setHours(23, 59, 59, 999);
    }
    return { from, to };
  }, [currentDate, viewType]);

  const { data: appointments = [], isLoading } = useRangeAppointments(range.from, range.to, filters);

  const events: EventInput[] = useMemo(() => {
    return appointments.map((a) => {
      const raw = a.appointmentDate;
      const dateStr = raw.includes('T') ? raw.split('T')[0] : raw.split(' ')[0];
      const color = a.doctorColor || '#3B82F6';
      return {
        id: a.id,
        title: a.patientName || 'Sin paciente',
        start: `${dateStr}T${a.startTime}`,
        end: `${dateStr}T${a.endTime}`,
        backgroundColor: hexToRgba(color, 0.25),
        borderColor: color,
        borderWidth: 0,
        textColor: '#1f2937',
        extendedProps: { appointment: a, doctorColor: color },
      };
    });
  }, [appointments]);

  const renderEventContent = useCallback((arg: EventContentArg) => {
    const a = arg.event.extendedProps.appointment as Appointment;
    const color = arg.event.extendedProps.doctorColor as string || a.doctorColor || '#3B82F6';
    const isCompact = viewType === 'dayGridMonth' || viewType === 'timeGridWeek';

    if (isCompact) {
      return (
        <div
          className="flex items-center gap-1 w-full px-1.5 py-0.5 rounded"
          style={{ backgroundColor: hexToRgba(color, 0.2) }}
        >
          <span className="w-1.5 h-1.5 rounded-full flex-shrink-0" style={{ backgroundColor: color }} />
          <span className="truncate text-[11px] font-medium">{a.patientName}</span>
        </div>
      );
    }

    return (
      <div className="fc-event-content-wrapper px-2 py-1 relative pl-3" style={{ backgroundColor: hexToRgba(color, 0.25) }}>
        <div
          className="absolute left-0 top-0 bottom-0 w-1 rounded-l"
          style={{ backgroundColor: color }}
        />
        <div className="flex items-center justify-between">
          <span className="text-[11px] font-semibold text-gray-900 truncate">{a.patientName}</span>
          <span className={`w-1.5 h-1.5 rounded-full flex-shrink-0 ml-1 ${statusDotColors[a.status] ?? 'bg-gray-400'}`} />
        </div>
        <div className="text-[10px] text-gray-500">
          {formatTime(a.startTime)} - {formatTime(a.endTime)}
        </div>
        <div className="text-[10px] text-gray-600 truncate">{a.serviceName}</div>
        <div className="text-[10px] text-gray-500 truncate">{a.doctorName}</div>
      </div>
    );
  }, [viewType]);

  const handleDateClick = useCallback((arg: DateClickArg) => {
    onDateClick?.(arg.date);
  }, [onDateClick]);

  const handleEventClick = useCallback((arg: { event: { extendedProps: { appointment: Appointment } } }) => {
    onEventClick?.(arg.event.extendedProps.appointment);
  }, [onEventClick]);

  const handleEventDrop = useCallback(async (arg: EventDropArg) => {
    setHoveredEvent(null);
    const a = arg.event.extendedProps.appointment as Appointment;
    const newDate = toDateStr(arg.event.start!);
    const newStart = toTimeStr(arg.event.start!.getHours(), arg.event.start!.getMinutes());
    const newEnd = toTimeStr(arg.event.end!.getHours(), arg.event.end!.getMinutes());
    try {
      await moveMutation.mutateAsync({ id: a.id, date: newDate, startTime: newStart, endTime: newEnd });
    } catch (err) {
      console.error('Failed to move appointment:', err);
      arg.revert();
    }
  }, [moveMutation]);

  const handleEventResize = useCallback(async (arg: EventResizeArg) => {
    setHoveredEvent(null);
    const a = arg.event.extendedProps.appointment as Appointment;
    const newDate = toDateStr(arg.event.start!);
    const newStart = toTimeStr(arg.event.start!.getHours(), arg.event.start!.getMinutes());
    const newEnd = toTimeStr(arg.event.end!.getHours(), arg.event.end!.getMinutes());
    try {
      await moveMutation.mutateAsync({ id: a.id, date: newDate, startTime: newStart, endTime: newEnd });
    } catch (err) {
      console.error('Failed to resize appointment:', err);
      arg.revert();
    }
  }, [moveMutation]);

  const handleEventMouseEnter = useCallback((arg: { event: { extendedProps: { appointment: Appointment } }; jsEvent: MouseEvent }) => {
    setHoveredEvent(arg.event.extendedProps.appointment);
    setTooltipPos({ x: arg.jsEvent.clientX, y: arg.jsEvent.clientY });
  }, []);

  const handleEventMouseLeave = useCallback(() => {
    setHoveredEvent(null);
  }, []);

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-96">
        <div className="text-gray-500 text-sm">Cargando citas...</div>
      </div>
    );
  }

  return (
    <div>
      <FullCalendar
        ref={calendarRef}
        plugins={[dayGridPlugin, timeGridPlugin, interactionPlugin, listPlugin]}
        initialView={viewType}
        initialDate={currentDate}
        headerToolbar={false}
        slotMinTime="08:00:00"
        slotMaxTime="20:00:00"
        slotDuration="00:15:00"
        scrollTime="08:00:00"
        allDaySlot={false}
        editable={true}
        droppable={true}
        eventOverlap={true}
        slotEventOverlap={false}
        timeZone="local"
        events={events}
        eventContent={renderEventContent}
        dateClick={handleDateClick}
        eventClick={handleEventClick}
        eventDrop={handleEventDrop}
        eventResize={handleEventResize}
        eventMouseEnter={handleEventMouseEnter}
        eventMouseLeave={handleEventMouseLeave}
        height="auto"
        locale="es"
        buttonText={{
          today: 'Hoy',
          month: 'Mes',
          week: 'Semana',
          day: 'Día',
          list: 'Lista',
        }}
        views={{
          timeGridDay: { buttonText: 'Día' },
          timeGridWeek: { buttonText: 'Semana' },
          dayGridMonth: { buttonText: 'Mes' },
          listWeek: { buttonText: 'Lista' },
        }}
      />
      {hoveredEvent && (
        <div
          className="fixed z-50 pointer-events-none"
          style={{
            left: tooltipPos.x + 250 > window.innerWidth ? tooltipPos.x - 260 : tooltipPos.x + 12,
            top: tooltipPos.y + 200 > window.innerHeight ? tooltipPos.y - 210 : tooltipPos.y + 12,
          }}
        >
          <AppointmentTooltip appointment={hoveredEvent} />
        </div>
      )}
    </div>
  );
}
