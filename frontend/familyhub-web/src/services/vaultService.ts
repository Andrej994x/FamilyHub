import type { AxiosRequestConfig } from 'axios';
import apiClient from '../api/client';
import type { VaultRecord } from '../types';

export type VaultCategoryKey =
  | 'documents'
  | 'vehicles'
  | 'pets'
  | 'home'
  | 'warranties'
  | 'other';

// Removing the default JSON content-type lets the browser set the multipart boundary.
const multipartConfig = { headers: { 'Content-Type': null } } as unknown as AxiosRequestConfig;

export interface VaultRecordClient {
  list: (familyId: string) => Promise<VaultRecord[]>;
  get: (familyId: string, id: string) => Promise<VaultRecord>;
  create: (familyId: string, data: FormData) => Promise<VaultRecord>;
  update: (familyId: string, id: string, data: FormData) => Promise<VaultRecord>;
  remove: (familyId: string, id: string) => Promise<void>;
}

function makeClient(basePath: (familyId: string) => string): VaultRecordClient {
  return {
    async list(familyId) {
      const { data } = await apiClient.get<VaultRecord[]>(basePath(familyId));
      return data;
    },
    async get(familyId, id) {
      const { data } = await apiClient.get<VaultRecord>(`${basePath(familyId)}/${id}`);
      return data;
    },
    async create(familyId, formData) {
      const { data } = await apiClient.post<VaultRecord>(basePath(familyId), formData, multipartConfig);
      return data;
    },
    async update(familyId, id, formData) {
      const { data } = await apiClient.put<VaultRecord>(
        `${basePath(familyId)}/${id}`,
        formData,
        multipartConfig,
      );
      return data;
    },
    async remove(familyId, id) {
      await apiClient.delete(`${basePath(familyId)}/${id}`);
    },
  };
}

const clients: Record<VaultCategoryKey, VaultRecordClient> = {
  documents: makeClient((f) => `/families/${f}/documents`),
  vehicles: makeClient((f) => `/families/${f}/vault/vehicles`),
  pets: makeClient((f) => `/families/${f}/vault/pets`),
  home: makeClient((f) => `/families/${f}/vault/home`),
  warranties: makeClient((f) => `/families/${f}/vault/warranties`),
  other: makeClient((f) => `/families/${f}/vault/other`),
};

export const vaultService = {
  client(category: VaultCategoryKey): VaultRecordClient {
    return clients[category];
  },

  /** Relative URL of a vault-record attachment (used to fetch the file blob). */
  vaultAttachmentUrl(familyId: string, attachmentId: string): string {
    return `/families/${familyId}/vault/attachments/${attachmentId}`;
  },

  /** Relative URL of a document's single attachment. */
  documentAttachmentUrl(familyId: string, documentId: string): string {
    return `/families/${familyId}/documents/${documentId}/attachment`;
  },

  async deleteVaultAttachment(familyId: string, attachmentId: string): Promise<void> {
    await apiClient.delete(`/families/${familyId}/vault/attachments/${attachmentId}`);
  },

  /** Fetches a protected file as a blob (the JWT is attached by the client interceptor). */
  async fetchBlob(url: string): Promise<Blob> {
    const { data } = await apiClient.get<Blob>(url, { responseType: 'blob' });
    return data;
  },
};
