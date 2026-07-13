import { Clock } from 'lucide-react';

interface Props {
  totalMinutes: number;
  formatMinutes: (m: number) => string;
}

export function ProductiveHoursCard({ totalMinutes, formatMinutes }: Props) {
  return (
    <div className="flex items-center justify-between">
      <div className="flex items-center gap-3">
        <div className="flex items-center justify-center h-9 w-9 rounded-lg bg-blue-50">
          <Clock className="h-4.5 w-4.5 text-blue-600" />
        </div>
        <div>
          <p className="text-sm font-medium text-gray-900">Horas productivas esperadas</p>
          <p className="text-xs text-gray-500">Tiempo neto de atención por semana</p>
        </div>
      </div>
      <span className="text-lg font-semibold text-gray-900 tabular-nums">
        {formatMinutes(totalMinutes)}
      </span>
    </div>
  );
}
