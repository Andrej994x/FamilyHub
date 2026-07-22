const EVENT_TYPE_KEYS = [
  'enums.eventType.kindergarten',
  'enums.eventType.school',
  'enums.eventType.doctor',
  'enums.eventType.training',
  'enums.eventType.birthday',
  'enums.eventType.family',
  'enums.eventType.other',
];

const PRIORITY_KEYS = ['enums.priority.low', 'enums.priority.medium', 'enums.priority.high'];

const PICKUP_STATUS_KEYS = [
  'enums.pickupStatus.pending',
  'enums.pickupStatus.confirmed',
  'enums.pickupStatus.cannotAttend',
  'enums.pickupStatus.completed',
];

const TASK_STATUS_KEYS = [
  'enums.taskStatus.pending',
  'enums.taskStatus.inProgress',
  'enums.taskStatus.completed',
  'enums.taskStatus.cancelled',
];

const ITEM_CATEGORY_KEYS = [
  'enums.itemCategory.grocery',
  'enums.itemCategory.pharmacy',
  'enums.itemCategory.home',
  'enums.itemCategory.child',
  'enums.itemCategory.other',
];

export function itemCategoryKey(category: number): string {
  return ITEM_CATEGORY_KEYS[category] ?? 'enums.itemCategory.other';
}

export function eventTypeKey(type: number): string {
  return EVENT_TYPE_KEYS[type] ?? 'enums.eventType.other';
}

export function priorityKey(priority: number): string {
  return PRIORITY_KEYS[priority] ?? 'enums.priority.medium';
}

// Kindergarten, School, Doctor, Training, Birthday, Family, Other
const EVENT_TYPE_CLASSES = [
  'bg-sky-50 text-sky-700',
  'bg-indigo-50 text-indigo-700',
  'bg-red-50 text-red-700',
  'bg-emerald-50 text-emerald-700',
  'bg-pink-50 text-pink-700',
  'bg-amber-50 text-amber-700',
  'bg-gray-100 text-gray-600',
];

export function eventTypeClasses(type: number): string {
  return EVENT_TYPE_CLASSES[type] ?? 'bg-gray-100 text-gray-600';
}

export function pickupStatusKey(status: number): string {
  return PICKUP_STATUS_KEYS[status] ?? 'enums.pickupStatus.pending';
}

export function priorityClasses(priority: number): string {
  switch (priority) {
    case 2:
      return 'bg-red-50 text-red-700';
    case 1:
      return 'bg-amber-50 text-amber-700';
    default:
      return 'bg-gray-100 text-gray-600';
  }
}

export function pickupStatusClasses(status: number): string {
  switch (status) {
    case 1:
      return 'bg-green-50 text-green-700';
    case 2:
      return 'bg-red-50 text-red-700';
    case 3:
      return 'bg-gray-100 text-gray-600';
    default:
      return 'bg-amber-50 text-amber-700';
  }
}

export function taskStatusKey(status: number): string {
  return TASK_STATUS_KEYS[status] ?? 'enums.taskStatus.pending';
}

export function taskStatusClasses(status: number): string {
  switch (status) {
    case 1:
      return 'bg-blue-50 text-blue-700';
    case 2:
      return 'bg-green-50 text-green-700';
    case 3:
      return 'bg-gray-100 text-gray-500';
    default:
      return 'bg-amber-50 text-amber-700';
  }
}
