import { StrictMode, Suspense } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import './locales/i18n';
import App from './App.tsx';
import { FullPageLoader } from './components/FullPageLoader';

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <Suspense fallback={<FullPageLoader />}>
      <App />
    </Suspense>
  </StrictMode>,
);
