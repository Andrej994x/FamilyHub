import { useEffect } from 'react';
import { Outlet, useLocation } from 'react-router-dom';
import { Sidebar } from '../components/Sidebar';
import { BottomNav } from '../components/BottomNav';
import { Header } from '../components/Header';
import { NotificationsProvider } from '../contexts/NotificationsContext';

export function AppLayout() {
  // Start each page at the top when navigating between tabs.
  const { pathname } = useLocation();
  useEffect(() => {
    window.scrollTo(0, 0);
  }, [pathname]);

  return (
    <NotificationsProvider>
      <div className="flex min-h-screen bg-gray-50">
        <Sidebar />

        <div className="flex min-w-0 flex-1 flex-col">
          <Header />
          {/* Extra bottom padding on mobile so content clears the bottom nav + safe area. */}
          <main className="flex-1 p-4 pb-[calc(5.5rem+env(safe-area-inset-bottom))] md:p-6 md:pb-6">
            <Outlet />
          </main>
        </div>

        <BottomNav />
      </div>
    </NotificationsProvider>
  );
}
