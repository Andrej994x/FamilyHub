export const DocumentType = {
  IdentityCard: 0,
  Passport: 1,
  DrivingLicense: 2,
  HealthCard: 3,
  ResidencePermit: 4,
  StudentDocument: 5,
} as const;

export interface VaultAttachment {
  id: string;
  fileName: string | null;
  contentType: string;
  sizeBytes: number;
  createdAt: string;
}

/**
 * A vault record of any category. The known fields common to every category are typed;
 * category-specific fields are read generically via the index signature using the
 * category field config, so a single set of components can render all six sections.
 */
export interface VaultRecord {
  id: string;
  familyId: string;
  createdByUserId: string;
  createdAt: string;
  // Vault records (vehicles, pets, home, warranties, other) carry many attachments.
  attachments?: VaultAttachment[];
  // Documents carry a single attachment described by these fields.
  hasAttachment?: boolean;
  attachmentPath?: string | null;
  // Documents subject.
  familyMemberId?: string | null;
  childProfileId?: string | null;
  documentType?: number;
  [key: string]: unknown;
}
