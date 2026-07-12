import { useState } from 'react';
import { Plus, Pencil, Trash2 } from 'lucide-react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Button } from '../../components/ui/Button';
import { Card } from '../../components/ui/Card';
import { DataTable } from '../../components/ui/DataTable';
import { Input } from '../../components/ui/Input';
import { Modal } from '../../components/ui/Modal';
import { usePagedQuery, useCreateMutation, useUpdateMutation, useDeleteMutation } from '../../hooks/useApi';
import type { KnowledgeArticle } from '../../types';

const knowledgeArticleSchema = z.object({
  title: z.string().min(1, 'El título es requerido'),
  content: z.string().min(1, 'El contenido es requerido'),
  category: z.string().optional(),
  keywords: z.string().optional(),
  isActive: z.boolean(),
});

type KnowledgeArticleFormData = z.infer<typeof knowledgeArticleSchema>;

export function KnowledgeBasePage() {
  const [page, setPage] = useState(1);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showEditModal, setShowEditModal] = useState(false);
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);
  const [editingArticle, setEditingArticle] = useState<KnowledgeArticle | null>(null);
  const [deletingArticle, setDeletingArticle] = useState<KnowledgeArticle | null>(null);

  const { data, isLoading } = usePagedQuery<KnowledgeArticle>('knowledge-articles', '/knowledgearticles', { page, pageSize: 10 });
  const createMutation = useCreateMutation<KnowledgeArticleFormData, KnowledgeArticle>('knowledge-articles', '/knowledgearticles');
  const updateMutation = useUpdateMutation<KnowledgeArticleFormData, KnowledgeArticle>('knowledge-articles', '/knowledgearticles');
  const deleteMutation = useDeleteMutation('knowledge-articles', '/knowledgearticles');

  const createForm = useForm<KnowledgeArticleFormData>({
    resolver: zodResolver(knowledgeArticleSchema),
    defaultValues: {
      title: '',
      content: '',
      category: '',
      keywords: '',
      isActive: true,
    },
  });

  const editForm = useForm<KnowledgeArticleFormData>({
    resolver: zodResolver(knowledgeArticleSchema),
  });

  const handleCreate = async (formData: KnowledgeArticleFormData) => {
    await createMutation.mutateAsync(formData);
    setShowCreateModal(false);
    createForm.reset();
  };

  const openEdit = (article: KnowledgeArticle) => {
    setEditingArticle(article);
    editForm.reset({
      title: article.title,
      content: article.content,
      category: article.category ?? '',
      keywords: article.keywords ?? '',
      isActive: article.isActive,
    });
    setShowEditModal(true);
  };

  const handleUpdate = async (formData: KnowledgeArticleFormData) => {
    if (!editingArticle) return;
    await updateMutation.mutateAsync({ id: editingArticle.id, data: formData });
    setShowEditModal(false);
    setEditingArticle(null);
  };

  const handleDelete = async () => {
    if (!deletingArticle) return;
    await deleteMutation.mutateAsync(deletingArticle.id);
    setShowDeleteConfirm(false);
    setDeletingArticle(null);
  };

  const columns = [
    { key: 'title', header: 'Título' },
    { key: 'category', header: 'Categoría', render: (a: KnowledgeArticle) => a.category || '—' },
    { key: 'isActive', header: 'Estado', render: (a: KnowledgeArticle) => (
      <span className={`inline-flex px-2 py-1 rounded-full text-xs font-medium ${a.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'}`}>
        {a.isActive ? 'Activo' : 'Inactivo'}
      </span>
    )},
    {
      key: 'actions',
      header: 'Acciones',
      render: (a: KnowledgeArticle) => (
        <div className="flex gap-2">
          <Button variant="ghost" size="sm" onClick={() => openEdit(a)}>
            <Pencil className="h-4 w-4" />
          </Button>
          <Button variant="ghost" size="sm" onClick={() => { setDeletingArticle(a); setShowDeleteConfirm(true); }}>
            <Trash2 className="h-4 w-4 text-red-600" />
          </Button>
        </div>
      ),
    },
  ];

  const formFields = (form: ReturnType<typeof useForm<KnowledgeArticleFormData>>) => (
    <div className="space-y-4">
      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Input label="Título" error={form.formState.errors.title?.message} {...form.register('title')} />
        <Input label="Categoría" {...form.register('category')} />
      </div>
      <Input label="Palabras Clave" placeholder="separadas por coma" {...form.register('keywords')} />
      <div className="space-y-1">
        <label className="block text-sm font-medium text-gray-700">Contenido</label>
        <textarea
          className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
          rows={4}
          {...form.register('content')}
        />
        {form.formState.errors.content && (
          <p className="text-sm text-red-600">{form.formState.errors.content.message}</p>
        )}
      </div>
      <div className="flex items-center gap-2">
        <input type="checkbox" id="isActive" className="rounded border-gray-300" {...form.register('isActive')} />
        <label htmlFor="isActive" className="text-sm font-medium text-gray-700">Activo</label>
      </div>
    </div>
  );

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Base de Conocimiento</h1>
        <Button onClick={() => setShowCreateModal(true)}>
          <Plus className="h-4 w-4 mr-2" />
          Nuevo Artículo
        </Button>
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

      <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="Nuevo Artículo" size="lg">
        <form onSubmit={createForm.handleSubmit(handleCreate)} className="space-y-4">
          {formFields(createForm)}
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" type="button" onClick={() => setShowCreateModal(false)}>Cancelar</Button>
            <Button type="submit" isLoading={createMutation.isPending}>Guardar</Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={showEditModal} onClose={() => setShowEditModal(false)} title="Editar Artículo" size="lg">
        <form onSubmit={editForm.handleSubmit(handleUpdate)} className="space-y-4">
          {formFields(editForm)}
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" type="button" onClick={() => setShowEditModal(false)}>Cancelar</Button>
            <Button type="submit" isLoading={updateMutation.isPending}>Actualizar</Button>
          </div>
        </form>
      </Modal>

      <Modal isOpen={showDeleteConfirm} onClose={() => setShowDeleteConfirm(false)} title="Eliminar Artículo" size="sm">
        <p className="text-sm text-gray-600 mb-4">
          ¿Estás seguro de que deseas eliminar el artículo <strong>{deletingArticle?.title}</strong>? Esta acción no se puede deshacer.
        </p>
        <div className="flex justify-end gap-2">
          <Button variant="secondary" onClick={() => setShowDeleteConfirm(false)}>Cancelar</Button>
          <Button variant="danger" onClick={handleDelete} isLoading={deleteMutation.isPending}>Eliminar</Button>
        </div>
      </Modal>
    </div>
  );
}
