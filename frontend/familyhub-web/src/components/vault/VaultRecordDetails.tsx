import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal } from '../Modal';
import { AttachmentPreview, type UiAttachment } from './AttachmentPreview';
import { ExpiryChips } from './ExpiryChips';
import { formatFieldValue, inferContentType, fileNameFromPath, recordTitle, subjectName } from './vaultDisplay';
import { vaultService } from '../../services/vaultService';
import { getApiErrorMessage } from '../../utils/apiError';
import type { ChildResponse, FamilyMemberResponse, VaultRecord } from '../../types';
import type { CategoryConfig } from './vaultConfig';

interface VaultRecordDetailsProps {
  open: boolean;
  config: CategoryConfig;
  record: VaultRecord | null;
  familyId: string;
  members: FamilyMemberResponse[];
  childProfiles: ChildResponse[];
  canManage: boolean;
  onEdit: (record: VaultRecord) => void;
  onDelete: (record: VaultRecord) => void;
  onClose: () => void;
  onAttachmentsChanged: () => void;
}

function buildAttachments(
  config: CategoryConfig,
  record: VaultRecord,
  familyId: string,
  canManage: boolean,
): UiAttachment[] {
  if (config.singleAttachment) {
    if (!record.hasAttachment) return [];
    return [
      {
        key: record.id,
        fileName: fileNameFromPath(record.attachmentPath),
        contentType: inferContentType(record.attachmentPath),
        url: vaultService.documentAttachmentUrl(familyId, record.id),
        deletable: false,
      },
    ];
  }
  return (record.attachments ?? []).map((a) => ({
    key: a.id,
    fileName: a.fileName,
    contentType: a.contentType,
    url: vaultService.vaultAttachmentUrl(familyId, a.id),
    deletable: canManage,
  }));
}

export function VaultRecordDetails({
  open,
  config,
  record,
  familyId,
  members,
  childProfiles,
  canManage,
  onEdit,
  onDelete,
  onClose,
  onAttachmentsChanged,
}: VaultRecordDetailsProps) {
  const { t, i18n } = useTranslation();
  const [attachments, setAttachments] = useState<UiAttachment[]>([]);
  const [removingKey, setRemovingKey] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);

  const initialAttachments = useMemo(
    () => (record ? buildAttachments(config, record, familyId, canManage) : []),
    [config, record, familyId, canManage],
  );

  useEffect(() => {
    setAttachments(initialAttachments);
    setError(null);
  }, [initialAttachments]);

  if (!record) {
    return null;
  }

  const subject = config.key === 'documents' ? subjectName(record, members, childProfiles, t) : null;

  const rows = config.fields
    .filter((field) => field.type !== 'subject')
    .map((field) => ({ field, value: formatFieldValue(field, record, t, i18n.language) }))
    .filter((row) => row.value !== '');

  const handleDeleteAttachment = async (attachment: UiAttachment) => {
    if (!window.confirm(t('vault.attachments.deleteConfirm'))) {
      return;
    }
    setRemovingKey(attachment.key);
    setError(null);
    try {
      await vaultService.deleteVaultAttachment(familyId, attachment.key);
      setAttachments((prev) => prev.filter((a) => a.key !== attachment.key));
      onAttachmentsChanged();
    } catch (err) {
      setError(getApiErrorMessage(err, t('vault.errors.deleteFailed')));
    } finally {
      setRemovingKey(null);
    }
  };

  return (
    <Modal open={open} onClose={onClose} title={recordTitle(config, record, t)}>
      <div className="space-y-4">
        <ExpiryChips config={config} record={record} />

        <dl className="divide-y divide-gray-100 overflow-hidden rounded-xl border border-gray-200">
          {subject && (
            <div className="flex justify-between gap-4 px-3 py-2">
              <dt className="text-sm text-gray-500">{t('vault.fields.belongsTo')}</dt>
              <dd className="text-sm font-medium text-gray-900">{subject}</dd>
            </div>
          )}
          {rows.map(({ field, value }) => (
            <div key={field.name} className="flex justify-between gap-4 px-3 py-2">
              <dt className="text-sm text-gray-500">{t(field.labelKey)}</dt>
              <dd className="max-w-[60%] text-right text-sm font-medium text-gray-900">{value}</dd>
            </div>
          ))}
        </dl>

        <div>
          <h3 className="mb-2 text-sm font-semibold text-gray-900">
            {config.singleAttachment ? t('vault.fields.attachment') : t('vault.fields.attachments')}
          </h3>
          {attachments.length === 0 ? (
            <p className="rounded-lg border border-dashed border-gray-200 px-3 py-4 text-center text-sm text-gray-400">
              {t('vault.attachments.none')}
            </p>
          ) : (
            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
              {attachments.map((attachment) => (
                <AttachmentPreview
                  key={attachment.key}
                  attachment={attachment}
                  onDelete={handleDeleteAttachment}
                  deleting={removingKey === attachment.key}
                />
              ))}
            </div>
          )}
        </div>

        {error && <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>}

        {canManage && (
          <div className="flex gap-3">
            <button
              type="button"
              onClick={() => onDelete(record)}
              className="flex-1 rounded-lg border border-red-200 px-4 py-2.5 text-sm font-medium text-red-600 hover:bg-red-50"
            >
              {t('common.delete')}
            </button>
            <button
              type="button"
              onClick={() => onEdit(record)}
              className="flex-1 rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700"
            >
              {t('common.edit')}
            </button>
          </div>
        )}
      </div>
    </Modal>
  );
}
