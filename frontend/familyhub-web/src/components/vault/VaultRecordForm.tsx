import { useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Modal } from '../Modal';
import { vaultService } from '../../services/vaultService';
import { getApiErrorMessage } from '../../utils/apiError';
import { documentTypeKey } from '../../utils/vaultStatus';
import type { ChildResponse, FamilyMemberResponse, VaultRecord } from '../../types';
import type { CategoryConfig, FieldDef } from './vaultConfig';

interface VaultRecordFormProps {
  open: boolean;
  config: CategoryConfig;
  record: VaultRecord | null;
  familyId: string;
  members: FamilyMemberResponse[];
  childProfiles: ChildResponse[];
  onClose: () => void;
  onSaved: () => void;
}

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100';

const DOCUMENT_TYPE_VALUES = [0, 1, 2, 3, 4, 5];

function initialValue(field: FieldDef, record: VaultRecord | null): string {
  if (!record) {
    return field.type === 'documentType' ? '0' : '';
  }
  if (field.type === 'subject') {
    if (record.familyMemberId) return `member:${record.familyMemberId}`;
    if (record.childProfileId) return `child:${record.childProfileId}`;
    return '';
  }
  const raw = record[field.name];
  if (raw === null || raw === undefined) {
    return field.type === 'documentType' ? '0' : '';
  }
  if (field.type === 'date' || field.type === 'dateonly') {
    return String(raw).slice(0, 10);
  }
  return String(raw);
}

export function VaultRecordForm({
  open,
  config,
  record,
  familyId,
  members,
  childProfiles,
  onClose,
  onSaved,
}: VaultRecordFormProps) {
  const { t } = useTranslation();
  const [values, setValues] = useState<Record<string, string>>({});
  const [files, setFiles] = useState<File[]>([]);
  const [removeAttachment, setRemoveAttachment] = useState(false);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (open) {
      const next: Record<string, string> = {};
      for (const field of config.fields) {
        next[field.name] = initialValue(field, record);
      }
      setValues(next);
      setFiles([]);
      setRemoveAttachment(false);
      setError(null);
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  }, [open, config, record]);

  const setValue = (name: string, value: string) =>
    setValues((prev) => ({ ...prev, [name]: value }));

  const handleFiles = (event: React.ChangeEvent<HTMLInputElement>) => {
    setFiles(event.target.files ? Array.from(event.target.files) : []);
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();

    for (const field of config.fields) {
      if (field.required && !values[field.name]?.trim()) {
        setError(t('vault.errors.requiredMissing'));
        return;
      }
    }

    const formData = new FormData();
    for (const field of config.fields) {
      const value = values[field.name] ?? '';
      if (field.type === 'subject') {
        if (value.startsWith('member:')) formData.append('familyMemberId', value.slice(7));
        else if (value.startsWith('child:')) formData.append('childProfileId', value.slice(6));
        continue;
      }
      if (value.trim() === '') {
        continue;
      }
      if (field.type === 'date') {
        formData.append(field.name, new Date(value).toISOString());
      } else if (field.type === 'dateonly') {
        formData.append(field.name, value);
      } else {
        formData.append(field.name, value.trim());
      }
    }

    if (config.singleAttachment) {
      if (files[0]) formData.append('attachment', files[0]);
      if (removeAttachment) formData.append('removeAttachment', 'true');
    } else {
      for (const file of files) {
        formData.append('attachments', file);
      }
    }

    setSubmitting(true);
    setError(null);
    try {
      const client = vaultService.client(config.key);
      if (record) {
        await client.update(familyId, record.id, formData);
      } else {
        await client.create(familyId, formData);
      }
      onSaved();
      onClose();
    } catch (err) {
      setError(getApiErrorMessage(err, t('vault.errors.saveFailed')));
    } finally {
      setSubmitting(false);
    }
  };

  const title = record
    ? t('vault.form.editTitle', { name: t(config.labelKey) })
    : t('vault.form.addTitle', { name: t(config.labelKey) });

  const showRemoveAttachment = config.singleAttachment && record?.hasAttachment && files.length === 0;

  return (
    <Modal open={open} onClose={onClose} title={title}>
      <form onSubmit={handleSubmit} className="space-y-4">
        {config.fields.map((field) => (
          <div key={field.name}>
            <label className="mb-1 block text-sm font-medium text-gray-700">
              {t(field.labelKey)}
              {field.required && <span className="ml-0.5 text-red-500">*</span>}
            </label>
            {renderField(field, values[field.name] ?? '', setValue, { t, members, children: childProfiles })}
          </div>
        ))}

        <div>
          <label className="mb-1 block text-sm font-medium text-gray-700">
            {config.singleAttachment ? t('vault.fields.attachment') : t('vault.fields.attachments')}
          </label>
          <input
            ref={fileInputRef}
            type="file"
            accept="image/png,image/jpeg,application/pdf"
            multiple={!config.singleAttachment}
            onChange={handleFiles}
            className="block w-full text-sm text-gray-600 file:mr-3 file:rounded-lg file:border-0 file:bg-brand-50 file:px-3 file:py-2 file:text-sm file:font-medium file:text-brand-700 hover:file:bg-brand-100"
          />
          <p className="mt-1 text-xs text-gray-400">{t('vault.attachments.uploadHint')}</p>
          {showRemoveAttachment && (
            <label className="mt-2 flex items-center gap-2 text-xs text-gray-600">
              <input
                type="checkbox"
                checked={removeAttachment}
                onChange={(e) => setRemoveAttachment(e.target.checked)}
              />
              {t('vault.attachments.removeCurrent')}
            </label>
          )}
        </div>

        {error && <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>}

        <div className="flex gap-3">
          <button
            type="button"
            onClick={onClose}
            className="flex-1 rounded-lg border border-gray-300 px-4 py-2.5 text-sm font-medium text-gray-700 hover:bg-gray-50"
          >
            {t('common.cancel')}
          </button>
          <button
            type="submit"
            disabled={submitting}
            className="flex-1 rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-60"
          >
            {submitting ? t('common.saving') : t('vault.actions.save')}
          </button>
        </div>
      </form>
    </Modal>
  );
}

