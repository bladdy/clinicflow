import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import api from '../services/api';
import type { ApiResponse, PagedResult, PaginationParams } from '../types';

export function usePagedQuery<T>(
  key: string,
  endpoint: string,
  params: PaginationParams & Record<string, string | number | undefined>
) {
  return useQuery({
    queryKey: [key, params],
    queryFn: async () => {
      const response = await api.get<ApiResponse<PagedResult<T>>>(endpoint, { params });
      return response.data.data!;
    },
  });
}

export function useDetailQuery<T>(key: string, endpoint: string, id: string) {
  return useQuery({
    queryKey: [key, id],
    queryFn: async () => {
      const response = await api.get<ApiResponse<T>>(`${endpoint}/${id}`);
      return response.data.data!;
    },
    enabled: !!id,
  });
}

export function useCreateMutation<TInput, TOutput>(key: string, endpoint: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (data: TInput) => {
      const response = await api.post<ApiResponse<TOutput>>(endpoint, data);
      if (!response.data.success) throw new Error(response.data.message);
      return response.data.data!;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [key] }),
  });
}

export function useUpdateMutation<TInput, TOutput>(key: string, endpoint: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, data }: { id: string; data: TInput }) => {
      const response = await api.put<ApiResponse<TOutput>>(`${endpoint}/${id}`, data);
      if (!response.data.success) throw new Error(response.data.message);
      return response.data.data!;
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [key] }),
  });
}

export function useDeleteMutation(key: string, endpoint: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      const response = await api.delete<ApiResponse<null>>(`${endpoint}/${id}`);
      if (!response.data.success) throw new Error(response.data.message);
    },
    onSuccess: () => queryClient.invalidateQueries({ queryKey: [key] }),
  });
}
