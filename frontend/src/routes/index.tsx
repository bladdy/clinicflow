import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from '../store/authStore';
import { LoginPage } from '../pages/auth/LoginPage';
import { RegisterPage } from '../pages/auth/RegisterPage';
import { DashboardLayout } from '../layouts/DashboardLayout';
import { DashboardPage } from '../pages/dashboard/DashboardPage';
import { PatientsPage } from '../pages/patients/PatientsPage';
import { ServicesPage } from '../pages/services/ServicesPage';
import { AppointmentsPage } from '../pages/appointments/AppointmentsPage';
import { ConversationsPage } from '../pages/conversations/ConversationsPage';
import { ConversationDetailPage } from '../pages/conversations/ConversationDetailPage';
import { SettingsPage } from '../pages/settings/SettingsPage';
import { WhatsAppSettingsPage } from '../pages/settings/WhatsAppSettingsPage';
import { AISettingsPage } from '../pages/settings/AISettingsPage';
import { KnowledgeBasePage } from '../pages/settings/KnowledgeBasePage';
import { ReportsPage } from '../pages/reports/ReportsPage';
import { SubscriptionPage } from '../pages/subscription/SubscriptionPage';
import { SuperAdminPage } from '../pages/admin/SuperAdminPage';
import { DoctorsPage } from '../pages/doctors/DoctorsPage';
import { BusinessHoursPage } from '../pages/business-hours/WorkingHoursPage';

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
}

function PublicRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  return !isAuthenticated ? <>{children}</> : <Navigate to="/dashboard" replace />;
}

export function AppRouter() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<PublicRoute><LoginPage /></PublicRoute>} />
          <Route path="/register" element={<PublicRoute><RegisterPage /></PublicRoute>} />
          <Route path="/" element={<ProtectedRoute><DashboardLayout /></ProtectedRoute>}>
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="dashboard" element={<DashboardPage />} />
            <Route path="patients" element={<PatientsPage />} />
            <Route path="doctors" element={<DoctorsPage />} />
            <Route path="services" element={<ServicesPage />} />
            <Route path="appointments" element={<AppointmentsPage />} />
            <Route path="conversations" element={<ConversationsPage />} />
            <Route path="conversations/:id" element={<ConversationDetailPage />} />
            <Route path="reports" element={<ReportsPage />} />
            <Route path="settings" element={<SettingsPage />} />
            <Route path="settings/whatsapp" element={<WhatsAppSettingsPage />} />
            <Route path="settings/ai" element={<AISettingsPage />} />
            <Route path="settings/knowledge" element={<KnowledgeBasePage />} />
            <Route path="settings/business-hours" element={<BusinessHoursPage />} />
            <Route path="subscription" element={<SubscriptionPage />} />
            <Route path="admin" element={<SuperAdminPage />} />
          </Route>
          <Route path="*" element={<Navigate to="/dashboard" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}