interface RenderContext {
  t: (key: string) => string;
  members: FamilyMemberResponse[];
  children: ChildResponse[];
}

function renderField(
  field: FieldDef,
  value: string,
  setValue: (name: string, value: string) => void,
  ctx: RenderContext,
) {
  const onChange = (v: string) => setValue(field.name, v);

  switch (field.type) {
    case 'textarea':
      return (
        <textarea
          rows={2}
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className={inputClass}
        />
      );
    case 'number':
      return (
        <input
          type="number"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className={inputClass}
        />
      );
    case 'date':
    case 'dateonly':
      return (
        <input
          type="date"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className={inputClass}
        />
      );
    case 'documentType':
      return (
        <select value={value} onChange={(e) => onChange(e.target.value)} className={inputClass}>
          {DOCUMENT_TYPE_VALUES.map((v) => (
            <option key={v} value={v}>
              {ctx.t(documentTypeKey(v))}
            </option>
          ))}
        </select>
      );
    case 'subject':
      return (
        <select value={value} onChange={(e) => onChange(e.target.value)} className={inputClass}>
          <option value="">{ctx.t('vault.fields.selectSubject')}</option>
          {ctx.members.length > 0 && (
            <optgroup label={ctx.t('vault.fields.members')}>
              {ctx.members.map((m) => (
                <option key={m.id} value={`member:${m.id}`}>
                  {m.firstName} {m.lastName}
                </option>
              ))}
            </optgroup>
          )}
          {ctx.children.length > 0 && (
            <optgroup label={ctx.t('vault.fields.children')}>
              {ctx.children.map((c) => (
                <option key={c.id} value={`child:${c.id}`}>
                  {c.firstName} {c.lastName}
                </option>
              ))}
            </optgroup>
          )}
        </select>
      );
    default:
      return (
        <input
          type="text"
          value={value}
          onChange={(e) => onChange(e.target.value)}
          className={inputClass}
        />
      );
  }
}
