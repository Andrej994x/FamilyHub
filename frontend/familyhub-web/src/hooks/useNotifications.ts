import { useContext } from 'react';
import {
  NotificationsContext,
  type NotificationsContextValue,
} from '../contexts/NotificationsContext';

export function useNotifications(): NotificationsContextValue {
  const context = useContext(NotificationsContext);
  if (context === undefined) {
    throw new Error('useNotifications must be used within a NotificationsProvider');
  }
  return context;
}
