import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import { AppRoutes } from './routes/AppRoutes';
import { InstallPrompt } from './components/InstallPrompt';
import { UpdateNotification } from './components/UpdateNotification';

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
        {/* App-wide PWA affordances: install offer and new-version prompt. */}
        <InstallPrompt />
        <UpdateNotification />
      </AuthProvider>
    </BrowserRouter>
  );
}
