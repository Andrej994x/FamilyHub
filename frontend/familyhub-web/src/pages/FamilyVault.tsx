import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useFamily } from '../hooks/useFamily';
import { familyService } from '../services/familyService';
import { childService } from '../services/childService';
import { canManageFamily } from '../utils/roles';
import { VaultSection } from '../components/vault/VaultSection';
import { VAULT_CATEGORIES } from '../components/vault/vaultConfig';
import type { ChildResponse, FamilyMemberResponse } from '../types';
import type { VaultCategoryKey } from '../services/vaultService';

export default function FamilyVault() {
  const { t } = useTranslation();
  const { family, role } = useFamily();
  const familyId = family?.id ?? '';
  const canManage = canManageFamily(role);

  const [activeKey, setActiveKey] = useState<VaultCategoryKey>('documents');
  const [members, setMembers] = useState<FamilyMemberResponse[]>([]);
  const [children, setChildren] = useState<ChildResponse[]>([]);

  // Members and children are needed for the Documents subject picker and labels.
  useEffect(() => {
    if (!familyId) {
      return;
    }
    familyService.getMembers(familyId).then(setMembers).catch(() => setMembers([]));
    childService.list(familyId).then(setChildren).catch(() => setChildren([]));
  }, [familyId]);

  const activeConfig = useMemo(
    () => VAULT_CATEGORIES.find((c) => c.key === activeKey) ?? VAULT_CATEGORIES[0],
    [activeKey],
  );

  if (!family) {
    return null;
  }

  return (
    <div className="mx-auto max-w-3xl">
      <header className="mb-4">
        <h1 className="text-2xl font-semibold text-gray-900">{t('vault.title')}</h1>
        <p className="mt-1 text-sm text-gray-500">{t('vault.subtitle')}</p>
      </header>

      {/* Mobile-first category tabs (horizontally scrollable). */}
      <div className="-mx-4 mb-5 flex gap-2 overflow-x-auto px-4 pb-1 md:mx-0 md:px-0">
        {VAULT_CATEGORIES.map((category) => {
          const active = category.key === activeKey;
          return (
            <button
              key={category.key}
              type="button"
              onClick={() => setActiveKey(category.key)}
              className={`flex shrink-0 items-center gap-1.5 whitespace-nowrap rounded-full px-4 py-2 text-sm font-medium transition ${
                active
                  ? 'bg-brand-600 text-white'
                  : 'bg-white text-gray-700 ring-1 ring-gray-200 hover:bg-gray-50'
              }`}
            >
              <span aria-hidden>{category.emoji}</span>
              {t(category.labelKey)}
            </button>
          );
        })}
      </div>

      <VaultSection
        key={activeConfig.key}
        config={activeConfig}
        familyId={familyId}
        canManage={canManage}
        members={members}
        childProfiles={children}
      />
    </div>
  );
}
