import apiClient from '../api/client';
import type { TaskFilter, TaskRequest, TaskResponse } from '../types';

export const taskService = {
  async list(familyId: string, filter?: TaskFilter): Promise<TaskResponse[]> {
    const { data } = await apiClient.get<TaskResponse[]>(`/families/${familyId}/tasks`, {
      params: filter,
    });
    return data;
  },

  async create(familyId: string, payload: TaskRequest): Promise<TaskResponse> {
    const { data } = await apiClient.post<TaskResponse>(`/families/${familyId}/tasks`, payload);
    return data;
  },

  async update(familyId: string, taskId: string, payload: TaskRequest): Promise<TaskResponse> {
    const { data } = await apiClient.put<TaskResponse>(
      `/families/${familyId}/tasks/${taskId}`,
      payload,
    );
    return data;
  },

  async updateStatus(familyId: string, taskId: string, status: number): Promise<TaskResponse> {
    const { data } = await apiClient.patch<TaskResponse>(
      `/families/${familyId}/tasks/${taskId}/status`,
      { status },
    );
    return data;
  },

  async remove(familyId: string, taskId: string): Promise<void> {
    await apiClient.delete(`/families/${familyId}/tasks/${taskId}`);
  },
};
