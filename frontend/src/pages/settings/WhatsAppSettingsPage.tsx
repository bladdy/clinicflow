import { useState, useEffect, useCallback } from 'react';
import { Plus, Trash2, QrCode, CheckCircle, XCircle, Loader2, RefreshCw, Wifi, WifiOff, RotateCcw, LogOut, AlertTriangle, Shield, Webhook } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Button } from '../../components/ui/Button';
import { Card, CardContent } from '../../components/ui/Card';
import { Input } from '../../components/ui/Input';
import { Modal } from '../../components/ui/Modal';
import api from '../../services/api';
import type { ApiResponse } from '../../types';
import type { WhatsAppInstance, WhatsAppInstanceDetail } from '../../types';

const whatsappInstanceSchema = z.object({
  instanceName: z.string().min(1, 'El nombre es requerido'),
  apiUrl: z.string().url('URL inválida'),
  apiKey: z.string().min(1, 'La API key es requerida'),
  phoneNumber: z.string().min(1, 'El teléfono es requerido'),
});

type WhatsAppInstanceFormData = z.infer<typeof whatsappInstanceSchema>;

export function WhatsAppSettingsPage() {
  const queryClient = useQueryClient();
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [showQrModal, setShowQrModal] = useState(false);
  const [selectedInstance, setSelectedInstance] = useState<WhatsAppInstance | null>(null);
  const [deletingInstance, setDeletingInstance] = useState<WhatsAppInstance | null>(null);

  const { data: instances = [], isLoading } = useQuery<WhatsAppInstance[]>({
    queryKey: ['whatsapp-instances'],
    queryFn: async () => {
      const response = await api.get<ApiResponse<WhatsAppInstance[]>>('/whatsapp/instances');
      return response.data.data ?? [];
    },
  });

  const createForm = useForm<WhatsAppInstanceFormData>({
    resolver: zodResolver(whatsappInstanceSchema),
    defaultValues: { instanceName: '', apiUrl: '', apiKey: '', phoneNumber: '' },
  });

  const createMutation = useMutation({
    mutationFn: async (formData: WhatsAppInstanceFormData) => {
      const response = await api.post<ApiResponse<WhatsAppInstance>>('/whatsapp/instances', formData);
      return response.data.data!;
    },
    onSuccess: (newInstance) => {
      setShowCreateModal(false);
      createForm.reset();
      setSelectedInstance(newInstance);
      setShowQrModal(true);
      queryClient.invalidateQueries({ queryKey: ['whatsapp-instances'] });
    },
  });

  const deleteMutation = useMutation({
    mutationFn: async (id: string) => {
      await api.delete(`/whatsapp/instances/${id}`);
    },
    onSuccess: () => {
      setShowDeleteConfirm(false);
      setDeletingInstance(null);
      queryClient.invalidateQueries({ queryKey: ['whatsapp-instances'] });
    },
  });

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Configuración de WhatsApp</h1>
        <Button onClick={() => setShowCreateModal(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Nueva Instancia
        </Button>
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-12">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
        </div>
      ) : instances.length === 0 ? (
        <Card>
          <CardContent>
            <p className="text-center text-gray-500 py-8">No hay instancias de WhatsApp configuradas</p>
          </CardContent>
        </Card>
      ) : (
        <div className="space-y-4">
          {instances.map((instance) => (
            <InstanceCard
              key={instance.id}
              instance={instance}
              onViewQR={() => { setSelectedInstance(instance); setShowQrModal(true); }}
              onDelete={() => { setDeletingInstance(instance); setShowDeleteConfirm(true); }}
            />
          ))}
        </div>
      )}

      <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="Nueva Instancia WhatsApp" size="lg">
        <form onSubmit={createForm.handleSubmit((data) => createMutation.mutate(data))} className="space-y-4">
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input label="Nombre de Instancia" error={createForm.formState.errors.instanceName?.message} {...createForm.register('instanceName')} />
            <Input label="Número de Teléfono" placeholder="521XXXXXXXXXX" error={createForm.formState.errors.phoneNumber?.message} {...createForm.register('phoneNumber')} />
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <Input label="API URL" placeholder="http://localhost:8080" error={createForm.formState.errors.apiUrl?.message} {...createForm.register('apiUrl')} />
            <Input label="API Key" type="password" error={createForm.formState.errors.apiKey?.message} {...createForm.register('apiKey')} />
          </div>
          {createMutation.isError && (
            <p className="text-sm text-red-600">Error al crear la instancia. Verifica los datos.</p>
          )}
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" type="button" onClick={() => setShowCreateModal(false)}>Cancelar</Button>
            <Button type="submit" isLoading={createMutation.isPending}>Crear y Conectar</Button>
          </div>
        </form>
      </Modal>

      <QrModal
        isOpen={showQrModal}
        onClose={() => { setShowQrModal(false); setSelectedInstance(null); }}
        instance={selectedInstance}
      />

      <Modal isOpen={showDeleteConfirm} onClose={() => setShowDeleteConfirm(false)} title="Eliminar Instancia" size="sm">
        <p className="text-sm text-gray-600 mb-4">
          ¿Estás seguro de que deseas eliminar la instancia <strong>{deletingInstance?.instanceName}</strong>? Se eliminará de Evolution API y de la base de datos.
        </p>
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setShowDeleteConfirm(false)}>Cancelar</Button>
          <Button variant="danger" onClick={() => deletingInstance && deleteMutation.mutate(deletingInstance.id)} isLoading={deleteMutation.isPending}>Eliminar</Button>
        </div>
      </Modal>
    </div>
  );
}

