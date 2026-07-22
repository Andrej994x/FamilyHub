import { useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../hooks/useAuth';
import { useFamily } from '../hooks/useFamily';
import { pickupService } from '../services/pickupService';
import { familyService } from '../services/familyService';
import { childService } from '../services/childService';
import { getApiErrorMessage } from '../utils/apiError';
import { dayKey, formatDayLong, formatTime } from '../utils/date';
import { pickupStatusClasses, pickupStatusKey } from '../utils/labels';
import { canManageFamily } from '../utils/roles';
import { PickupDialog } from '../components/PickupDialog';
import { PickupStatus } from '../types';
import type { ChildResponse, FamilyMemberResponse, PickupResponse } from '../types';

export default function Pickups() {
  const { t, i18n } = useTranslation();
  const { user } = useAuth();
  const { family, role } = useFamily();
  const familyId = family?.id ?? '';
  const canManage = canManageFamily(role);

  const [pickups, setPickups] = useState<PickupResponse[]>([]);
  const [members, setMembers] = useState<FamilyMemberResponse[]>([]);
  const [children, setChildren] = useState<ChildResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingPickup, setEditingPickup] = useState<PickupResponse | null>(null);

  const load = useCallback(async () => {
    if (!familyId) {
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const [pickupList, memberList, childList] = await Promise.all([
        pickupService.list(familyId),
        familyService.getMembers(familyId),
        childService.list(familyId),
      ]);
      setPickups(pickupList);
      setMembers(memberList);
      setChildren(childList);
    } catch (err) {
      setError(getApiErrorMessage(err, t('pickups.errors.loadFailed')));
    } finally {
      setLoading(false);
    }
  }, [familyId, t]);

  useEffect(() => {
    void load();
  }, [load]);

  // The current user's member id — needed to decide who may confirm/complete a pickup.
  const myMemberId = useMemo(
    () => members.find((m) => m.userId === user?.id)?.id ?? null,
    [members, user?.id],
  );

  const childName = useCallback(
    (id: string): string => {
      const c = children.find((child) => child.id === id);
      return c ? `${c.firstName} ${c.lastName}` : t('pickups.unknownChild');
    },
    [children, t],
  );

  const memberName = useCallback(
    (id: string): string => {
      const m = members.find((member) => member.id === id);
      return m ? `${m.firstName} ${m.lastName}` : t('pickups.unknownMember');
    },
    [members, t],
  );

  // Pickups grouped by calendar day, groups and pickups both ascending by time.
  const groups = useMemo(() => {
    const map = new Map<string, PickupResponse[]>();
    for (const pickup of pickups) {
      const key = dayKey(pickup.pickupDateTime);
      const list = map.get(key) ?? [];
      list.push(pickup);
      map.set(key, list);
    }
    return [...map.entries()]
      .map(([key, items]) => ({
        key,
        items: items.sort((a, b) => a.pickupDateTime.localeCompare(b.pickupDateTime)),
      }))
      .sort((a, b) => a.key.localeCompare(b.key));
  }, [pickups]);

  const openCreate = () => {
    setEditingPickup(null);
    setDialogOpen(true);
  };

  const openEdit = (pickup: PickupResponse) => {
    setEditingPickup(pickup);
    setDialogOpen(true);
  };

  const changeStatus = async (pickup: PickupResponse, status: number) => {
    try {
      await pickupService.updateStatus(familyId, pickup.id, status);
      await load();
    } catch (err) {
      setError(getApiErrorMessage(err, t('pickups.errors.statusFailed')));
    }
  };

  const takeOver = async (pickup: PickupResponse) => {
    try {
      await pickupService.takeOver(familyId, pickup.id);
      await load();
    } catch (err) {
      setError(getApiErrorMessage(err, t('pickups.errors.takeOverFailed')));
    }
  };

  const handleDelete = async (pickup: PickupResponse) => {
    if (!window.confirm(t('pickups.deleteConfirm'))) {
      return;
    }
    try {
      await pickupService.remove(familyId, pickup.id);
      await load();
    } catch (err) {
      setError(getApiErrorMessage(err, t('pickups.errors.deleteFailed')));
    }
  };

  if (!family) {
    return null;
  }

  return (
    <div className="mx-auto max-w-3xl">
      <header className="mb-6 flex items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">{t('nav.pickups')}</h1>
          <p className="mt-1 text-sm text-gray-500">{t('pages.pickups.subtitle')}</p>
        </div>
        {canManage && (
          <button
            type="button"
            onClick={openCreate}
            className="shrink-0 rounded-lg bg-brand-600 px-3 py-2 text-sm font-medium text-white hover:bg-brand-700"
          >
            {t('pickups.add')}
          </button>
        )}
      </header>

      {error && (
        <div className="mb-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>
      )}

      {loading ? (
        <p className="text-sm text-gray-400">{t('common.loading')}</p>
      ) : groups.length === 0 ? (
        <div className="rounded-xl border-2 border-dashed border-gray-200 bg-white p-10 text-center text-sm text-gray-400">
          {t('pickups.empty')}
        </div>
      ) : (
        <div className="space-y-6">
          {groups.map((group) => (
            <section key={group.key}>
              <h2 className="mb-2 text-sm font-semibold text-gray-900">
                {formatDayLong(group.items[0].pickupDateTime, i18n.language)}
              </h2>
              <div className="space-y-3">
                {group.items.map((pickup) => {
                  const isAssignee = myMemberId !== null && pickup.assignedMemberId === myMemberId;
                  return (
                    <div
                      key={pickup.id}
                      className="rounded-xl border border-gray-200 bg-white p-4"
                    >
                      <div className="flex items-start justify-between gap-3">
                        <div className="min-w-0">
                          <div className="flex flex-wrap items-center gap-2">
                            <p className="font-medium text-gray-900">
                              {childName(pickup.childProfileId)}
                            </p>
                            <span
                              className={`rounded-full px-2 py-0.5 text-xs font-medium ${pickupStatusClasses(pickup.status)}`}
                            >
                              {t(pickupStatusKey(pickup.status))}
                            </span>
                          </div>
                          <div className="mt-1 space-y-0.5 text-sm text-gray-500">
                            <p>
                              {formatTime(pickup.pickupDateTime, i18n.language)} · {pickup.location}
                            </p>
                            <p className="text-xs text-gray-400">
                              {t('pickups.assignedTo', {
                                name: memberName(pickup.assignedMemberId),
                              })}
                            </p>
                            {pickup.notes && (
                              <p className="text-xs text-gray-400">{pickup.notes}</p>
                            )}
                          </div>
                        </div>
                      </div>

                      {/* Status actions — availability depends on role, assignee and status. */}
                      <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-2 border-t border-gray-100 pt-3">
                        {isAssignee && pickup.status === PickupStatus.Pending && (
                          <button
                            type="button"
                            onClick={() => changeStatus(pickup, PickupStatus.Confirmed)}
                            className="text-xs font-semibold text-green-700 hover:underline"
                          >
                            {t('pickups.actions.confirm')}
                          </button>
                        )}
                        {isAssignee && pickup.status === PickupStatus.Confirmed && (
                          <button
                            type="button"
                            onClick={() => changeStatus(pickup, PickupStatus.Completed)}
                            className="text-xs font-semibold text-green-700 hover:underline"
                          >
                            {t('pickups.actions.complete')}
                          </button>
                        )}
                        {isAssignee &&
                          (pickup.status === PickupStatus.Pending ||
                            pickup.status === PickupStatus.Confirmed) && (
                            <button
                              type="button"
                              onClick={() => changeStatus(pickup, PickupStatus.CannotAttend)}
                              className="text-xs font-medium text-red-600 hover:underline"
                            >
                              {t('pickups.actions.cannotAttend')}
                            </button>
                          )}
                        {canManage &&
                          !isAssignee &&
                          pickup.status === PickupStatus.CannotAttend && (
                            <button
                              type="button"
                              onClick={() => takeOver(pickup)}
                              className="text-xs font-semibold text-brand-600 hover:underline"
                            >
                              {t('pickups.actions.takeOver')}
                            </button>
                          )}

                        {/* Manage actions pushed to the right. */}
                        {canManage && (
                          <div className="ml-auto flex items-center gap-4">
                            <button
                              type="button"
                              onClick={() => openEdit(pickup)}
                              className="text-xs font-medium text-brand-600 hover:underline"
                            >
                              {t('common.edit')}
                            </button>
                            <button
                              type="button"
                              onClick={() => handleDelete(pickup)}
                              className="text-xs font-medium text-red-600 hover:underline"
                            >
                              {t('common.delete')}
                            </button>
                          </div>
                        )}
                      </div>
                    </div>
                  );
                })}
              </div>
            </section>
          ))}
        </div>
      )}

      <PickupDialog
        open={dialogOpen}
        familyId={familyId}
        pickup={editingPickup}
        members={members}
        childProfiles={children}
        onClose={() => setDialogOpen(false)}
        onSaved={load}
      />
    </div>
  );
}
