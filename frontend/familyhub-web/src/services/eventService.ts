import apiClient from '../api/client';
import type { EventFilter, EventResponse } from '../types';

export const eventService = {
  async list(familyId: string, filter?: EventFilter): Promise<EventResponse[]> {
    const { data } = await apiClient.get<EventResponse[]>(`/families/${familyId}/events`, {
      params: filter,
    });
    return data;
  },
};