function InstanceCard({ instance, onViewQR, onDelete }: { instance: WhatsAppInstance; onViewQR: () => void; onDelete: () => void }) {
  const queryClient = useQueryClient();

  const { data: detail } = useQuery<WhatsAppInstanceDetail>({
    queryKey: ['whatsapp-instance', instance.id],
    queryFn: async () => {
      const res = await api.get<ApiResponse<WhatsAppInstanceDetail>>(`/whatsapp/instances/${instance.id}`);
      return res.data.data!;
    },
    refetchInterval: 10000,
  });

  const restartMutation = useMutation({
    mutationFn: async () => { await api.post(`/whatsapp/instances/${instance.id}/restart`); },
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['whatsapp-instance', instance.id] }); },
  });

  const logoutMutation = useMutation({
    mutationFn: async () => { await api.post(`/whatsapp/instances/${instance.id}/logout`); },
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['whatsapp-instance', instance.id] }); },
  });

  const fixWebhookMutation = useMutation({
    mutationFn: async () => { await api.post(`/whatsapp/instances/${instance.id}/webhook/fix`); },
    onSuccess: () => { queryClient.invalidateQueries({ queryKey: ['whatsapp-instance', instance.id] }); },
  });

  const state = detail?.connectionState ?? 'unknown';
  const isConnected = state === 'open';
  const webhookOk = detail?.webhookConfigured ?? false;

  return (
    <Card>
      <CardContent>
        <div className="space-y-4">
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
            <div className="flex items-center gap-4">
              <div className={`h-3 w-3 rounded-full flex-shrink-0 ${isConnected ? 'bg-green-500' : state === 'connecting' ? 'bg-yellow-500 animate-pulse' : 'bg-red-500'}`} />
              <div>
                <p className="font-medium text-gray-900">{instance.instanceName}</p>
                <p className="text-sm text-gray-500">{instance.phoneNumber || 'Sin número'}</p>
              </div>
            </div>
            <div className="flex items-center gap-2">
              <span className={`inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium ${isConnected ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
                {isConnected ? <Wifi className="h-3 w-3" /> : <WifiOff className="h-3 w-3" />}
                {isConnected ? 'Conectado' : state === 'connecting' ? 'Conectando' : 'Desconectado'}
              </span>
              <span className={`inline-flex items-center gap-1 px-2 py-1 rounded-full text-xs font-medium ${webhookOk ? 'bg-blue-100 text-blue-800' : 'bg-orange-100 text-orange-800'}`}>
                <Webhook className="h-3 w-3" />
                {webhookOk ? 'Webhook OK' : 'Sin Webhook'}
              </span>
            </div>
          </div>

          {detail && detail.webhookEvents.length > 0 && (
            <div className="flex flex-wrap gap-1">
              {detail.webhookEvents.map((evt) => (
                <span key={evt} className="text-[10px] bg-gray-100 text-gray-600 px-1.5 py-0.5 rounded">{evt}</span>
              ))}
            </div>
          )}

          <div className="flex items-center gap-2 flex-wrap">
            <Button variant="ghost" size="sm" onClick={onViewQR}>
              <QrCode className="h-4 w-4 mr-1" /> QR
            </Button>
            <Button variant="ghost" size="sm" onClick={() => restartMutation.mutate()} disabled={restartMutation.isPending}>
              <RotateCcw className="h-4 w-4 mr-1" /> Reconectar
            </Button>
            <Button variant="ghost" size="sm" onClick={() => logoutMutation.mutate()} disabled={logoutMutation.isPending}>
              <LogOut className="h-4 w-4 mr-1" /> Desconectar
            </Button>
            {!webhookOk && (
              <Button variant="ghost" size="sm" onClick={() => fixWebhookMutation.mutate()} disabled={fixWebhookMutation.isPending}>
                <Shield className="h-4 w-4 mr-1" /> Corregir Webhook
              </Button>
            )}
            <Button variant="ghost" size="sm" onClick={onDelete}>
              <Trash2 className="h-4 w-4 text-red-600" />
            </Button>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}

function QrModal({ isOpen, onClose, instance }: { isOpen: boolean; onClose: () => void; instance: WhatsAppInstance | null }) {
  const [qrBase64, setQrBase64] = useState('');
  const [isConnected, setIsConnected] = useState(false);
  const [isChecking, setIsChecking] = useState(false);
  const [isLoadingQr, setIsLoadingQr] = useState(false);

  const fetchQr = useCallback(async () => {
    if (!instance) return;
    setIsLoadingQr(true);
    try {
      const res = await api.get<ApiResponse<string>>(`/whatsapp/instances/${instance.id}/qrcode`);
      setQrBase64(res.data.data ?? '');
    } catch {
      setQrBase64('');
    } finally {
      setIsLoadingQr(false);
    }
  }, [instance]);

  const checkConnection = useCallback(async () => {
    if (!instance) return;
    setIsChecking(true);
    try {
      const res = await api.get<ApiResponse<{ isConnected: boolean; connectionState: string }>>(`/whatsapp/instances/${instance.id}/status`);
      setIsConnected(res.data.data?.isConnected ?? false);
    } catch {
      setIsConnected(false);
    } finally {
      setIsChecking(false);
    }
  }, [instance]);

  useEffect(() => {
    if (isOpen && instance) {
      fetchQr();
      setIsConnected(false);
    }
  }, [isOpen, instance, fetchQr]);

  useEffect(() => {
    if (!isOpen || isConnected) return;
    const interval = setInterval(checkConnection, 5000);
    return () => clearInterval(interval);
  }, [isOpen, isConnected, checkConnection]);

  if (!instance) return null;

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={`Conectar: ${instance.instanceName}`} size="md">
      <div className="flex flex-col items-center gap-4 py-4">
        {isConnected ? (
          <div className="flex flex-col items-center gap-3 py-8">
            <CheckCircle className="h-16 w-16 text-green-500" />
            <p className="text-lg font-semibold text-green-700">¡Conectado!</p>
            <p className="text-sm text-gray-500">Tu WhatsApp está vinculado correctamente</p>
            <Button onClick={onClose}>Cerrar</Button>
          </div>
        ) : (
          <>
            <p className="text-sm text-gray-600 text-center">
              Escanea este código QR con tu WhatsApp para vincular la instancia
            </p>
            <p className="text-xs text-gray-400 text-center">
              WhatsApp → Dispositivos vinculados → Vincular dispositivo
            </p>

            <div className="w-64 h-64 bg-white border-2 border-gray-200 rounded-xl flex items-center justify-center">
              {isLoadingQr ? (
                <Loader2 className="h-8 w-8 text-blue-500 animate-spin" />
              ) : qrBase64 ? (
                <img
                  src={qrBase64.startsWith('data:') ? qrBase64 : `data:image/png;base64,${qrBase64}`}
                  alt="QR Code"
                  className="w-full h-full object-contain p-2"
                />
              ) : (
                <p className="text-sm text-gray-400 text-center px-4">No se pudo cargar el QR. Verifica la conexión con Evolution API.</p>
              )}
            </div>

            <div className="flex gap-2">
              <Button variant="secondary" size="sm" onClick={fetchQr} disabled={isLoadingQr}>
                <RefreshCw className={`h-4 w-4 mr-1 ${isLoadingQr ? 'animate-spin' : ''}`} />
                Regenerar QR
              </Button>
              <Button size="sm" onClick={checkConnection} isLoading={isChecking}>
                Verificar Conexión
              </Button>
            </div>

            <p className="text-xs text-gray-400">
              El estado se verifica automáticamente cada 5 segundos
            </p>
          </>
        )}
      </div>
    </Modal>
  );
}
