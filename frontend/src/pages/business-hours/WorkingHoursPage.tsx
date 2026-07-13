import { useEffect, useCallback } from 'react';
import { CheckCircle2, AlertTriangle } from 'lucide-react';
import { useAuth } from '../../store/authStore';
import { Card, CardContent } from '../../components/ui/Card';
import { ProductiveHoursCard } from './components/ProductiveHoursCard';
import { WorkingDaysCard } from './components/WorkingDaysCard';
import { BreaksCard } from './components/BreaksCard';
import { LunchSettingsCard } from './components/LunchSettingsCard';
import { SaveBar } from './components/SaveBar';
import { useSchedule } from './hooks/useSchedule';

export function BusinessHoursPage() {
  const { user } = useAuth();
  const branchId = user?.branchId;

  const {
    days, breaks, lunch, isLoading, hasChanges, openDays, totalMinutes,
    showSuccess, savePending, saveError,
    toggleDay, updateDayTime, applyWeekdays, openAll, closeAll, restoreDefaults,
    addBreak, updateBreak, removeBreak, setLunch, handleSave,
    dayMinutes, formatMinutes, timeRangeDisplay,
  } = useSchedule(branchId);

  const copyToWeekdays = useCallback((sourceDay: number) => {
    const source = days.find(d => d.dayOfWeek === sourceDay);
    if (!source) return;
    [1, 2, 3, 4, 5].forEach(dow => {
      if (dow !== sourceDay) {
        updateDayTime(dow, 'openTime', source.openTime);
        updateDayTime(dow, 'closeTime', source.closeTime);
        const targetDay = days.find(d => d.dayOfWeek === dow);
        if (targetDay && !targetDay.isOpen) toggleDay(dow);
      }
    });
  }, [days, updateDayTime, toggleDay]);

  useEffect(() => {
    const handler = (e: BeforeUnloadEvent) => {
      if (savePending) return;
      e.preventDefault();
    };
    window.addEventListener('beforeunload', handler);
    return () => window.removeEventListener('beforeunload', handler);
  }, [savePending]);

  if (!branchId) {
    return (
      <div className="max-w-3xl mx-auto space-y-6">
        <h1 className="text-2xl font-bold text-gray-900">Horarios</h1>
        <Card>
          <CardContent>
            <div className="flex flex-col items-center justify-center py-12 text-center">
              <p className="text-gray-700 font-medium">No tienes una sucursal asignada</p>
              <p className="text-sm text-gray-400 mt-1">Contacta al administrador.</p>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="max-w-3xl mx-auto space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Horarios</h1>
        <p className="text-sm text-gray-500 mt-0.5">Configura los horarios de atención de tu sucursal.</p>
      </div>

      {/* Productive Hours */}
      <Card>
        <CardContent>
          <ProductiveHoursCard totalMinutes={totalMinutes} formatMinutes={formatMinutes} />
        </CardContent>
      </Card>

      {/* Working Days */}
      <Card>
        <div className="px-6 py-4 border-b border-gray-200">
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm font-semibold text-gray-900">Días de atención</p>
              <p className="text-xs text-gray-500">{openDays} de 7 días abiertos</p>
            </div>
            <div className="flex items-center gap-2">
              <button onClick={applyWeekdays} className="text-xs text-blue-600 hover:text-blue-700 font-medium">Lun-Vie</button>
              <span className="text-gray-300">·</span>
              <button onClick={openAll} className="text-xs text-blue-600 hover:text-blue-700 font-medium">Abrir todos</button>
              <span className="text-gray-300">·</span>
              <button onClick={closeAll} className="text-xs text-blue-600 hover:text-blue-700 font-medium">Cerrar todos</button>
              <span className="text-gray-300">·</span>
              <button onClick={restoreDefaults} className="text-xs text-gray-500 hover:text-gray-700 font-medium">Restaurar</button>
            </div>
          </div>
        </div>
        {isLoading ? (
          <div className="flex items-center justify-center py-12">
            <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
          </div>
        ) : (
          <WorkingDaysCard
            days={days}
            onToggle={toggleDay}
            onTimeChange={updateDayTime}
            onCopyToWeekdays={copyToWeekdays}
            dayMinutes={dayMinutes}
            formatMinutes={formatMinutes}
            timeRangeDisplay={timeRangeDisplay}
          />
        )}
      </Card>

      {/* Breaks */}
      <Card>
        <CardContent>
          <BreaksCard breaks={breaks} onAdd={addBreak} onUpdate={updateBreak} onRemove={removeBreak} />
        </CardContent>
      </Card>

      {/* Lunch */}
      <Card>
        <CardContent>
          <LunchSettingsCard lunch={lunch} onChange={setLunch} />
        </CardContent>
      </Card>

      {/* Save Bar */}
      <SaveBar hasChanges={hasChanges} isPending={savePending} onSave={handleSave} />

      {/* Toasts */}
      {showSuccess && (
        <div className="fixed bottom-6 right-6 z-50 flex items-center gap-3 rounded-xl bg-gray-900 px-5 py-3 text-sm font-medium text-white shadow-lg">
          <CheckCircle2 className="h-4 w-4 text-green-400" />
          Horarios guardados exitosamente.
        </div>
      )}
      {saveError && (
        <div className="fixed bottom-6 right-6 z-50 flex items-center gap-3 rounded-xl bg-red-600 px-5 py-3 text-sm font-medium text-white shadow-lg">
          <AlertTriangle className="h-4 w-4" />
          Error al guardar los horarios.
        </div>
      )}
    </div>
  );
}
