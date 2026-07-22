export interface NotificationResponse {
  id: string;
  title: string;
  message: string;
  type: number;
  relatedEntityId: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface UnreadCountResponse {
  unreadCount: number;
}
