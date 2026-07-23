import { Navigate, Route, Routes } from 'react-router-dom';
import { ProtectedRoute } from './ProtectedRoute';
import { PublicRoute } from './PublicRoute';
import { FamilyGate } from './FamilyGate';
import { RequireFamily } from './RequireFamily';
import { AppLayout } from '../layouts/AppLayout';
import Login from '../pages/Login';
import Register from '../pages/Register';
import CreateFamilyPage from '../pages/CreateFamilyPage';
import Dashboard from '../pages/Dashboard';
import Calendar from '../pages/Calendar';
import Tasks from '../pages/Tasks';
import Shopping from '../pages/Shopping';
import FamilyMembersPage from '../pages/FamilyMembersPage';
import Pickups from '../pages/Pickups';
import Notifications from '../pages/Notifications';
import FamilyVault from '../pages/FamilyVault';
import AcceptInvitation from '../pages/AcceptInvitation';

export function AppRoutes() {
  return (
    <Routes>
      {/* Invitation acceptance — reachable whether or not the user is signed in. */}
      <Route path="/invitations/accept" element={<AcceptInvitation />} />

      {/* Public (unauthenticated) */}
      <Route element={<PublicRoute />}>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
      </Route>

      {/* Authenticated — family context available to all children */}
      <Route element={<ProtectedRoute />}>
        <Route element={<FamilyGate />}>
          {/* Onboarding: create or join a family */}
          <Route path="/onboarding" element={<CreateFamilyPage />} />

          {/* Everything else requires a family */}
          <Route element={<RequireFamily />}>
            <Route element={<AppLayout />}>
              <Route path="/dashboard" element={<Dashboard />} />
              <Route path="/calendar" element={<Calendar />} />
              <Route path="/tasks" element={<Tasks />} />
              <Route path="/shopping" element={<Shopping />} />
              <Route path="/family" element={<FamilyMembersPage />} />
              <Route path="/pickups" element={<Pickups />} />
              <Route path="/vault" element={<FamilyVault />} />
              <Route path="/notifications" element={<Notifications />} />
            </Route>
          </Route>
        </Route>
      </Route>

      <Route path="/" element={<Navigate to="/dashboard" replace />} />
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  );
}
