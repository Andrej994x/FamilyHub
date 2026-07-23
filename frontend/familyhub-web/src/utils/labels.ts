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

// NotificationType category: 0 Family, 1 Task, 2 Calendar, 3 Shopping, 4 FamilyVault, 5 System.
const NOTIFICATION_TYPE_KEYS = [
  'notifications.types.family',
  'notifications.types.task',
  'notifications.types.calendar',
  'notifications.types.shopping',
  'notifications.types.familyVault',
  'notifications.types.system',
];

export function notificationTypeKey(type: number): string {
  return NOTIFICATION_TYPE_KEYS[type] ?? 'notifications.types.system';
}

// Icon container colours, aligned with the category order above.
const NOTIFICATION_TYPE_CLASSES = [
  'bg-violet-50 text-violet-600',
  'bg-amber-50 text-amber-600',
  'bg-sky-50 text-sky-600',
  'bg-emerald-50 text-emerald-600',
  'bg-indigo-50 text-indigo-600',
  'bg-gray-100 text-gray-500',
];

export function notificationTypeClasses(type: number): string {
  return NOTIFICATION_TYPE_CLASSES[type] ?? 'bg-gray-100 text-gray-500';
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
