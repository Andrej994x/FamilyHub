import { describe, expect, it, vi } from 'vitest';
import {
  clearPendingInvitationToken,
  getPendingInvitationToken,
  postAuthRedirectPath,
  setPendingInvitationToken,
} from './pendingInvitation';

describe('pending invitation', () => {
  it('sends a user to the dashboard when no invitation is pending', () => {
    expect(postAuthRedirectPath()).toBe('/dashboard');
  });

  it('returns to the acceptance page with the URL-encoded token', () => {
    setPendingInvitationToken('abc 123&x');

    expect(getPendingInvitationToken()).toBe('abc 123&x');
    expect(postAuthRedirectPath()).toBe('/invitations/accept?token=abc%20123%26x');
  });

  it('forgets the token once cleared', () => {
    setPendingInvitationToken('abc');
    clearPendingInvitationToken();

    expect(postAuthRedirectPath()).toBe('/dashboard');
  });

  it('degrades gracefully when session storage is unavailable', () => {
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(() => {
      throw new Error('SecurityError');
    });
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(() => {
      throw new Error('SecurityError');
    });

    expect(() => setPendingInvitationToken('abc')).not.toThrow();
    expect(getPendingInvitationToken()).toBeNull();
    expect(postAuthRedirectPath()).toBe('/dashboard');
  });
});
