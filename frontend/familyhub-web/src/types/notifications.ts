/** Mirrors the backend NotificationType enum (stored/serialised as its ordinal). */
export const NotificationType = {
  Family: 0,
  Task: 1,
  Calendar: 2,
  Shopping: 3,
  FamilyVault: 4,
  System: 5,
} as const;

export type NotificationType = (typeof NotificationType)[keyof typeof NotificationType];

export interface NotificationResponse {
  id: string;
  familyId: string | null;
  type: number;
  title: string;
  message: string;
  /** Client-relative deep link (e.g. "/tasks"), or null when there is no destination. */
  relatedUrl: string | null;
  isRead: boolean;
  createdAt: string;
}

export interface UnreadCountResponse {
  unreadCount: number;
}
