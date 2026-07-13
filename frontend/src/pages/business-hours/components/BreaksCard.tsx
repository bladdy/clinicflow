import { Plus } from 'lucide-react';
import { BreakRow } from './BreakRow';
import type { BreakUI } from '../hooks/useSchedule';

interface Props {
  breaks: BreakUI[];
  onAdd: () => void;
  onUpdate: (tempId: string, field: keyof BreakUI, value: string) => void;
  onRemove: (tempId: string) => void;
}

export function BreaksCard({ breaks, onAdd, onUpdate, onRemove }: Props) {
  return (
    <div>
      <div className="flex items-center justify-between mb-3">
        <div>
          <p className="text-sm font-medium text-gray-900">Descansos</p>
          <p className="text-xs text-gray-500">Pausas que aplican todos los días</p>
        </div>
        <button
          onClick={onAdd}
          className="inline-flex items-center gap-1.5 text-sm font-medium text-blue-600 hover:text-blue-700 transition-colors"
        >
          <Plus className="h-4 w-4" />
          Agregar
        </button>
      </div>

      {breaks.length === 0 ? (
        <p className="text-sm text-gray-400 italic py-2">Sin descansos configurados</p>
      ) : (
        <div className="space-y-1">
          {breaks.map(b => (
            <BreakRow key={b.tempId} breakItem={b} onUpdate={onUpdate} onRemove={onRemove} />
          ))}
        </div>
      )}
    </div>
  );
}
