import { BrowserRouter } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import { AppRoutes } from './routes/AppRoutes';
import { InstallPrompt } from './components/InstallPrompt';
import { UpdateNotification } from './components/UpdateNotification';
import { PushNavigationListener } from './components/PushNavigationListener';

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <AppRoutes />
        {/* App-wide PWA affordances: install offer, new-version prompt, push click routing. */}
        <InstallPrompt />
        <UpdateNotification />
        <PushNavigationListener />
      </AuthProvider>
    </BrowserRouter>
  );
}
