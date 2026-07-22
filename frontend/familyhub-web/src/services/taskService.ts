import apiClient from '../api/client';
import type { TaskFilter, TaskResponse } from '../types';

export const taskService = {
  async list(familyId: string, filter?: TaskFilter): Promise<TaskResponse[]> {
    const { data } = await apiClient.get<TaskResponse[]>(`/families/${familyId}/tasks`, {
      params: filter,
    });
    return data;
  },
};
