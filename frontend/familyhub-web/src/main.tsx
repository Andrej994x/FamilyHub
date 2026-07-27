import { StrictMode, Suspense } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import './locales/i18n';
import App from './App.tsx';
import { FullPageLoader } from './components/FullPageLoader';
import { registerServiceWorker } from './pwa/serviceWorkerRegistration';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Suspense fallback={<FullPageLoader />}>
      <App />
    </Suspense>
  </StrictMode>,
);

// Register the service worker to make the app installable, offline-capable, and
// self-updating. Only in production builds — in dev it would interfere with Vite's HMR.
if (import.meta.env.PROD) {
  window.addEventListener('load', () => registerServiceWorker());
}
