export const FamilyRole = {
  Owner: 0,
  Parent: 1,
  Member: 2,
  Child: 3,
} as const;

export const InvitationStatus = {
  Pending: 0,
  Accepted: 1,
  Expired: 2,
  Cancelled: 3,
} as const;

export interface FamilyResponse {
  id: string;
  name: string;
  createdByUserId: string;
  createdAt: string;
  currentUserRole: number;
  memberCount: number;
}

export interface FamilyMemberResponse {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  email: string;
  role: number;
  joinedAt: string;
}

export interface InvitationResponse {
  id: string;
  familyId: string;
  email: string;
  role: number;
  status: number;
  expiresAt: string;
  createdAt: string;
  lastSentAt: string | null;
}

export interface CreatedInvitationResponse {
  invitation: InvitationResponse;
  token: string;
  acceptUrl: string;
}

export interface ChildResponse {
  id: string;
  familyId: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string | null;
  notes: string | null;
  createdAt: string;
}

export interface CreateFamilyRequest {
  name: string;
}

export interface CreateInvitationRequest {
  email: string;
  role: number;
}

export interface ChildRequest {
  firstName: string;
  lastName: string;
  dateOfBirth: string | null;
  notes: string | null;
}
