import apiClient from '../api/client';
import type { PickupResponse } from '../types';

export const pickupService = {
  async list(familyId: string): Promise<PickupResponse[]> {
    const { data } = await apiClient.get<PickupResponse[]>(`/families/${familyId}/pickups`);
    return data;
  },
};
