import { FamilyRole } from '../types';

export function canManageFamily(role: number | null): boolean {
  return role === FamilyRole.Owner || role === FamilyRole.Parent;
}

export function isOwner(role: number | null): boolean {
  return role === FamilyRole.Owner;
}

/** Family Expenses is for every member except children (the API also returns 403 for them). */
export function canAccessExpenses(role: number | null): boolean {
  return role !== null && role !== FamilyRole.Child;
}

/** Owners and parents can change any expense; other members only the ones they created. */
export function canEditExpense(role: number | null, createdByUserId: string, userId: string | undefined): boolean {
  return canManageFamily(role) || (canAccessExpenses(role) && createdByUserId === userId);
}

export function roleLabelKey(role: number): string {
  switch (role) {
    case FamilyRole.Owner:
      return 'roles.owner';
    case FamilyRole.Parent:
      return 'roles.parent';
    case FamilyRole.Child:
      return 'roles.child';
    default:
      return 'roles.member';
  }
}

export function invitationStatusKey(status: number): string {
  switch (status) {
    case 1:
      return 'invitations.status.accepted';
    case 2:
      return 'invitations.status.expired';
    case 3:
      return 'invitations.status.cancelled';
    default:
      return 'invitations.status.pending';
  }
}
