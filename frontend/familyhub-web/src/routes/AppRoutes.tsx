
import { Navigate, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { PublicRoute } from './PublicRoute';
import { AppLayout } from '../layouts/AppLayout';
import Login from '../pages/Login';
import Register from '../pages/Register';
import Dashboard from '../pages/Dashboard';
import Calendar from '../pages/Calendar';
import Tasks from '../pages/Tasks';
import Shopping from '../pages/Shopping';
import Family from '../pages/Family';
import Pickups from '../pages/Pickups';

export function AppRoutes() {
  return (
    <Routes>
      {/* Public (unauthenticated) */}
      <Route element={<PublicRoute />}>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
      </Route>

      {/* Protected (authenticated) */}
      <Route element={<ProtectedRoute />}>
        <Route element={<AppLayout />}>
          <Route path="/dashboard" element={<Dashboard />} />
          <Route path="/calendar" element={<Calendar />} />
          <Route path="/tasks" element={<Tasks />} />
          <Route path="/shopping" element={<Shopping />} />
          <Route path="/family" element={<Family />} />
          <Route path="/pickups" element={<Pickups />} />
        </Route>
      </Route>

      <Route path="/" element={<Navigate to="/dashboard" replace />} />
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}
