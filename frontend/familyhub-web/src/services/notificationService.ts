import apiClient from '../api/client';
import type { NotificationResponse, UnreadCountResponse } from '../types';

export const notificationService = {
  async list(): Promise<NotificationResponse[]> {
    const { data } = await apiClient.get<NotificationResponse[]>('/notifications');
    return data;
  },

  async getUnreadCount(): Promise<number> {
    const { data } = await apiClient.get<UnreadCountResponse>('/notifications/unread-count');
    return data.unreadCount;
  },

  async markAsRead(notificationId: string): Promise<void> {
    await apiClient.patch(`/notifications/${notificationId}/read`);
  },

  async markAllAsRead(): Promise<void> {
    await apiClient.patch('/notifications/read-all');
  },
};
