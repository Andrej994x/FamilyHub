import apiClient from '../api/client';
import type { CreateFamilyRequest, FamilyMemberResponse, FamilyResponse } from '../types';

export const familyService = {
  async getCurrent(): Promise<FamilyResponse> {
    const { data } = await apiClient.get<FamilyResponse>('/families/current');
    return data;
  },

  async create(payload: CreateFamilyRequest): Promise<FamilyResponse> {
    const { data } = await apiClient.post<FamilyResponse>('/families', payload);
    return data;
  },

  async getMembers(familyId: string): Promise<FamilyMemberResponse[]> {
    const { data } = await apiClient.get<FamilyMemberResponse[]>(`/families/${familyId}/members`);
    return data;
  },

  async removeMember(familyId: string, memberId: string): Promise<void> {
    await apiClient.delete(`/families/${familyId}/members/${memberId}`);
  },
};
