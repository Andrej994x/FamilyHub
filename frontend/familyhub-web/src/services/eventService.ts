import apiClient from '../api/client';
import type { EventFilter, EventRequest, EventResponse } from '../types';

export const eventService = {
  async list(familyId: string, filter?: EventFilter): Promise<EventResponse[]> {
    const { data } = await apiClient.get<EventResponse[]>(`/families/${familyId}/events`, {
      params: filter,
    });
    return data;
  },

  async create(familyId: string, payload: EventRequest): Promise<EventResponse> {
    const { data } = await apiClient.post<EventResponse>(`/families/${familyId}/events`, payload);
    return data;
  },

  async update(familyId: string, eventId: string, payload: EventRequest): Promise<EventResponse> {
    const { data } = await apiClient.put<EventResponse>(
      `/families/${familyId}/events/${eventId}`,
      payload,
    );
    return data;
  },

  async remove(familyId: string, eventId: string): Promise<void> {
    await apiClient.delete(`/families/${familyId}/events/${eventId}`);
  },
};
