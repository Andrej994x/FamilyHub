export const PickupStatus = {
  Pending: 0,
  Confirmed: 1,
  CannotAttend: 2,
  Completed: 3,
} as const;

export interface PickupResponse {
  id: string;
  familyId: string;
  childProfileId: string;
  assignedMemberId: string;
  pickupDateTime: string;
  location: string;
  notes: string | null;
  status: number;
  createdByUserId: string;
  createdAt: string;
  completedAt: string | null;
}
