import { Outlet } from 'react-router-dom';
import { Sidebar } from '../components/Sidebar';
import { BottomNav } from '../components/BottomNav';
import { Header } from '../components/Header';

export function AppLayout() {
  return (
    <div className="flex min-h-screen bg-gray-50">
      <Sidebar />

      <div className="flex min-w-0 flex-1 flex-col">
        <Header />
        {/* Extra bottom padding on mobile so content clears the bottom nav. */}
        <main className="flex-1 p-4 pb-24 md:p-6 md:pb-6">
          <Outlet />
        </main>
      </div>

      <BottomNav />
    </div>
  );
}
