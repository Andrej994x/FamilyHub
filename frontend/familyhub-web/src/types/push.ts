export interface PushSubscriptionResponse {
  id: string;
  endpoint: string;
  userAgent: string | null;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  lastUsedAt: string | null;
}

export interface VapidPublicKeyResponse {
  publicKey: string;
}

export interface RegisterPushRequest {
  endpoint: string;
  p256dh: string;
  auth: string;
  userAgent?: string;
}
