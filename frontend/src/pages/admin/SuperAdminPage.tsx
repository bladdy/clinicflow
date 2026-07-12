import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import api from '../../services/api';
import { Card, CardHeader, CardContent } from '../../components/ui/Card';
import { DataTable } from '../../components/ui/DataTable';
import { Building2, Users, UserCheck, Calendar, Crown } from 'lucide-react';
import { formatDate } from '../../utils/formatters';
import type { ApiResponse, PagedResult } from '../../types';

interface CompanyAdmin {
  id: string;
  name: string;
  email: string;
  phone: string;
  planName: string;
  subscriptionStatus: string;
  totalDoctors: number;
  totalPatients: number;
  totalAppointments: number;
  createdAt: string;
}

interface SuperAdminStats {
  totalCompanies: number;
  activeSubscriptions: number;
  totalDoctors: number;
  totalPatients: number;
  totalAppointments: number;
}

export function SuperAdminPage() {
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');

  const { data: stats } = useQuery({
    queryKey: ['super-admin-stats'],
    queryFn: async () => {
      const res = await api.get<ApiResponse<SuperAdminStats>>('/super-admin/stats');
      return res.data.data!;
    },
  });

  const { data, isLoading } = useQuery({
    queryKey: ['super-admin-companies', page, search],
    queryFn: async () => {
      const res = await api.get<ApiResponse<PagedResult<CompanyAdmin>>>('/super-admin/companies', {
        params: { page, pageSize: 10, search: search || undefined }
      });
      return res.data.data!;
    },
  });

  const columns = [
    { key: 'name', header: 'Empresa', render: (c: CompanyAdmin) => (
      <div>
        <p className="font-medium text-gray-900">{c.name}</p>
        <p className="text-xs text-gray-500">{c.email}</p>
      </div>
    )},
    { key: 'planName', header: 'Plan', render: (c: CompanyAdmin) => (
      <span className="inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium bg-blue-100 text-blue-800">
        <Crown className="h-3 w-3" />
        {c.planName}
      </span>
    )},
    { key: 'subscriptionStatus', header: 'Estado', render: (c: CompanyAdmin) => (
      <span className={`text-xs font-medium ${c.subscriptionStatus === 'Active' ? 'text-green-600' : 'text-red-600'}`}>
        {c.subscriptionStatus === 'Active' ? 'Activa' : c.subscriptionStatus}
      </span>
    )},
    { key: 'totalDoctors', header: 'Doctores' },
    { key: 'totalPatients', header: 'Pacientes' },
    { key: 'totalAppointments', header: 'Citas' },
    { key: 'createdAt', header: 'Registro', render: (c: CompanyAdmin) => formatDate(c.createdAt) },
  ];

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-gray-900">Panel de Administración</h1>
        <p className="text-sm text-gray-500 mt-1">Gestiona todas las empresas del sistema</p>
      </div>

      {stats && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
          <StatCard icon={<Building2 className="h-5 w-5 text-blue-600" />} label="Empresas" value={stats.totalCompanies} bg="bg-blue-50" />
          <StatCard icon={<Crown className="h-5 w-5 text-green-600" />} label="Suscripciones" value={stats.activeSubscriptions} bg="bg-green-50" />
          <StatCard icon={<Users className="h-5 w-5 text-purple-600" />} label="Doctores" value={stats.totalDoctors} bg="bg-purple-50" />
          <StatCard icon={<UserCheck className="h-5 w-5 text-amber-600" />} label="Pacientes" value={stats.totalPatients} bg="bg-amber-50" />
          <StatCard icon={<Calendar className="h-5 w-5 text-cyan-600" />} label="Citas" value={stats.totalAppointments} bg="bg-cyan-50" />
        </div>
      )}

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h2 className="text-lg font-semibold">Empresas</h2>
            <input
              type="text"
              placeholder="Buscar empresa..."
              value={search}
              onChange={(e) => { setSearch(e.target.value); setPage(1); }}
              className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm focus:border-blue-500 focus:ring-2 focus:ring-blue-500 focus:outline-none"
            />
          </div>
        </CardHeader>
        <DataTable
          columns={columns}
          data={data?.items ?? []}
          page={data?.page ?? 1}
          totalPages={data?.totalPages ?? 1}
          onPageChange={setPage}
          isLoading={isLoading}
          emptyMessage="No hay empresas registradas"
        />
      </Card>
    </div>
  );
}

function StatCard({ icon, label, value, bg }: { icon: React.ReactNode; label: string; value: number; bg: string }) {
  return (
    <Card>
      <CardContent className="flex items-center gap-3">
        <div className={`p-2 rounded-lg ${bg}`}>{icon}</div>
        <div>
          <p className="text-xs text-gray-500">{label}</p>
          <p className="text-xl font-bold text-gray-900">{value}</p>
        </div>
      </CardContent>
    </Card>
  );
}
