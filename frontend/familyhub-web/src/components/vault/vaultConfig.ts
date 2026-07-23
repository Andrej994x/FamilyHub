import type { VaultCategoryKey } from '../../services/vaultService';

export type FieldType =
  | 'text'
  | 'textarea'
  | 'date' // DateTimeOffset on the API, edited as a calendar date
  | 'dateonly' // DateOnly on the API (e.g. a birth date)
  | 'number'
  | 'documentType'
  | 'subject'; // documents only: a family member or a child

export interface FieldDef {
  name: string;
  labelKey: string;
  type: FieldType;
  required?: boolean;
}

export interface ExpiryFieldDef {
  name: string;
  labelKey: string;
}

export interface CategoryConfig {
  key: VaultCategoryKey;
  labelKey: string;
  emoji: string;
  /** Field whose value is shown as the record's title (documents use the document type). */
  titleField: string;
  /** Extra fields surfaced as small subtitles on the list card. */
  subtitleFields: string[];
  fields: FieldDef[];
  /** Date fields that drive the Valid / Expiring Soon / Expired status chips. */
  expiryFields: ExpiryFieldDef[];
  /** Documents keep a single attachment; the other categories keep many. */
  singleAttachment: boolean;
}

export const VAULT_CATEGORIES: CategoryConfig[] = [
  {
    key: 'documents',
    labelKey: 'vault.tabs.documents',
    emoji: '📄',
    titleField: 'documentNumber',
    subtitleFields: ['documentNumber'],
    singleAttachment: true,
    fields: [
      { name: 'subject', labelKey: 'vault.fields.belongsTo', type: 'subject', required: true },
      { name: 'documentType', labelKey: 'vault.fields.documentType', type: 'documentType', required: true },
      { name: 'documentNumber', labelKey: 'vault.fields.documentNumber', type: 'text' },
      { name: 'issueDate', labelKey: 'vault.fields.issueDate', type: 'date' },
      { name: 'expiryDate', labelKey: 'vault.fields.expiryDate', type: 'date' },
      { name: 'notes', labelKey: 'vault.fields.notes', type: 'textarea' },
    ],
    expiryFields: [{ name: 'expiryDate', labelKey: 'vault.fields.expiryDate' }],
  },
  {
    key: 'vehicles',
    labelKey: 'vault.tabs.vehicles',
    emoji: '🚗',
    titleField: 'name',
    subtitleFields: ['make', 'model', 'registrationNumber'],
    singleAttachment: false,
    fields: [
      { name: 'name', labelKey: 'vault.fields.name', type: 'text', required: true },
      { name: 'make', labelKey: 'vault.fields.make', type: 'text' },
      { name: 'model', labelKey: 'vault.fields.model', type: 'text' },
      { name: 'registrationNumber', labelKey: 'vault.fields.registrationNumber', type: 'text' },
      { name: 'registrationExpiry', labelKey: 'vault.fields.registrationExpiry', type: 'date' },
      { name: 'insuranceExpiry', labelKey: 'vault.fields.insuranceExpiry', type: 'date' },
      { name: 'nextServiceDate', labelKey: 'vault.fields.nextServiceDate', type: 'date' },
      { name: 'nextServiceMileage', labelKey: 'vault.fields.nextServiceMileage', type: 'number' },
      { name: 'notes', labelKey: 'vault.fields.notes', type: 'textarea' },
    ],
    expiryFields: [
      { name: 'registrationExpiry', labelKey: 'vault.fields.registration' },
      { name: 'insuranceExpiry', labelKey: 'vault.fields.insurance' },
      { name: 'nextServiceDate', labelKey: 'vault.fields.service' },
    ],
  },
  {
    key: 'pets',
    labelKey: 'vault.tabs.pets',
    emoji: '🐾',
    titleField: 'name',
    subtitleFields: ['type', 'breed'],
    singleAttachment: false,
    fields: [
      { name: 'name', labelKey: 'vault.fields.name', type: 'text', required: true },
      { name: 'type', labelKey: 'vault.fields.petType', type: 'text' },
      { name: 'breed', labelKey: 'vault.fields.breed', type: 'text' },
      { name: 'dateOfBirth', labelKey: 'vault.fields.dateOfBirth', type: 'dateonly' },
      { name: 'microchipNumber', labelKey: 'vault.fields.microchipNumber', type: 'text' },
      { name: 'vaccinationName', labelKey: 'vault.fields.vaccinationName', type: 'text' },
      { name: 'lastVaccinationDate', labelKey: 'vault.fields.lastVaccinationDate', type: 'date' },
      { name: 'nextVaccinationDate', labelKey: 'vault.fields.nextVaccinationDate', type: 'date' },
      { name: 'veterinarian', labelKey: 'vault.fields.veterinarian', type: 'text' },
      { name: 'notes', labelKey: 'vault.fields.notes', type: 'textarea' },
    ],
    expiryFields: [{ name: 'nextVaccinationDate', labelKey: 'vault.fields.vaccination' }],
  },
  {
    key: 'home',
    labelKey: 'vault.tabs.home',
    emoji: '🏠',
    titleField: 'title',
    subtitleFields: ['type', 'provider'],
    singleAttachment: false,
    fields: [
      { name: 'title', labelKey: 'vault.fields.title', type: 'text', required: true },
      { name: 'type', labelKey: 'vault.fields.homeType', type: 'text' },
      { name: 'provider', labelKey: 'vault.fields.provider', type: 'text' },
      { name: 'issueDate', labelKey: 'vault.fields.issueDate', type: 'date' },
      { name: 'renewalDate', labelKey: 'vault.fields.renewalDate', type: 'date' },
      { name: 'notes', labelKey: 'vault.fields.notes', type: 'textarea' },
    ],
    expiryFields: [{ name: 'renewalDate', labelKey: 'vault.fields.renewal' }],
  },
  {
    key: 'warranties',
    labelKey: 'vault.tabs.warranties',
    emoji: '🧾',
    titleField: 'productName',
    subtitleFields: ['store', 'serialNumber'],
    singleAttachment: false,
    fields: [
      { name: 'productName', labelKey: 'vault.fields.productName', type: 'text', required: true },
      { name: 'store', labelKey: 'vault.fields.store', type: 'text' },
      { name: 'purchaseDate', labelKey: 'vault.fields.purchaseDate', type: 'date' },
      { name: 'warrantyExpiryDate', labelKey: 'vault.fields.warrantyExpiryDate', type: 'date' },
      { name: 'serialNumber', labelKey: 'vault.fields.serialNumber', type: 'text' },
      { name: 'notes', labelKey: 'vault.fields.notes', type: 'textarea' },
    ],
    expiryFields: [{ name: 'warrantyExpiryDate', labelKey: 'vault.fields.warranty' }],
  },
  {
    key: 'other',
    labelKey: 'vault.tabs.other',
    emoji: '📌',
    titleField: 'title',
    subtitleFields: ['description'],
    singleAttachment: false,
    fields: [
      { name: 'title', labelKey: 'vault.fields.title', type: 'text', required: true },
      { name: 'description', labelKey: 'vault.fields.description', type: 'textarea' },
      { name: 'importantDate', labelKey: 'vault.fields.importantDate', type: 'date' },
      { name: 'expiryDate', labelKey: 'vault.fields.expiryDate', type: 'date' },
    ],
    expiryFields: [{ name: 'expiryDate', labelKey: 'vault.fields.expiryDate' }],
  },
];

export function getCategoryConfig(key: VaultCategoryKey): CategoryConfig {
  return VAULT_CATEGORIES.find((c) => c.key === key) ?? VAULT_CATEGORIES[0];
}
