import { useState, useCallback, useMemo, useEffect } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import api from '../../../services/api';
import type { ApiResponse, BusinessSchedule } from '../../../types';

export interface DayUI {
  dayOfWeek: number;
  label: string;
  short: string;
  isOpen: boolean;
  openTime: string;
  closeTime: string;
}

export interface BreakUI {
  tempId: string;
  id?: string;
  name: string;
  startTime: string;
  endTime: string;
}

export interface LunchUI {
  durationMinutes: number;
  startTime: string;
}

const DAYS_META = [
  { value: 1, label: 'Lunes', short: 'L' },
  { value: 2, label: 'Martes', short: 'M' },
  { value: 3, label: 'Miércoles', short: 'X' },
  { value: 4, label: 'Jueves', short: 'J' },
  { value: 5, label: 'Viernes', short: 'V' },
  { value: 6, label: 'Sábado', short: 'S' },
  { value: 7, label: 'Domingo', short: 'D' },
];

function createDefaultDays(): DayUI[] {
  return DAYS_META.map(d => ({
    dayOfWeek: d.value,
    label: d.label,
    short: d.short,
    isOpen: d.value <= 5,
    openTime: '09:00',
    closeTime: '17:00',
  }));
}

function parseTimeToMinutes(time: string): number {
  const [h, m] = time.split(':').map(Number);
  return h * 60 + (m || 0);
}

function formatTime12(time: string): string {
  const [h, m] = time.split(':').map(Number);
  if (isNaN(h)) return time;
  const ampm = h >= 12 ? 'PM' : 'AM';
  const h12 = h % 12 || 12;
  return `${h12}:${(m ?? 0).toString().padStart(2, '0')} ${ampm}`;
}

