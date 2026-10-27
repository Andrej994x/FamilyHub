import { describe, expect, it } from 'vitest';
import { FamilyRole } from '../types';
import { canManageFamily, invitationStatusKey, isOwner, roleLabelKey } from './roles';

describe('role helpers', () => {
  it('lets only owners and parents manage the family', () => {
    expect(canManageFamily(FamilyRole.Owner)).toBe(true);
    expect(canManageFamily(FamilyRole.Parent)).toBe(true);
    expect(canManageFamily(FamilyRole.Member)).toBe(false);
    expect(canManageFamily(FamilyRole.Child)).toBe(false);
    expect(canManageFamily(null)).toBe(false);
  });

  it('identifies only the owner as owner', () => {
    expect(isOwner(FamilyRole.Owner)).toBe(true);
    expect(isOwner(FamilyRole.Parent)).toBe(false);
    expect(isOwner(null)).toBe(false);
  });

  it('keeps role values in sync with the backend FamilyRole enum order', () => {
    expect(FamilyRole).toEqual({ Owner: 0, Parent: 1, Member: 2, Child: 3 });
  });

  it('maps roles and invitation statuses to translation keys', () => {
    expect(roleLabelKey(FamilyRole.Owner)).toBe('roles.owner');
    expect(roleLabelKey(FamilyRole.Child)).toBe('roles.child');
    expect(roleLabelKey(99)).toBe('roles.member');
    expect(invitationStatusKey(0)).toBe('invitations.status.pending');
    expect(invitationStatusKey(3)).toBe('invitations.status.cancelled');
  });
});
