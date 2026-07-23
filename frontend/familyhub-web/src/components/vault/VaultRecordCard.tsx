import { useTranslation } from 'react-i18next';
import { ExpiryChips } from './ExpiryChips';
import { attachmentCount, recordTitle, subjectName } from './vaultDisplay';
import type { ChildResponse, FamilyMemberResponse, VaultRecord } from '../../types';
import type { CategoryConfig } from './vaultConfig';

interface VaultRecordCardProps {
  config: CategoryConfig;
  record: VaultRecord;
  members: FamilyMemberResponse[];
  childProfiles: ChildResponse[];
  onOpen: (record: VaultRecord) => void;
}

export function VaultRecordCard({ config, record, members, childProfiles, onOpen }: VaultRecordCardProps) {
  const { t } = useTranslation();

  // The subtitle differs for documents (the subject) vs. other records (key fields).
  const subtitle =
    config.key === 'documents'
      ? subjectName(record, members, childProfiles, t)
      : config.subtitleFields
          .map((name) => record[name])
          .filter((v): v is string => typeof v === 'string' && v.trim() !== '')
          .join(' · ');

  const count = attachmentCount(record);

  return (
    <button
      type="button"
      onClick={() => onOpen(record)}
      className="w-full rounded-xl border border-gray-200 bg-white p-4 text-left transition hover:border-brand-300 hover:bg-brand-50/30"
    >
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate font-medium text-gray-900">{recordTitle(config, record, t)}</p>
          {subtitle && <p className="mt-0.5 truncate text-sm text-gray-500">{subtitle}</p>}
        </div>
        {count > 0 && (
          <span className="flex shrink-0 items-center gap-1 text-xs text-gray-400">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" strokeWidth={1.7} stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" d="M18.375 12.739l-7.693 7.693a4.5 4.5 0 01-6.364-6.364l10.94-10.94A3 3 0 1113.5 7.372L6.62 14.25" />
            </svg>
            {count}
          </span>
        )}
      </div>
      <div className="mt-2">
        <ExpiryChips config={config} record={record} />
      </div>
    </button>
  );
}
