import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';

/**
 * Listens for PUSH_NAVIGATE messages from the service worker (sent when the user taps a push)
 * and performs an in-app React Router navigation. Renders nothing.
 */
export function PushNavigationListener() {
  const navigate = useNavigate();

  useEffect(() => {
    if (!('serviceWorker' in navigator)) {
      return;
    }
    const onMessage = (event: MessageEvent) => {
      const data = event.data;
      if (data && data.type === 'PUSH_NAVIGATE' && typeof data.url === 'string') {
        navigate(data.url);
      }
    };
    navigator.serviceWorker.addEventListener('message', onMessage);
    return () => navigator.serviceWorker.removeEventListener('message', onMessage);
  }, [navigate]);

  return null;
}
