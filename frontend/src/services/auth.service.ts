import api from './api';
import type { LoginRequest, RegisterRequest, AuthResponse, ApiResponse } from '../types';

export const authService = {
  login: async (data: LoginRequest): Promise<AuthResponse> => {
    const response = await api.post<ApiResponse<AuthResponse>>('/auth/login', data);
    if (!response.data.success) throw new Error(response.data.message);
    return response.data.data!;
  },

  register: async (data: RegisterRequest): Promise<AuthResponse> => {
    const response = await api.post<ApiResponse<AuthResponse>>('/auth/register', data);
    if (!response.data.success) throw new Error(response.data.message);
    return response.data.data!;
  },

  refresh: async (token: string, refreshToken: string): Promise<AuthResponse> => {
    const response = await api.post<ApiResponse<AuthResponse>>('/auth/refresh', { token, refreshToken });
    if (!response.data.success) throw new Error(response.data.message);
    return response.data.data!;
  },

  logout: async (refreshToken: string): Promise<void> => {
    await api.post('/auth/revoke', { refreshToken });
  },
};
