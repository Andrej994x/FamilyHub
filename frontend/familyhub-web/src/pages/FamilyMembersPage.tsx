import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../hooks/useAuth';
import { useFamily } from '../hooks/useFamily';
import { familyService } from '../services/familyService';
import { invitationService } from '../services/invitationService';
import { getApiErrorMessage } from '../utils/apiError';
import { formatDate } from '../utils/format';
import { canManageFamily, invitationStatusKey, isOwner, roleLabelKey } from '../utils/roles';
import { InviteMemberDialog } from '../components/InviteMemberDialog';
import { ChildProfilesSection } from '../components/ChildProfilesSection';
import { FamilyRole, InvitationStatus } from '../types';
import type { FamilyMemberResponse, InvitationResponse } from '../types';

export default function FamilyMembersPage() {
  const { t, i18n } = useTranslation();
  const { user } = useAuth();
  const { family, role } = useFamily();

  const [members, setMembers] = useState<FamilyMemberResponse[]>([]);
  const [invitations, setInvitations] = useState<InvitationResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [inviteOpen, setInviteOpen] = useState(false);

  const canManage = canManageFamily(role);
  const owner = isOwner(role);
  const familyId = family?.id ?? '';

  const loadMembers = useCallback(async () => {
    if (!familyId) {
      return;
    }
    setMembers(await familyService.getMembers(familyId));
  }, [familyId]);

  const loadInvitations = useCallback(async () => {
    if (!familyId || !canManage) {
      setInvitations([]);
      return;
    }
    setInvitations(await invitationService.list(familyId));
  }, [familyId, canManage]);

  const loadAll = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      await Promise.all([loadMembers(), loadInvitations()]);
    } catch (err) {
      setError(getApiErrorMessage(err, t('family.loadError')));
    } finally {
      setLoading(false);
    }
  }, [loadMembers, loadInvitations, t]);

  useEffect(() => {
    void loadAll();
  }, [loadAll]);

  const handleRemoveMember = async (member: FamilyMemberResponse) => {
    if (!window.confirm(t('family.members.removeConfirm'))) {
      return;
    }
    try {
      await familyService.removeMember(familyId, member.id);
      await loadMembers();
    } catch (err) {
      setError(getApiErrorMessage(err, t('family.loadError')));
    }
  };

  const handleCancelInvitation = async (invitation: InvitationResponse) => {
    if (!window.confirm(t('invitations.cancelConfirm'))) {
      return;
    }
    try {
      await invitationService.cancel(familyId, invitation.id);
      await loadInvitations();
    } catch (err) {
      setError(getApiErrorMessage(err, t('invitations.errors.createFailed')));
    }
  };

  if (!family) {
    return null;
  }

  return (
    <div className="mx-auto max-w-4xl">
      <header className="mb-6">
        <h1 className="text-2xl font-semibold text-gray-900">{family.name}</h1>
        <p className="mt-1 text-sm text-gray-500">
          {t('family.memberCount', { count: family.memberCount })} ·{' '}
          {t('family.createdOn', { date: formatDate(family.createdAt, i18n.language) })}
        </p>
      </header>

      {error && (
        <div className="mb-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>
      )}

      {/* Members */}
      <section>
        <h2 className="mb-3 text-lg font-semibold text-gray-900">{t('family.members.title')}</h2>
        {loading ? (
          <p className="text-sm text-gray-400">{t('common.loading')}</p>
        ) : (
          <div className="divide-y divide-gray-100 overflow-hidden rounded-xl border border-gray-200 bg-white">
            {members.map((member) => {
              const isSelf = member.userId === user?.id;
              const canRemove = owner && member.role !== FamilyRole.Owner;
              return (
                <div key={member.id} className="flex items-center justify-between gap-3 p-4">
                  <div className="min-w-0">
                    <p className="truncate font-medium text-gray-900">
                      {member.firstName} {member.lastName}
                      {isSelf && (
                        <span className="ml-2 text-xs font-normal text-gray-400">
                          ({t('common.you')})
                        </span>
                      )}
                    </p>
                    <p className="truncate text-xs text-gray-500">{member.email}</p>
                  </div>
                  <div className="flex shrink-0 items-center gap-3">
                    <span className="rounded-full bg-brand-50 px-2.5 py-1 text-xs font-medium text-brand-700">
                      {t(roleLabelKey(member.role))}
                    </span>
                    {canRemove && (
                      <button
                        type="button"
                        onClick={() => handleRemoveMember(member)}
                        className="text-xs font-medium text-red-600 hover:underline"
                      >
                        {t('common.remove')}
                      </button>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </section>

      {/* Invitations (managers only) */}
      {canManage && (
        <section className="mt-8">
          <div className="mb-3 flex items-center justify-between">
            <h2 className="text-lg font-semibold text-gray-900">{t('invitations.title')}</h2>
            <button
              type="button"
              onClick={() => setInviteOpen(true)}
              className="shrink-0 rounded-lg bg-brand-600 px-3 py-2 text-sm font-medium text-white hover:bg-brand-700"
            >
              {t('invitations.invite')}
            </button>
          </div>

          {invitations.length === 0 ? (
            <div className="rounded-xl border-2 border-dashed border-gray-200 bg-white p-6 text-center text-sm text-gray-400">
              {t('invitations.empty')}
            </div>
          ) : (
            <div className="divide-y divide-gray-100 overflow-hidden rounded-xl border border-gray-200 bg-white">
              {invitations.map((invitation) => (
                <div key={invitation.id} className="flex items-center justify-between gap-3 p-4">
                  <div className="min-w-0">
                    <p className="truncate font-medium text-gray-900">{invitation.email}</p>
                    <p className="text-xs text-gray-500">
                      {t(roleLabelKey(invitation.role))} ·{' '}
                      {t('invitations.expires', {
                        date: formatDate(invitation.expiresAt, i18n.language),
                      })}
                    </p>
                  </div>
                  <div className="flex shrink-0 items-center gap-3">
                    <span className="rounded-full bg-gray-100 px-2.5 py-1 text-xs font-medium text-gray-600">
                      {t(invitationStatusKey(invitation.status))}
                    </span>
                    {invitation.status === InvitationStatus.Pending && (
                      <button
                        type="button"
                        onClick={() => handleCancelInvitation(invitation)}
                        className="text-xs font-medium text-red-600 hover:underline"
                      >
                        {t('common.cancel')}
                      </button>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      )}

      {/* Child profiles */}
      <ChildProfilesSection familyId={familyId} canManage={canManage} />

      <InviteMemberDialog
        open={inviteOpen}
        familyId={familyId}
        onClose={() => setInviteOpen(false)}
        onInvited={loadInvitations}
      />
    </div>
  );
}
