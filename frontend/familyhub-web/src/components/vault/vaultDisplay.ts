import { formatDate } from '../../utils/format';
import { documentTypeKey } from '../../utils/vaultStatus';
import type { ChildResponse, FamilyMemberResponse, VaultRecord } from '../../types';
import type { CategoryConfig, FieldDef } from './vaultConfig';

type Translate = (key: string) => string;

export function recordTitle(config: CategoryConfig, record: VaultRecord, t: Translate): string {
  if (config.key === 'documents') {
    return t(documentTypeKey(Number(record.documentType ?? 0)));
  }
  const value = record[config.titleField];
  return typeof value === 'string' && value.trim() !== '' ? value : t('vault.untitled');
}

export function subjectName(
  record: VaultRecord,
  members: FamilyMemberResponse[],
  children: ChildResponse[],
  t: Translate,
): string | null {
  if (record.familyMemberId) {
    const member = members.find((m) => m.id === record.familyMemberId);
    return member ? `${member.firstName} ${member.lastName}` : t('vault.unknownSubject');
  }
  if (record.childProfileId) {
    const child = children.find((c) => c.id === record.childProfileId);
    return child ? `${child.firstName} ${child.lastName}` : t('vault.unknownSubject');
  }
  return null;
}

export function formatFieldValue(
  field: FieldDef,
  record: VaultRecord,
  t: Translate,
  locale: string,
): string {
  const raw = record[field.name];
  if (field.type === 'documentType') {
    return t(documentTypeKey(Number(raw ?? 0)));
  }
  if (raw === null || raw === undefined || raw === '') {
    return '';
  }
  if (field.type === 'date' || field.type === 'dateonly') {
    return formatDate(String(raw), locale);
  }
  return String(raw);
}

export function attachmentCount(record: VaultRecord): number {
  if (record.attachments) {
    return record.attachments.length;
  }
  return record.hasAttachment ? 1 : 0;
}

export function fileNameFromPath(path: string | null | undefined): string | null {
  if (!path) return null;
  const parts = path.split('/');
  return parts[parts.length - 1] || null;
}

export function inferContentType(path: string | null | undefined): string {
  const lower = (path ?? '').toLowerCase();
  if (lower.endsWith('.pdf')) return 'application/pdf';
  if (lower.endsWith('.png')) return 'image/png';
  if (lower.endsWith('.jpg') || lower.endsWith('.jpeg')) return 'image/jpeg';
  return 'application/octet-stream';
}
