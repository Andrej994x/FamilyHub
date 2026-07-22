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

export function eventTypeKey(type: number): string {
  return EVENT_TYPE_KEYS[type] ?? 'enums.eventType.other';
}

export function priorityKey(priority: number): string {
  return PRIORITY_KEYS[priority] ?? 'enums.priority.medium';
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
