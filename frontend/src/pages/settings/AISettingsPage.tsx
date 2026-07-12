import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import api from '../../services/api';
import { Button } from '../../components/ui/Button';
import { Input } from '../../components/ui/Input';
import { Card, CardHeader, CardContent } from '../../components/ui/Card';
import { Bot, Save, Power } from 'lucide-react';
import type { ApiResponse } from '../../types';

const aiSettingsSchema = z.object({
  ollamaUrl: z.string().min(1, 'La URL de Ollama es requerida'),
  modelName: z.string().min(1, 'El nombre del modelo es requerido'),
  systemPrompt: z.string().min(10, 'El prompt del sistema debe tener al menos 10 caracteres'),
  maxTokens: z.number().min(50).max(2000),
  temperature: z.number().min(0).max(2),
  isEnabled: z.boolean(),
  welcomeMessage: z.string().optional(),
  transferMessage: z.string().optional(),
});

type AISettingsFormData = z.infer<typeof aiSettingsSchema>;

interface AISettings {
  id: string;
  companyId: string;
  ollamaUrl: string;
  modelName: string;
  systemPrompt: string;
  maxTokens: number;
  temperature: number;
  isEnabled: boolean;
  welcomeMessage?: string;
  transferMessage?: string;
}

export function AISettingsPage() {
  const queryClient = useQueryClient();

  const { data: settings, isLoading } = useQuery({
    queryKey: ['ai-settings'],
    queryFn: async () => {
      const res = await api.get<ApiResponse<AISettings>>('/ai-settings');
      return res.data.data!;
    },
  });

  const { register, handleSubmit, watch, formState: { isDirty } } = useForm<AISettingsFormData>({
    resolver: zodResolver(aiSettingsSchema),
    values: settings ? {
      ollamaUrl: settings.ollamaUrl,
      modelName: settings.modelName,
      systemPrompt: settings.systemPrompt,
      maxTokens: settings.maxTokens,
      temperature: settings.temperature,
      isEnabled: settings.isEnabled,
      welcomeMessage: settings.welcomeMessage ?? '',
      transferMessage: settings.transferMessage ?? '',
    } : undefined,
  });

  const mutation = useMutation({
    mutationFn: async (data: AISettingsFormData) => {
      const res = await api.put<ApiResponse<AISettings>>('/ai-settings', data);
      return res.data.data!;
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['ai-settings'] });
    },
  });

  const isEnabled = watch('isEnabled');

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-12">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600" />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Configuración de IA</h1>
          <p className="text-sm text-gray-500 mt-1">Configura el asistente virtual para WhatsApp</p>
        </div>
        <div className={`flex items-center gap-2 px-3 py-1.5 rounded-full text-sm font-medium ${isEnabled ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-600'}`}>
          <Power className="h-4 w-4" />
          {isEnabled ? 'Activo' : 'Inactivo'}
        </div>
      </div>

      <form onSubmit={handleSubmit((data) => mutation.mutate(data))} className="space-y-6">
        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold flex items-center gap-2">
              <Bot className="h-5 w-5" />
              Conexión con Ollama
            </h2>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <Input label="URL de Ollama" {...register('ollamaUrl')} placeholder="http://localhost:11434" />
              <Input label="Modelo" {...register('modelName')} placeholder="qwen3:4b" />
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <Input label="Máximo de tokens" type="number" {...register('maxTokens', { valueAsNumber: true })} />
              <Input label="Temperatura (0-2)" type="number" step="0.1" {...register('temperature', { valueAsNumber: true })} />
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold">Comportamiento del Bot</h2>
          </CardHeader>
          <CardContent className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">Prompt del Sistema</label>
              <textarea
                {...register('systemPrompt')}
                rows={6}
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                placeholder="Instrucciones para el asistente virtual..."
              />
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Mensaje de bienvenida</label>
                <textarea
                  {...register('welcomeMessage')}
                  rows={2}
                  className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                  placeholder="¡Hola! Soy el asistente virtual..."
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Mensaje de transferencia</label>
                <textarea
                  {...register('transferMessage')}
                  rows={2}
                  className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                  placeholder="Un representante se comunicará contigo..."
                />
              </div>
            </div>
          </CardContent>
        </Card>

        <Card>
          <CardContent>
            <div className="flex items-center justify-between">
              <div>
                <h3 className="font-medium text-gray-900">Activar asistente IA</h3>
                <p className="text-sm text-gray-500">Cuando está activo, el bot responderá automáticamente via WhatsApp</p>
              </div>
              <label className="relative inline-flex items-center cursor-pointer">
                <input type="checkbox" {...register('isEnabled')} className="sr-only peer" />
                <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-blue-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-blue-600" />
              </label>
            </div>
          </CardContent>
        </Card>

        <div className="flex justify-end">
          <Button type="submit" isLoading={mutation.isPending} disabled={!isDirty}>
            <Save className="h-4 w-4 mr-2" />
            Guardar Configuración
          </Button>
        </div>
      </form>
    </div>
  );
}
