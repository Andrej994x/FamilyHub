import apiClient from '../api/client';
import type { PickupRequest, PickupResponse } from '../types';

export const pickupService = {
  async list(familyId: string): Promise<PickupResponse[]> {
    const { data } = await apiClient.get<PickupResponse[]>(`/families/${familyId}/pickups`);
    return data;
  },

  async create(familyId: string, payload: PickupRequest): Promise<PickupResponse> {
    const { data } = await apiClient.post<PickupResponse>(`/families/${familyId}/pickups`, payload);
    return data;
  },

  async update(familyId: string, pickupId: string, payload: PickupRequest): Promise<PickupResponse> {
    const { data } = await apiClient.put<PickupResponse>(
      `/families/${familyId}/pickups/${pickupId}`,
      payload,
    );
    return data;
  },

  async updateStatus(familyId: string, pickupId: string, status: number): Promise<PickupResponse> {
    const { data } = await apiClient.patch<PickupResponse>(
      `/families/${familyId}/pickups/${pickupId}/status`,
      { status },
    );
    return data;
  },

  async takeOver(familyId: string, pickupId: string): Promise<PickupResponse> {
    const { data } = await apiClient.patch<PickupResponse>(
      `/families/${familyId}/pickups/${pickupId}/take-over`,
    );
    return data;
  },

  async remove(familyId: string, pickupId: string): Promise<void> {
    await apiClient.delete(`/families/${familyId}/pickups/${pickupId}`);
  },
};
