import { DayRow } from './DayRow';
import type { DayUI } from '../hooks/useSchedule';

interface Props {
  days: DayUI[];
  onToggle: (dayOfWeek: number) => void;
  onTimeChange: (dayOfWeek: number, field: 'openTime' | 'closeTime', value: string) => void;
  onCopyToWeekdays: (dayOfWeek: number) => void;
  dayMinutes: (d: DayUI) => number;
  formatMinutes: (m: number) => string;
  timeRangeDisplay: (d: DayUI) => string;
}

export function WorkingDaysCard({ days, onToggle, onTimeChange, onCopyToWeekdays, dayMinutes, formatMinutes, timeRangeDisplay }: Props) {
  return (
    <div>
      {days.map(day => (
        <DayRow
          key={day.dayOfWeek}
          day={day}
          onToggle={() => onToggle(day.dayOfWeek)}
          onTimeChange={(field, value) => onTimeChange(day.dayOfWeek, field, value)}
          onCopyToWeekdays={() => onCopyToWeekdays(day.dayOfWeek)}
          dayMinutes={dayMinutes(day)}
          formatMinutes={formatMinutes}
          timeRangeDisplay={timeRangeDisplay}
        />
      ))}
    </div>
  );
}
