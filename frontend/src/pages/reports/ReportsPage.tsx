import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import api from '../../services/api';
import { Card, CardHeader, CardContent } from '../../components/ui/Card';
import { BarChart3, Users, Calendar, TrendingUp, DollarSign, AlertTriangle } from 'lucide-react';
import { formatCurrency, formatDate } from '../../utils/formatters';
import type { ApiResponse } from '../../types';

interface ReportsOverview {
  totalAppointments: number;
  completedAppointments: number;
  cancelledAppointments: number;
  noShowAppointments: number;
  totalPatients: number;
  totalRevenue: number;
  dateFrom: string;
  dateTo: string;
}

interface ServiceReport {
  serviceId: string;
  serviceName: string;
  totalAppointments: number;
  completedAppointments: number;
  revenue: number;
}

interface DoctorReport {
  doctorId: string;
  doctorName: string;
  specialty: string;
  totalAppointments: number;
  completedAppointments: number;
  noShowAppointments: number;
}

interface MonthlyTrend {
  month: string;
  totalAppointments: number;
  completedAppointments: number;
  revenue: number;
}

interface PatientRetention {
  totalPatients: number;
  patientsWithAppointments: number;
  returningPatients: number;
  firstTimePatients: number;
  retentionRate: number;
}

type TabType = 'overview' | 'services' | 'doctors' | 'trend' | 'retention';

