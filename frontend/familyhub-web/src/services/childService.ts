import apiClient from '../api/client';
import type { ChildRequest, ChildResponse } from '../types';

export const childService = {
  async list(familyId: string): Promise<ChildResponse[]> {
    const { data } = await apiClient.get<ChildResponse[]>(`/families/${familyId}/children`);
    return data;
  },

  async create(familyId: string, payload: ChildRequest): Promise<ChildResponse> {
    const { data } = await apiClient.post<ChildResponse>(`/families/${familyId}/children`, payload);
    return data;
  },

  async update(familyId: string, childId: string, payload: ChildRequest): Promise<ChildResponse> {
    const { data } = await apiClient.put<ChildResponse>(
      `/families/${familyId}/children/${childId}`,
      payload,
    );
    return data;
  },

  async remove(familyId: string, childId: string): Promise<void> {
    await apiClient.delete(`/families/${familyId}/children/${childId}`);
  },
};