export function useSchedule(branchId: string | undefined) {
  const queryClient = useQueryClient();
  const [showSuccess, setShowSuccess] = useState(false);

  const { data: scheduleData, isLoading } = useQuery({
    queryKey: ['business-schedule', branchId],
    queryFn: async () => {
      const response = await api.get<ApiResponse<BusinessSchedule>>(`/business-schedule/${branchId}`);
      return response.data.data!;
    },
    enabled: !!branchId,
  });

  const [days, setDays] = useState<DayUI[]>(createDefaultDays);
  const [breaks, setBreaks] = useState<BreakUI[]>([]);
  const [lunch, setLunch] = useState<LunchUI>({ durationMinutes: 45, startTime: '13:00' });
  const [initialized, setInitialized] = useState(false);

  useEffect(() => {
    if (scheduleData && !initialized) {
      setDays(scheduleData.days.map(d => ({
        dayOfWeek: d.dayOfWeek,
        label: DAYS_META.find(m => m.value === d.dayOfWeek)?.label ?? d.dayName,
        short: DAYS_META.find(m => m.value === d.dayOfWeek)?.short ?? d.dayName[0],
        isOpen: d.isOpen,
        openTime: d.openTime,
        closeTime: d.closeTime,
      })));

      setBreaks(scheduleData.breaks.map(b => ({
        tempId: b.id,
        id: b.id,
        name: b.name,
        startTime: b.startTime,
        endTime: b.endTime,
      })));

      if (scheduleData.lunchConfig) {
        setLunch({
          durationMinutes: scheduleData.lunchConfig.durationMinutes,
          startTime: scheduleData.lunchConfig.startTime,
        });
      }
      setInitialized(true);
    }
  }, [scheduleData, initialized]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      const body = {
        days: days.map(d => ({
          dayOfWeek: d.dayOfWeek,
          isOpen: d.isOpen,
          openTime: d.openTime,
          closeTime: d.closeTime,
        })),
        breaks: breaks.map((b, i) => ({
          name: b.name,
          startTime: b.startTime,
          endTime: b.endTime,
          sortOrder: i,
        })),
        lunchConfig: {
          durationMinutes: lunch.durationMinutes,
          startTime: lunch.startTime,
        },
      };
      const response = await api.put<ApiResponse<BusinessSchedule>>(
        `/business-schedule/${branchId}`, body
      );
      return response.data.data!;
    },
    onSuccess: (data) => {
      queryClient.setQueryData(['business-schedule', branchId], data);
      setShowSuccess(true);
      setTimeout(() => setShowSuccess(false), 3000);
    },
  });

  const toggleDay = useCallback((dayOfWeek: number) => {
    setDays(prev => prev.map(d =>
      d.dayOfWeek === dayOfWeek ? { ...d, isOpen: !d.isOpen } : d
    ));
  }, []);

  const updateDayTime = useCallback((dayOfWeek: number, field: 'openTime' | 'closeTime', value: string) => {
    setDays(prev => prev.map(d =>
      d.dayOfWeek === dayOfWeek ? { ...d, [field]: value } : d
    ));
  }, []);

  const applyWeekdays = useCallback(() => {
    setDays(prev => prev.map(d => ({
      ...d,
      isOpen: d.dayOfWeek <= 5,
      openTime: '09:00',
      closeTime: '17:00',
    })));
  }, []);

  const openAll = useCallback(() => {
    setDays(prev => prev.map(d => ({
      ...d,
      isOpen: true,
      openTime: '09:00',
      closeTime: '17:00',
    })));
  }, []);

  const closeAll = useCallback(() => {
    setDays(prev => prev.map(d => ({ ...d, isOpen: false })));
  }, []);

  const restoreDefaults = useCallback(() => {
    setDays(createDefaultDays());
    setBreaks([]);
    setLunch({ durationMinutes: 45, startTime: '13:00' });
  }, []);

  const addBreak = useCallback(() => {
    setBreaks(prev => [...prev, {
      tempId: crypto.randomUUID(),
      name: '',
      startTime: '10:30',
      endTime: '10:45',
    }]);
  }, []);

  const updateBreak = useCallback((tempId: string, field: keyof BreakUI, value: string) => {
    setBreaks(prev => prev.map(b =>
      b.tempId === tempId ? { ...b, [field]: value } : b
    ));
  }, []);

  const removeBreak = useCallback((tempId: string) => {
    setBreaks(prev => prev.filter(b => b.tempId !== tempId));
  }, []);

  const hasChanges = useMemo(() => {
    if (!scheduleData) return false;
    const currentDays = days.map(d => `${d.dayOfWeek}:${d.isOpen}:${d.openTime}:${d.closeTime}`).join('|');
    const origDays = scheduleData.days.map(d => `${d.dayOfWeek}:${d.isOpen}:${d.openTime}:${d.closeTime}`).join('|');
    if (currentDays !== origDays) return true;
    if (breaks.length !== scheduleData.breaks.length) return true;
    const currentBreaks = breaks.map(b => `${b.name}:${b.startTime}:${b.endTime}`).join('|');
    const origBreaks = scheduleData.breaks.map(b => `${b.name}:${b.startTime}:${b.endTime}`).join('|');
    if (currentBreaks !== origBreaks) return true;
    const currentLunch = `${lunch.durationMinutes}:${lunch.startTime}`;
    const origLunch = scheduleData.lunchConfig ? `${scheduleData.lunchConfig.durationMinutes}:${scheduleData.lunchConfig.startTime}` : `${45}:${'13:00'}`;
    return currentLunch !== origLunch;
  }, [days, breaks, lunch, scheduleData]);

  const openDays = useMemo(() => days.filter(d => d.isOpen).length, [days]);

  const totalMinutes = useMemo(() => {
    const workMinutes = days.filter(d => d.isOpen).reduce((sum, d) => {
      return sum + Math.max(0, parseTimeToMinutes(d.closeTime) - parseTimeToMinutes(d.openTime));
    }, 0);
    const breakMinutes = breaks.reduce((sum, b) => {
      return sum + Math.max(0, parseTimeToMinutes(b.endTime) - parseTimeToMinutes(b.startTime));
    }, 0);
    const lunchMinutes = lunch.durationMinutes;
    return Math.max(0, workMinutes - breakMinutes - lunchMinutes);
  }, [days, breaks, lunch]);

  const dayMinutes = useCallback((day: DayUI): number => {
    if (!day.isOpen) return 0;
    return Math.max(0, parseTimeToMinutes(day.closeTime) - parseTimeToMinutes(day.openTime));
  }, []);

  const formatMinutes = (mins: number): string => {
    if (mins === 0) return '0h';
    const h = Math.floor(mins / 60);
    const m = mins % 60;
    if (m === 0) return `${h}h`;
    return `${h}h ${m}m`;
  };

  const timeRangeDisplay = (day: DayUI): string => {
    if (!day.isOpen) return 'Cerrado';
    return `${formatTime12(day.openTime)} – ${formatTime12(day.closeTime)}`;
  };

  return {
    days,
    breaks,
    lunch,
    isLoading,
    hasChanges,
    openDays,
    totalMinutes,
    showSuccess,
    savePending: saveMutation.isPending,
    saveError: saveMutation.isError,
    toggleDay,
    updateDayTime,
    applyWeekdays,
    openAll,
    closeAll,
    restoreDefaults,
    addBreak,
    updateBreak,
    removeBreak,
    setLunch,
    handleSave: () => saveMutation.mutateAsync(),
    dayMinutes,
    formatMinutes,
    timeRangeDisplay,
  };
}
