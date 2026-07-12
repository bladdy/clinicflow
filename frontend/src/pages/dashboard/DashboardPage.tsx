import { useQuery } from '@tanstack/react-query';
import { Calendar, Users, MessageSquare, Clock } from 'lucide-react';
import { Card, CardContent } from '../../components/ui/Card';
import api from '../../services/api';
import type { ApiResponse, DashboardStats } from '../../types';

export function DashboardPage() {
  const { data: stats, isLoading } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: async () => {
      const response = await api.get<ApiResponse<DashboardStats>>('/dashboard/stats');
      return response.data.data!;
    },
  });

  const cards = [
    { label: 'Citas Hoy', value: stats?.todayAppointments ?? 0, icon: Calendar, color: 'bg-blue-500' },
    { label: 'Total Pacientes', value: stats?.totalPatients ?? 0, icon: Users, color: 'bg-green-500' },
    { label: 'Conversaciones Activas', value: stats?.activeConversations ?? 0, icon: MessageSquare, color: 'bg-purple-500' },
    { label: 'Citas Pendientes', value: stats?.pendingAppointments ?? 0, icon: Clock, color: 'bg-orange-500' },
  ];

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
        {cards.map((card) => (
          <Card key={card.label}>
            <CardContent>
              <div className="flex items-center gap-4">
                <div className={`${card.color} p-3 rounded-lg`}>
                  <card.icon className="h-6 w-6 text-white" />
                </div>
                <div>
                  <p className="text-sm text-gray-500">{card.label}</p>
                  <p className="text-2xl font-bold text-gray-900">
                    {isLoading ? '—' : card.value}
                  </p>
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
