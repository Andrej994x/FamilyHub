import apiClient from '../api/client';
import type {
  CreateInvitationRequest,
  CreatedInvitationResponse,
  InvitationResponse,
} from '../types';

export const invitationService = {
  async create(
    familyId: string,
    payload: CreateInvitationRequest,
  ): Promise<CreatedInvitationResponse> {
    const { data } = await apiClient.post<CreatedInvitationResponse>(
      `/families/${familyId}/invitations`,
      payload,
    );
    return data;
  },

  async list(familyId: string): Promise<InvitationResponse[]> {
    const { data } = await apiClient.get<InvitationResponse[]>(`/families/${familyId}/invitations`);
    return data;
  },

  async cancel(familyId: string, invitationId: string): Promise<void> {
    await apiClient.delete(`/families/${familyId}/invitations/${invitationId}`);
  },

  async accept(token: string): Promise<InvitationResponse> {
    const { data } = await apiClient.post<InvitationResponse>(`/invitations/${token}/accept`);
    return data;
  },
};
