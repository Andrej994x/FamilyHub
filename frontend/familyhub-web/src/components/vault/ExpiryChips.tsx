import { useTranslation } from 'react-i18next';
import { expiryChipClasses, getExpiryInfo, type ExpiryInfo } from '../../utils/vaultStatus';
import type { VaultRecord } from '../../types';
import type { CategoryConfig } from './vaultConfig';

function statusText(info: ExpiryInfo, t: (k: string, o?: Record<string, unknown>) => string): string {
  switch (info.status) {
    case 'expired':
      return t('vault.status.expired');
    case 'expiringSoon':
      return info.days === 0 ? t('vault.status.today') : t('vault.status.inDays', { days: info.days });
    case 'valid':
      return t('vault.status.valid');
    default:
      return '';
  }
}

interface ExpiryChipsProps {
  config: CategoryConfig;
  record: VaultRecord;
}

export function ExpiryChips({ config, record }: ExpiryChipsProps) {
  const { t } = useTranslation();

  const chips = config.expiryFields
    .map((field) => ({ field, info: getExpiryInfo(record[field.name]) }))
    .filter((entry) => entry.info.status !== 'none');

  if (chips.length === 0) {
    return null;
  }

  return (
    <div className="flex flex-wrap gap-1.5">
      {chips.map(({ field, info }) => (
        <span
          key={field.name}
          className={`rounded-full px-2 py-0.5 text-xs font-medium ${expiryChipClasses(info.status)}`}
        >
          {t(field.labelKey)}: {statusText(info, t)}
        </span>
      ))}
    </div>
  );
}
