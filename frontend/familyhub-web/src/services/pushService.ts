import apiClient from '../api/client';
import type {
  PushSubscriptionResponse,
  RegisterPushRequest,
  VapidPublicKeyResponse,
} from '../types/push';

export const pushService = {
  async getVapidPublicKey(): Promise<string> {
    const { data } = await apiClient.get<VapidPublicKeyResponse>('/push/vapid-public-key');
    return data.publicKey;
  },

  async list(): Promise<PushSubscriptionResponse[]> {
    const { data } = await apiClient.get<PushSubscriptionResponse[]>('/push/subscriptions');
    return data;
  },

  async register(request: RegisterPushRequest): Promise<PushSubscriptionResponse> {
    const { data } = await apiClient.post<PushSubscriptionResponse>('/push/subscriptions', request);
    return data;
  },

  async remove(id: string): Promise<void> {
    await apiClient.delete(`/push/subscriptions/${id}`);
  },

  /** Deletes the backend row for a browser endpoint (the browser only knows the endpoint). */
  async removeByEndpoint(endpoint: string): Promise<void> {
    const subscriptions = await pushService.list();
    const match = subscriptions.find((s) => s.endpoint === endpoint);
    if (match) {
      await pushService.remove(match.id);
    }
  },
};
