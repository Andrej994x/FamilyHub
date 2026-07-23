import { useCallback, useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { VaultRecordCard } from './VaultRecordCard';
import { VaultRecordDetails } from './VaultRecordDetails';
import { VaultRecordForm } from './VaultRecordForm';
import { vaultService } from '../../services/vaultService';
import { getApiErrorMessage } from '../../utils/apiError';
import type { ChildResponse, FamilyMemberResponse, VaultRecord } from '../../types';
import type { CategoryConfig } from './vaultConfig';

interface VaultSectionProps {
  config: CategoryConfig;
  familyId: string;
  canManage: boolean;
  members: FamilyMemberResponse[];
  childProfiles: ChildResponse[];
}

export function VaultSection({ config, familyId, canManage, members, childProfiles }: VaultSectionProps) {
  const { t } = useTranslation();
  const [records, setRecords] = useState<VaultRecord[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const [detailsId, setDetailsId] = useState<string | null>(null);
  const [formOpen, setFormOpen] = useState(false);
  const [editingRecord, setEditingRecord] = useState<VaultRecord | null>(null);

  const load = useCallback(async () => {
    if (!familyId) {
      return;
    }
    setLoading(true);
    setError(null);
    try {
      setRecords(await vaultService.client(config.key).list(familyId));
    } catch (err) {
      setError(getApiErrorMessage(err, t('vault.errors.loadFailed')));
    } finally {
      setLoading(false);
    }
  }, [familyId, config.key, t]);

  useEffect(() => {
    void load();
  }, [load]);

  const detailsRecord = detailsId ? records.find((r) => r.id === detailsId) ?? null : null;

  const openCreate = () => {
    setEditingRecord(null);
    setFormOpen(true);
  };

  const handleEdit = (record: VaultRecord) => {
    setDetailsId(null);
    setEditingRecord(record);
    setFormOpen(true);
  };

  const handleDelete = async (record: VaultRecord) => {
    if (!window.confirm(t('vault.actions.deleteConfirm'))) {
      return;
    }
    try {
      await vaultService.client(config.key).remove(familyId, record.id);
      setDetailsId(null);
      await load();
    } catch (err) {
      setError(getApiErrorMessage(err, t('vault.errors.deleteFailed')));
    }
  };

  return (
    <div>
      <div className="mb-4 flex items-center justify-between gap-3">
        <h2 className="flex items-center gap-2 text-lg font-semibold text-gray-900">
          <span aria-hidden>{config.emoji}</span>
          {t(config.labelKey)}
        </h2>
        {canManage && (
          <button
            type="button"
            onClick={openCreate}
            className="shrink-0 rounded-lg bg-brand-600 px-3 py-2 text-sm font-medium text-white hover:bg-brand-700"
          >
            {t('vault.actions.add')}
          </button>
        )}
      </div>

      {error && (
        <div className="mb-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>
      )}

      {loading ? (
        <p className="text-sm text-gray-400">{t('common.loading')}</p>
      ) : records.length === 0 ? (
        <div className="rounded-xl border-2 border-dashed border-gray-200 bg-white p-10 text-center text-sm text-gray-400">
          {t('vault.empty', { name: t(config.labelKey) })}
        </div>
      ) : (
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
          {records.map((record) => (
            <VaultRecordCard
              key={record.id}
              config={config}
              record={record}
              members={members}
              childProfiles={childProfiles}
              onOpen={(r) => setDetailsId(r.id)}
            />
          ))}
        </div>
      )}

      <VaultRecordDetails
        open={detailsRecord !== null}
        config={config}
        record={detailsRecord}
        familyId={familyId}
        members={members}
        childProfiles={childProfiles}
        canManage={canManage}
        onEdit={handleEdit}
        onDelete={handleDelete}
        onClose={() => setDetailsId(null)}
        onAttachmentsChanged={load}
      />

      <VaultRecordForm
        open={formOpen}
        config={config}
        record={editingRecord}
        familyId={familyId}
        members={members}
        childProfiles={childProfiles}
        onClose={() => setFormOpen(false)}
        onSaved={load}
      />
    </div>
  );
}
