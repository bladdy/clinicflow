import { useState } from 'react';
import { Plus, Trash2 } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Button } from '../../components/ui/Button';
import { Card, CardContent } from '../../components/ui/Card';
import { Input } from '../../components/ui/Input';
import { Modal } from '../../components/ui/Modal';
import { usePagedQuery, useCreateMutation, useDeleteMutation } from '../../hooks/useApi';
import type { WhatsAppInstance } from '../../types';

const whatsappInstanceSchema = z.object({
  instanceName: z.string().min(1, 'El nombre es requerido'),
  apiUrl: z.string().url('URL inválida'),
  apiKey: z.string().min(1, 'La API key es requerida'),
  phoneNumber: z.string().min(1, 'El teléfono es requerido'),
});

type WhatsAppInstanceFormData = z.infer<typeof whatsappInstanceSchema>;

export function WhatsAppSettingsPage() {
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [deletingInstance, setDeletingInstance] = useState<WhatsAppInstance | null>(null);

  const { data, isLoading } = usePagedQuery<WhatsAppInstance>('whatsapp-instances', '/whatsapp/instances', { page: 1, pageSize: 10 });
  const createMutation = useCreateMutation<WhatsAppInstanceFormData, WhatsAppInstance>('whatsapp-instances', '/whatsapp/instances');
  const deleteMutation = useDeleteMutation('whatsapp-instances', '/whatsapp/instances');

  const createForm = useForm<WhatsAppInstanceFormData>({
    resolver: zodResolver(whatsappInstanceSchema),
    defaultValues: {
      instanceName: '',
      apiUrl: '',
      apiKey: '',
      phoneNumber: '',
    },
  });

  const handleCreate = async (formData: WhatsAppInstanceFormData) => {
    await createMutation.mutateAsync(formData);
    setShowCreateModal(false);
    createForm.reset();
  };

  const handleDelete = async () => {
    if (!deletingInstance) return;
    await deleteMutation.mutateAsync(deletingInstance.id);
    setShowDeleteConfirm(false);
    setDeletingInstance(null);
  };

  const instances = data?.items ?? [];

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
            <Card key={instance.id}>
              <CardContent>
                <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
                  <div className="flex items-center gap-4">
                    <div className={`h-3 w-3 rounded-full ${instance.isActive ? 'bg-green-500' : 'bg-red-500'}`} />
                    <div>
                      <p className="font-medium text-gray-900">{instance.instanceName}</p>
                      <p className="text-sm text-gray-500">{instance.phoneNumber}</p>
                      <p className="text-xs text-gray-400 mt-1 break-all">{instance.apiUrl}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-3">
                    <span className={`inline-flex px-2 py-1 rounded-full text-xs font-medium ${instance.isActive ? 'bg-green-100 text-green-800' : 'bg-red-100 text-red-800'}`}>
                      {instance.isActive ? 'Conectado' : 'Desconectado'}
                    </span>
                    <Button variant="ghost" size="sm" onClick={() => { setDeletingInstance(instance); setShowDeleteConfirm(true); }}>
                      <Trash2 className="h-4 w-4 text-red-600" />
                    </Button>
                  </div>
                </div>
              </CardContent>
            </Card>
          ))}
        </div>
      )}

      <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="Nueva Instancia WhatsApp" size="lg">
        <form onSubmit={createForm.handleSubmit(handleCreate)} className="space-y-4">
          <Input label="Nombre de Instancia" error={createForm.formState.errors.instanceName?.message} {...createForm.register('instanceName')} />
          <Input label="API URL" placeholder="https://api.example.com" error={createForm.formState.errors.apiUrl?.message} {...createForm.register('apiUrl')} />
          <Input label="API Key" type="password" error={createForm.formState.errors.apiKey?.message} {...createForm.register('apiKey')} />
          <Input label="Número de Teléfono" placeholder="+521234567890" error={createForm.formState.errors.phoneNumber?.message} {...createForm.register('phoneNumber')} />
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" type="button" onClick={() => setShowCreateModal(false)}>Cancelar</Button>
            <Button type="submit" isLoading={createMutation.isPending}>Guardar</Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={showDeleteConfirm} onClose={() => setShowDeleteConfirm(false)} title="Eliminar Instancia" size="sm">
        <p className="text-sm text-gray-600 mb-4">
          ¿Estás seguro de que deseas eliminar la instancia <strong>{deletingInstance?.instanceName}</strong>? Esta acción no se puede deshacer.
        </p>
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setShowDeleteConfirm(false)}>Cancelar</Button>
          <Button variant="danger" onClick={handleDelete} isLoading={deleteMutation.isPending}>Eliminar</Button>
        </div>
      </Modal>
    </div>
  );
}
