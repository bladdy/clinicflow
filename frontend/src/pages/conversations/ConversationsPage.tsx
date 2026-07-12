import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Eye } from 'lucide-react';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { DataTable } from '../../components/ui/DataTable';
import { usePagedQuery } from '../../hooks/useApi';
import type { Conversation } from '../../types';
import { statusColors, statusLabels, formatDateTime } from '../../utils/formatters';

export function ConversationsPage() {
  const [page, setPage] = useState(1);
  const navigate = useNavigate();
  const { data, isLoading } = usePagedQuery<Conversation>('conversations', '/conversations', { page, pageSize: 10 });

  const columns = [
    { key: 'phone', header: 'Teléfono' },
    { key: 'patient', header: 'Paciente', render: (c: Conversation) => c.patient ? `${c.patient.firstName} ${c.patient.lastName}` : '—' },
    { key: 'status', header: 'Estado', render: (c: Conversation) => (
      <span className={`inline-flex px-2 py-1 rounded-full text-xs font-medium ${statusColors[c.status] ?? ''}`}>
        {statusLabels[c.status] ?? c.status}
      </span>
    )},
    { key: 'startedAt', header: 'Inicio', render: (c: Conversation) => formatDateTime(c.startedAt) },
    {
      key: 'actions',
      header: 'Acciones',
      render: (c: Conversation) => (
        <div className="flex gap-2">
          <Button variant="ghost" size="sm" onClick={() => navigate(`/conversations/${c.id}`)}>
            <Eye className="h-4 w-4" />
          </Button>
        </div>
      ),
    },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Conversaciones</h1>
      </div>
      <Card>
        <DataTable
          columns={columns}
          data={data?.items ?? []}
          page={data?.page ?? 1}
          totalPages={data?.totalPages ?? 1}
          onPageChange={setPage}
          isLoading={isLoading}
        />
      </Card>
    </div>
  );
}
