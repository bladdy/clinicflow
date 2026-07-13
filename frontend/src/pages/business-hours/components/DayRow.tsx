import { useState, useRef, useEffect } from 'react';
import { ChevronDown, ChevronUp, Copy } from 'lucide-react';
import { Toggle } from '../../../components/ui/Toggle';
import type { DayUI } from '../hooks/useSchedule';

interface Props {
  day: DayUI;
  onToggle: () => void;
  onTimeChange: (field: 'openTime' | 'closeTime', value: string) => void;
  onCopyToWeekdays: () => void;
  dayMinutes: number;
  formatMinutes: (m: number) => string;
  timeRangeDisplay: (d: DayUI) => string;
}

export function DayRow({ day, onToggle, onTimeChange, onCopyToWeekdays, dayMinutes, formatMinutes, timeRangeDisplay }: Props) {
  const [expanded, setExpanded] = useState(false);
  const [showCopyMenu, setShowCopyMenu] = useState(false);
  const copyRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const handler = (e: MouseEvent) => {
      if (copyRef.current && !copyRef.current.contains(e.target as Node)) setShowCopyMenu(false);
    };
    document.addEventListener('mousedown', handler);
    return () => document.removeEventListener('mousedown', handler);
  }, []);

  const isWeekend = day.dayOfWeek >= 6;

  return (
    <div className={`transition-all duration-200 ${!day.isOpen ? 'opacity-50' : isWeekend ? 'bg-amber-50/40' : ''}`}>
      <div className={`flex items-center gap-3 py-3 px-6 transition-all duration-150 hover:bg-gray-50`}>
        <span className={`inline-flex items-center justify-center h-8 w-8 rounded-lg text-xs font-bold shrink-0 transition-colors ${
          !day.isOpen ? 'bg-gray-200 text-gray-500' : isWeekend ? 'bg-amber-100 text-amber-700' : 'bg-blue-100 text-blue-700'
        }`}>
          {day.short}
        </span>

        <div className="w-28 shrink-0">
          <p className="text-sm font-medium text-gray-900">{day.label}</p>
          <p className={`text-xs ${!day.isOpen ? 'text-gray-400' : 'text-green-600'}`}>
            {day.isOpen ? 'Abierto' : 'Cerrado'}
          </p>
        </div>

        <Toggle checked={day.isOpen} onChange={onToggle} label={`Abrir/cerrar ${day.label}`} size="sm" />

        <div className="flex items-center gap-2 shrink-0 ml-auto">
          {day.isOpen && (
            <div className="relative" ref={copyRef}>
              <button
                onClick={() => setShowCopyMenu(!showCopyMenu)}
                className="p-1 rounded-md text-gray-400 hover:text-gray-600 hover:bg-gray-100 transition-colors"
                title="Copiar a otros días"
              >
                <Copy className="h-3.5 w-3.5" />
              </button>
              {showCopyMenu && (
                <div className="absolute right-0 top-full mt-1 z-20 bg-white border border-gray-200 rounded-lg shadow-lg py-1 min-w-[180px]">
                  <button onClick={() => { onCopyToWeekdays(); setShowCopyMenu(false); }}
                    className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-50">
                    Copiar a Lun-Vie
                  </button>
                  <button onClick={() => { onCopyToWeekdays(); setShowCopyMenu(false); }}
                    className="w-full text-left px-3 py-2 text-sm text-gray-700 hover:bg-gray-50">
                    Copiar a todos los días
                  </button>
                </div>
              )}
            </div>
          )}

          <button
            onClick={() => setExpanded(!expanded)}
            className="flex items-center gap-2 text-sm text-gray-700 hover:text-gray-900 transition-colors group"
          >
            <span className="font-medium">{timeRangeDisplay(day)}</span>
            {expanded ? (
              <ChevronUp className="h-3.5 w-3.5 text-gray-400 group-hover:text-gray-600" />
            ) : (
              <ChevronDown className="h-3.5 w-3.5 text-gray-400 group-hover:text-gray-600" />
            )}
          </button>

          <div className={`text-xs font-semibold px-2.5 py-1 rounded-md ${
            !day.isOpen ? 'bg-gray-100 text-gray-400' : 'bg-gray-100 text-gray-600'
          }`}>
            {!day.isOpen ? '0h' : formatMinutes(dayMinutes)}
          </div>
        </div>
      </div>

      {expanded && day.isOpen && (
        <div className="px-6 pb-4 pt-1 border-t border-gray-100">
          <div className="flex items-center gap-4">
            <div className="flex items-center gap-2">
              <label className="text-xs font-medium text-gray-500">Apertura</label>
              <input type="time" value={day.openTime}
                onChange={e => onTimeChange('openTime', e.target.value)}
                className="h-8 px-2 text-sm border border-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent" />
            </div>
            <span className="text-gray-300">→</span>
            <div className="flex items-center gap-2">
              <label className="text-xs font-medium text-gray-500">Cierre</label>
              <input type="time" value={day.closeTime}
                onChange={e => onTimeChange('closeTime', e.target.value)}
                className="h-8 px-2 text-sm border border-gray-200 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent" />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
