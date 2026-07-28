import apiClient from '../api/client';
import type { ReminderPreference, UpdateReminderPreferenceRequest } from '../types/reminders';

export const reminderService = {
  async getAll(): Promise<ReminderPreference[]> {
    const { data } = await apiClient.get<ReminderPreference[]>('/reminder-preferences');
    return data;
  },

  async update(category: string, request: UpdateReminderPreferenceRequest): Promise<ReminderPreference> {
    const { data } = await apiClient.put<ReminderPreference>(`/reminder-preferences/${category}`, request);
    return data;
  },

  async resetDefaults(): Promise<ReminderPreference[]> {
    const { data } = await apiClient.post<ReminderPreference[]>('/reminder-preferences/reset-defaults');
    return data;
  },
};
