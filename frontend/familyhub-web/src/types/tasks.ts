export const TaskPriority = {
  Low: 0,
  Medium: 1,
  High: 2,
} as const;

export const TaskStatus = {
  Pending: 0,
  InProgress: 1,
  Completed: 2,
  Cancelled: 3,
} as const;

export interface TaskResponse {
  id: string;
  familyId: string;
  title: string;
  description: string | null;
  assignedToMemberId: string | null;
  dueDate: string | null;
  priority: number;
  status: number;
  createdByUserId: string;
  createdAt: string;
  completedAt: string | null;
}

export interface TaskFilter {
  status?: number;
  assignedMemberId?: string;
  dueFrom?: string;
  dueTo?: string;
  priority?: number;
}