export function ReportsPage() {
  const [activeTab, setActiveTab] = useState<TabType>('overview');
  const [dateFrom, setDateFrom] = useState(() => {
    const d = new Date();
    d.setDate(d.getDate() - 30);
    return d.toISOString().split('T')[0];
  });
  const [dateTo, setDateTo] = useState(() => new Date().toISOString().split('T')[0]);

  const dateParams = `dateFrom=${dateFrom}&dateTo=${dateTo}`;

  const { data: overview, isLoading: loadingOverview } = useQuery({
    queryKey: ['reports-overview', dateFrom, dateTo],
    queryFn: async () => {
      const res = await api.get<ApiResponse<ReportsOverview>>(`/reports/overview?${dateParams}`);
      return res.data.data!;
    },
    enabled: activeTab === 'overview',
  });

  const { data: services, isLoading: loadingServices } = useQuery({
    queryKey: ['reports-services', dateFrom, dateTo],
    queryFn: async () => {
      const res = await api.get<ApiResponse<ServiceReport[]>>(`/reports/appointments-by-service?${dateParams}`);
      return res.data.data!;
    },
    enabled: activeTab === 'services',
  });

  const { data: doctors, isLoading: loadingDoctors } = useQuery({
    queryKey: ['reports-doctors', dateFrom, dateTo],
    queryFn: async () => {
      const res = await api.get<ApiResponse<DoctorReport[]>>(`/reports/appointments-by-doctor?${dateParams}`);
      return res.data.data!;
    },
    enabled: activeTab === 'doctors',
  });

  const { data: trend, isLoading: loadingTrend } = useQuery({
    queryKey: ['reports-trend'],
    queryFn: async () => {
      const res = await api.get<ApiResponse<MonthlyTrend[]>>('/reports/monthly-trend?months=6');
      return res.data.data!;
    },
    enabled: activeTab === 'trend',
  });

  const { data: retention, isLoading: loadingRetention } = useQuery({
    queryKey: ['reports-retention'],
    queryFn: async () => {
      const res = await api.get<ApiResponse<PatientRetention>>('/reports/patient-retention');
      return res.data.data!;
    },
    enabled: activeTab === 'retention',
  });

  const tabs: { key: TabType; label: string; icon: React.ReactNode }[] = [
    { key: 'overview', label: 'Resumen', icon: <BarChart3 className="h-4 w-4" /> },
    { key: 'services', label: 'Por Servicio', icon: <Calendar className="h-4 w-4" /> },
    { key: 'doctors', label: 'Por Doctor', icon: <Users className="h-4 w-4" /> },
    { key: 'trend', label: 'Tendencia', icon: <TrendingUp className="h-4 w-4" /> },
    { key: 'retention', label: 'Retención', icon: <Users className="h-4 w-4" /> },
  ];

  const isLoading = loadingOverview || loadingServices || loadingDoctors || loadingTrend || loadingRetention;

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Reportes</h1>
          <p className="text-sm text-gray-500 mt-1">Estadísticas y métricas de la clínica</p>
        </div>
      </div>

      <Card className="p-4">
        <div className="flex flex-wrap gap-4 items-end">
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Desde</label>
            <input
              type="date"
              value={dateFrom}
              onChange={(e) => setDateFrom(e.target.value)}
              className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1">Hasta</label>
            <input
              type="date"
              value={dateTo}
              onChange={(e) => setDateTo(e.target.value)}
              className="rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-2 focus:ring-blue-500"
            />
          </div>
        </div>
      </Card>

      <div className="flex gap-1 bg-gray-100 p-1 rounded-lg">
        {tabs.map((tab) => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            className={`flex items-center gap-2 px-4 py-2 rounded-md text-sm font-medium transition-colors ${
              activeTab === tab.key
                ? 'bg-white text-blue-600 shadow-sm'
                : 'text-gray-600 hover:text-gray-900'
            }`}
          >
            {tab.icon}
            {tab.label}
          </button>
        ))}
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-12">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
        </div>
      ) : (
        <>
          {activeTab === 'overview' && overview && (
            <div className="space-y-6">
              <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
                <StatCard icon={<Calendar className="h-5 w-5 text-blue-600" />} title="Total Citas" value={overview.totalAppointments} bg="bg-blue-50" />
                <StatCard icon={<Calendar className="h-5 w-5 text-green-600" />} title="Completadas" value={overview.completedAppointments} bg="bg-green-50" />
                <StatCard icon={<AlertTriangle className="h-5 w-5 text-orange-600" />} title="Canceladas" value={overview.cancelledAppointments} bg="bg-orange-50" />
                <StatCard icon={<AlertTriangle className="h-5 w-5 text-red-600" />} title="No Asistió" value={overview.noShowAppointments} bg="bg-red-50" />
                <StatCard icon={<Users className="h-5 w-5 text-purple-600" />} title="Total Pacientes" value={overview.totalPatients} bg="bg-purple-50" />
                <StatCard icon={<DollarSign className="h-5 w-5 text-emerald-600" />} title="Ingresos" value={formatCurrency(overview.totalRevenue)} bg="bg-emerald-50" />
              </div>
              <Card>
                <CardContent>
                  <p className="text-sm text-gray-500">
                    Periodo: {formatDate(overview.dateFrom)} - {formatDate(overview.dateTo)}
                  </p>
                  {overview.totalAppointments > 0 && (
                    <p className="text-sm text-gray-700 mt-2">
                      Tasa de asistencia: {Math.round((overview.completedAppointments / overview.totalAppointments) * 100)}%
                    </p>
                  )}
                </CardContent>
              </Card>
            </div>
          )}

          {activeTab === 'services' && services && (
            <Card>
              <CardHeader>
                <h2 className="text-lg font-semibold">Citas por Servicio</h2>
              </CardHeader>
              <CardContent>
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead>
                      <tr>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Servicio</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Total</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Completadas</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Ingresos</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-200">
                      {services.map((s) => (
                        <tr key={s.serviceId}>
                          <td className="px-6 py-3 text-sm font-medium text-gray-900">{s.serviceName}</td>
                          <td className="px-6 py-3 text-sm text-gray-700">{s.totalAppointments}</td>
                          <td className="px-6 py-3 text-sm text-gray-700">{s.completedAppointments}</td>
                          <td className="px-6 py-3 text-sm text-gray-700">{formatCurrency(s.revenue)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </CardContent>
            </Card>
          )}

          {activeTab === 'doctors' && doctors && (
            <Card>
              <CardHeader>
                <h2 className="text-lg font-semibold">Citas por Doctor</h2>
              </CardHeader>
              <CardContent>
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead>
                      <tr>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Doctor</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Especialidad</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Total</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">Completadas</th>
                        <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">No Asistió</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-200">
                      {doctors.map((d) => (
                        <tr key={d.doctorId}>
                          <td className="px-6 py-3 text-sm font-medium text-gray-900">{d.doctorName}</td>
                          <td className="px-6 py-3 text-sm text-gray-700">{d.specialty}</td>
                          <td className="px-6 py-3 text-sm text-gray-700">{d.totalAppointments}</td>
                          <td className="px-6 py-3 text-sm text-gray-700">{d.completedAppointments}</td>
                          <td className="px-6 py-3 text-sm text-gray-700">{d.noShowAppointments}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </CardContent>
            </Card>
          )}

          {activeTab === 'trend' && trend && (
            <Card>
              <CardHeader>
                <h2 className="text-lg font-semibold">Tendencia Mensual</h2>
              </CardHeader>
              <CardContent>
                <div className="space-y-4">
                  {trend.map((t) => (
                    <div key={t.month} className="flex items-center gap-4">
                      <div className="w-24 text-sm font-medium text-gray-700">{t.month}</div>
                      <div className="flex-1">
                        <div className="flex items-center gap-2">
                          <div className="flex-1 bg-gray-100 rounded-full h-6 overflow-hidden">
                            <div
                              className="bg-blue-500 h-full rounded-full min-w-[40px] flex items-center justify-end pr-2"
                              style={{ width: `${Math.min((t.totalAppointments / Math.max(...trend.map(x => x.totalAppointments), 1)) * 100, 100)}%` }}
                            >
                              <span className="text-xs text-white font-medium">{t.totalAppointments}</span>
                            </div>
                          </div>
                          <span className="text-sm text-gray-500 w-24 text-right">{formatCurrency(t.revenue)}</span>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </CardContent>
            </Card>
          )}

          {activeTab === 'retention' && retention && (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <Card>
                <CardHeader>
                  <h2 className="text-lg font-semibold">Retención de Pacientes</h2>
                </CardHeader>
                <CardContent className="space-y-4">
                  <div className="text-center py-6">
                    <div className="text-5xl font-bold text-blue-600">{retention.retentionRate}%</div>
                    <p className="text-sm text-gray-500 mt-2">Tasa de retención</p>
                  </div>
                  <div className="grid grid-cols-2 gap-4 text-center">
                    <div className="p-4 bg-blue-50 rounded-lg">
                      <div className="text-2xl font-bold text-blue-700">{retention.totalPatients}</div>
                      <p className="text-xs text-gray-500">Total Pacientes</p>
                    </div>
                    <div className="p-4 bg-green-50 rounded-lg">
                      <div className="text-2xl font-bold text-green-700">{retention.returningPatients}</div>
                      <p className="text-xs text-gray-500">Recurrentes</p>
                    </div>
                    <div className="p-4 bg-purple-50 rounded-lg">
                      <div className="text-2xl font-bold text-purple-700">{retention.firstTimePatients}</div>
                      <p className="text-xs text-gray-500">Primera Vez</p>
                    </div>
                    <div className="p-4 bg-gray-50 rounded-lg">
                      <div className="text-2xl font-bold text-gray-700">{retention.patientsWithAppointments}</div>
                      <p className="text-xs text-gray-500">Con Citas</p>
                    </div>
                  </div>
                </CardContent>
              </Card>
              <Card>
                <CardHeader>
                  <h2 className="text-lg font-semibold">Resumen de Retención</h2>
                </CardHeader>
                <CardContent className="space-y-3">
                  <p className="text-sm text-gray-600">
                    {retention.returningPatients} de {retention.totalPatients} pacientes han regresado a la clínica.
                  </p>
                  <p className="text-sm text-gray-600">
                    {retention.firstTimePatients} pacientes son nuevos y aún no tienen una segunda cita.
                  </p>
                  {retention.retentionRate < 50 && (
                    <div className="flex items-start gap-2 p-3 bg-yellow-50 rounded-lg">
                      <AlertTriangle className="h-5 w-5 text-yellow-600 mt-0.5" />
                      <p className="text-sm text-yellow-700">
                        La tasa de retención es baja. Considere implementar recordatorios de seguimiento y promociones para pacientes que no han regresado.
                      </p>
                    </div>
                  )}
                  {retention.retentionRate >= 70 && (
                    <div className="flex items-start gap-2 p-3 bg-green-50 rounded-lg">
                      <TrendingUp className="h-5 w-5 text-green-600 mt-0.5" />
                      <p className="text-sm text-green-700">
                        Excelente tasa de retención. Los pacientes están regresando consistentemente.
                      </p>
                    </div>
                  )}
                </CardContent>
              </Card>
            </div>
          )}
        </>
      )}
    </div>
  );
}

function StatCard({ icon, title, value, bg }: { icon: React.ReactNode; title: string; value: string | number; bg: string }) {
  return (
    <Card>
      <CardContent className="flex items-center gap-4">
        <div className={`p-3 rounded-lg ${bg}`}>
          {icon}
        </div>
        <div>
          <p className="text-sm text-gray-500">{title}</p>
          <p className="text-2xl font-bold text-gray-900">{value}</p>
        </div>
      </CardContent>
    </Card>
  );
}
