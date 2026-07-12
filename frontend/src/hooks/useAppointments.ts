import { useState, useCallback } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '../services/api';
import type { ApiResponse, Appointment, Doctor, Patient, Service } from '../types';

export type CalendarView = 'list' | 'day' | 'week' | 'month';

const STORAGE_KEY = 'dentalbot-appointments-view';

function loadView(): CalendarView {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored && ['list', 'day', 'week', 'month'].includes(stored)) return stored as CalendarView;
  } catch {}
  return 'list';
}

export function useAppointmentsView() {
  const [view, setView] = useState<CalendarView>(loadView);
  const changeView = useCallback((v: CalendarView) => {
    setView(v);
    try { localStorage.setItem(STORAGE_KEY, v); } catch {}
  }, []);
  return { view, changeView };
}

export interface CalendarFilters {
  doctorId?: string;
  serviceId?: string;
  status?: string;
}

export function useRangeAppointments(from: Date, to: Date, filters?: CalendarFilters) {
  return useQuery({
    queryKey: ['appointments', 'range', from.toISOString(), to.toISOString(), filters?.doctorId, filters?.serviceId, filters?.status],
    queryFn: async () => {
      const toStr = (d: Date) => {
        const y = d.getFullYear();
        const m = String(d.getMonth() + 1).padStart(2, '0');
        const day = String(d.getDate()).padStart(2, '0');
        return `${y}-${m}-${day}`;
      };
      const params: Record<string, string> = {
        from: toStr(from),
        to: toStr(to),
      };
      if (filters?.doctorId) params.doctorId = filters.doctorId;
      if (filters?.serviceId) params.serviceId = filters.serviceId;
      if (filters?.status) params.status = filters.status;
      const response = await api.get<ApiResponse<Appointment[]>>('/appointments/range', { params });
      return response.data.data ?? [];
    },
  });
}

export function useAppointmentDoctors() {
  return useQuery({
    queryKey: ['doctors', { page: 1, pageSize: 200 }],
    queryFn: async () => {
      const response = await api.get<ApiResponse<{ items: Doctor[] }>>('/doctors', { params: { page: 1, pageSize: 200 } });
      return response.data.data?.items ?? [];
    },
  });
}

export function useAppointmentPatients() {
  return useQuery({
    queryKey: ['patients', { page: 1, pageSize: 200 }],
    queryFn: async () => {
      const response = await api.get<ApiResponse<{ items: Patient[] }>>('/patients', { params: { page: 1, pageSize: 200 } });
      return response.data.data?.items ?? [];
    },
  });
}

export function useAppointmentServices() {
  return useQuery({
    queryKey: ['services', { page: 1, pageSize: 200 }],
    queryFn: async () => {
      const response = await api.get<ApiResponse<{ items: Service[] }>>('/services', { params: { page: 1, pageSize: 200 } });
      return response.data.data?.items ?? [];
    },
  });
}

export function useMoveAppointment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, date, startTime, endTime }: { id: string; date: string; startTime: string; endTime: string }) => {
      const response = await api.put<ApiResponse<Appointment>>(`/appointments/${id}`, {
        appointmentDate: date,
        startTime,
        endTime,
      });
      if (!response.data.success) throw new Error(response.data.message);
      return response.data.data!;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['appointments'] });
    },
  });
}
