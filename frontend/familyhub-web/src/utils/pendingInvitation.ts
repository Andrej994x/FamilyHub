// Survives a login/registration round-trip so an invited user who wasn't signed in
// is returned to the acceptance page afterward to finish joining.
const KEY = 'familyhub_pending_invitation';

export function setPendingInvitationToken(token: string): void {
  try {
    sessionStorage.setItem(KEY, token);
  } catch {
    // Storage may be unavailable (private mode); the flow still works via the URL token.
  }
}

export function getPendingInvitationToken(): string | null {
  try {
    return sessionStorage.getItem(KEY);
  } catch {
    return null;
  }
}

export function clearPendingInvitationToken(): void {
  try {
    sessionStorage.removeItem(KEY);
  } catch {
    // Ignore.
  }
}

/** Where to send a user right after they authenticate. */
export function postAuthRedirectPath(): string {
  const token = getPendingInvitationToken();
  return token ? `/invitations/accept?token=${encodeURIComponent(token)}` : '/dashboard';
}
