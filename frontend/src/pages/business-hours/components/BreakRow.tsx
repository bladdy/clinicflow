import { Trash2 } from 'lucide-react';
import type { BreakUI } from '../hooks/useSchedule';

interface Props {
  breakItem: BreakUI;
  onUpdate: (tempId: string, field: keyof BreakUI, value: string) => void;
  onRemove: (tempId: string) => void;
}

function calcDuration(start: string, end: string): number {
  const [sh, sm] = start.split(':').map(Number);
  const [eh, em] = end.split(':').map(Number);
  return (eh * 60 + (em || 0)) - (sh * 60 + (sm || 0));
}

export function BreakRow({ breakItem, onUpdate, onRemove }: Props) {
  const duration = calcDuration(breakItem.startTime, breakItem.endTime);

  return (
    <div className="flex items-center gap-3 py-2.5 group">
      <input
        type="text"
        value={breakItem.name}
        onChange={e => onUpdate(breakItem.tempId, 'name', e.target.value)}
        placeholder="Nombre del descanso"
        className="flex-1 h-8 px-3 text-sm border border-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent placeholder:text-gray-400"
      />
      <input
        type="time"
        value={breakItem.startTime}
        onChange={e => onUpdate(breakItem.tempId, 'startTime', e.target.value)}
        className="h-8 px-2 text-sm border border-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
      />
      <span className="text-gray-300 text-sm">→</span>
      <input
        type="time"
        value={breakItem.endTime}
        onChange={e => onUpdate(breakItem.tempId, 'endTime', e.target.value)}
        className="h-8 px-2 text-sm border border-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
      />
      <span className="text-xs text-gray-500 w-12 text-right tabular-nums">
        {duration > 0 ? `${duration}m` : '—'}
      </span>
      <button
        onClick={() => onRemove(breakItem.tempId)}
        className="p-1.5 rounded-md text-gray-400 hover:text-red-600 hover:bg-red-50 transition-colors opacity-0 group-hover:opacity-100"
        title="Eliminar"
      >
        <Trash2 className="h-3.5 w-3.5" />
      </button>
    </div>
  );
}
