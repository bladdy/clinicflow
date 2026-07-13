import type { LunchUI } from '../hooks/useSchedule';

interface Props {
  lunch: LunchUI;
  onChange: (lunch: LunchUI) => void;
}

const DURATIONS = [30, 45, 60, 90];
const START_TIMES = ['12:00', '12:30', '13:00', '13:30', '14:00'];

function formatTime12(time: string): string {
  const [h, m] = time.split(':').map(Number);
  if (isNaN(h)) return time;
  const ampm = h >= 12 ? 'PM' : 'AM';
  const h12 = h % 12 || 12;
  return `${h12}:${(m ?? 0).toString().padStart(2, '0')} ${ampm}`;
}

export function LunchSettingsCard({ lunch, onChange }: Props) {
  const [h, m] = lunch.startTime.split(':').map(Number);
  const endMinutes = h * 60 + (m || 0) + lunch.durationMinutes;
  const endH = Math.floor(endMinutes / 60);
  const endM = endMinutes % 60;
  const endTime = `${endH.toString().padStart(2, '0')}:${endM.toString().padStart(2, '0')}`;

  return (
    <div>
      <p className="text-sm font-medium text-gray-900 mb-3">Horario de comida</p>
      <div className="flex items-center gap-6">
        <div className="flex items-center gap-2">
          <label className="text-xs font-medium text-gray-500">Duración</label>
          <select
            value={lunch.durationMinutes}
            onChange={e => onChange({ ...lunch, durationMinutes: Number(e.target.value) })}
            className="h-8 px-2 text-sm border border-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent bg-white"
          >
            {DURATIONS.map(d => (
              <option key={d} value={d}>{d} min</option>
            ))}
          </select>
        </div>
        <div className="flex items-center gap-2">
          <label className="text-xs font-medium text-gray-500">Inicia</label>
          <select
            value={lunch.startTime}
            onChange={e => onChange({ ...lunch, startTime: e.target.value })}
            className="h-8 px-2 text-sm border border-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent bg-white"
          >
            {START_TIMES.map(t => (
              <option key={t} value={t}>{formatTime12(t)}</option>
            ))}
          </select>
        </div>
        <div className="flex items-center gap-2">
          <label className="text-xs font-medium text-gray-500">Termina</label>
          <span className="text-sm text-gray-700 font-medium">{formatTime12(endTime)}</span>
        </div>
      </div>
    </div>
  );
}
