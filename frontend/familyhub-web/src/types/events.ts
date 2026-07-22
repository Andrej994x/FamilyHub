export const EventType = {
  Kindergarten: 0,
  School: 1,
  Doctor: 2,
  Training: 3,
  Birthday: 4,
  Family: 5,
  Other: 6,
} as const;

export interface EventResponse {
  id: string;
  familyId: string;
  title: string;
  description: string | null;
  eventType: number;
  startDateTime: string;
  endDateTime: string | null;
  location: string | null;
  assignedMemberId: string | null;
  childProfileId: string | null;
  createdByUserId: string;
  createdAt: string;
}

export interface EventFilter {
  dateFrom?: string;
  dateTo?: string;
  memberId?: string;
  childId?: string;
  eventType?: number;
}
