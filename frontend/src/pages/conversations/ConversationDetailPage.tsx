import { useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft } from 'lucide-react';
import { useDetailQuery } from '../../hooks/useApi';
import type { ConversationDetail } from '../../types';
import { Card, CardHeader, CardContent } from '../../components/ui/Card';
import { Button } from '../../components/ui/Button';
import { statusColors, statusLabels, formatDateTime } from '../../utils/formatters';

export function ConversationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { data: conversation, isLoading } = useDetailQuery<ConversationDetail>('conversations', '/conversations', id ?? '');

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
      </div>
    );
  }

  if (!conversation) {
    return (
      <div className="text-center py-12 text-gray-500">Conversación no encontrada</div>
    );
  }

  const senderTypeColors: Record<string, string> = {
    Patient: 'bg-gray-100 text-gray-800',
    Bot: 'bg-blue-100 text-blue-800',
    Human: 'bg-green-100 text-green-800',
  };

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-4">
        <Button variant="ghost" size="sm" onClick={() => navigate('/conversations')}>
          <ArrowLeft className="h-4 w-4 mr-1" />
          Volver
        </Button>
        <h1 className="text-2xl font-bold text-gray-900">Detalle de Conversación</h1>
      </div>

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div>
              <p className="text-sm text-gray-500">Teléfono: <span className="font-medium text-gray-900">{conversation.phone}</span></p>
              {conversation.patient && (
                <p className="text-sm text-gray-500">Paciente: <span className="font-medium text-gray-900">{conversation.patient.firstName} {conversation.patient.lastName}</span></p>
              )}
            </div>
            <span className={`inline-flex px-2 py-1 rounded-full text-xs font-medium ${statusColors[conversation.status] ?? ''}`}>
              {statusLabels[conversation.status] ?? conversation.status}
            </span>
          </div>
          <p className="text-xs text-gray-400 mt-1">Inicio: {formatDateTime(conversation.startedAt)}</p>
        </CardHeader>
      </Card>

      <Card>
        <CardContent>
          <div className="space-y-4 max-h-[500px] overflow-y-auto">
            {(conversation.messages ?? []).length === 0 && (
              <p className="text-center text-gray-500 py-8">No hay mensajes</p>
            )}
            {(conversation.messages ?? []).map((msg) => (
              <div
                key={msg.id}
                className={`flex ${msg.senderType === 'Patient' ? 'justify-start' : 'justify-end'}`}
              >
                <div className={`max-w-xs lg:max-w-md px-4 py-2 rounded-xl break-words ${
                  msg.senderType === 'Patient'
                    ? 'bg-gray-100 text-gray-800'
                    : msg.senderType === 'Bot'
                    ? 'bg-blue-500 text-white'
                    : 'bg-green-500 text-white'
                }`}>
                  <p className="text-sm whitespace-pre-wrap">{msg.content}</p>
                  <div className="flex items-center justify-between mt-1">
                    <span className={`text-[10px] px-1.5 py-0.5 rounded-full ${senderTypeColors[msg.senderType] ?? 'bg-gray-100 text-gray-600'}`}>
                      {msg.senderType}
                    </span>
                    <span className="text-[10px] opacity-70">
                      {new Date(msg.sentAt).toLocaleTimeString('es-MX', { hour: '2-digit', minute: '2-digit' })}
                    </span>
                  </div>
                </div>
              </div>
            ))}
          </div>
        </CardContent>
      </Card>
    </div>
  );
}
