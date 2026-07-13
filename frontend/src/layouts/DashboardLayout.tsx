import { Outlet } from 'react-router-dom';
import { Link, useLocation } from 'react-router-dom';
import { LayoutDashboard, Users, Stethoscope, Calendar, MessageSquare, Settings, LogOut, Menu, X, MessageCircle, BookOpen, Bot, BarChart3, Crown, Shield, UserCog, Clock } from 'lucide-react';
import { useState } from 'react';
import { useAuth } from '../store/authStore';

const navItems = [
  { path: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { path: '/patients', label: 'Pacientes', icon: Users },
  { path: '/doctors', label: 'Doctores', icon: UserCog },
  { path: '/services', label: 'Servicios', icon: Stethoscope },
  { path: '/appointments', label: 'Citas', icon: Calendar },
  { path: '/conversations', label: 'Conversaciones', icon: MessageSquare },
  { path: '/reports', label: 'Reportes', icon: BarChart3 },
  { path: '/settings', label: 'Configuración', icon: Settings },
  { path: '/settings/whatsapp', label: 'WhatsApp', icon: MessageCircle },
  { path: '/settings/ai', label: 'Configuración IA', icon: Bot },
  { path: '/settings/knowledge', label: 'Base de Conocimiento', icon: BookOpen },
  { path: '/settings/business-hours', label: 'Horarios', icon: Clock },
  { path: '/subscription', label: 'Suscripción', icon: Crown },
  { path: '/admin', label: 'Admin', icon: Shield, adminOnly: true },
];

export function DashboardLayout() {
  const { user, logout } = useAuth();
  const location = useLocation();
  const [sidebarOpen, setSidebarOpen] = useState(false);

  const filteredNavItems = navItems.filter((item) => {
    if ('adminOnly' in item && item.adminOnly) {
      return user?.role === 'Administrador';
    }
    return true;
  });

  const isActive = (path: string) => {
    if (path === '/settings') return location.pathname === '/settings';
    if (path === '/dashboard') return location.pathname === '/dashboard';
    return location.pathname === path || location.pathname.startsWith(path + '/');
  };

  return (
    <div className="min-h-screen bg-gray-50 flex">
      {sidebarOpen && (
        <div
          className="fixed inset-0 z-40 bg-black/50 lg:hidden"
          onClick={() => setSidebarOpen(false)}
        />
      )}

      <aside className={`fixed inset-y-0 left-0 z-50 w-64 bg-white border-r border-gray-200 transform transition-transform duration-200 ease-in-out lg:translate-x-0 lg:static lg:inset-auto flex flex-col shrink-0 ${sidebarOpen ? 'translate-x-0' : '-translate-x-full'}`}>
        <div className="flex items-center justify-between h-16 px-6 border-b border-gray-200 shrink-0">
          <h1 className="text-xl font-bold text-blue-600">DentalBot AI</h1>
          <button onClick={() => setSidebarOpen(false)} className="lg:hidden">
            <X className="h-5 w-5" />
          </button>
        </div>
        <nav className="flex-1 mt-4 px-3 space-y-1 overflow-y-auto">
          {filteredNavItems.map((item) => (
            <Link
              key={item.path}
              to={item.path}
              onClick={() => setSidebarOpen(false)}
              className={`flex items-center gap-3 px-3 py-2 rounded-lg text-sm font-medium transition-colors ${
                isActive(item.path) ? 'bg-blue-50 text-blue-700' : 'text-gray-600 hover:bg-gray-100'
              }`}
            >
              <item.icon className="h-5 w-5" />
              {item.label}
            </Link>
          ))}
        </nav>
        <div className="shrink-0 p-4 border-t border-gray-200">
          <div className="flex items-center gap-3 mb-3 px-3">
            <div className="h-8 w-8 rounded-full bg-blue-100 flex items-center justify-center text-sm font-medium text-blue-700">
              {user?.firstName?.[0]}{user?.lastName?.[0]}
            </div>
            <div className="text-sm">
              <p className="font-medium text-gray-900">{user?.firstName} {user?.lastName}</p>
              <p className="text-gray-500 text-xs">{user?.role}</p>
            </div>
          </div>
          <button onClick={logout} className="flex items-center gap-3 px-3 py-2 rounded-lg text-sm text-gray-600 hover:bg-gray-100 w-full">
            <LogOut className="h-5 w-5" />
            Cerrar sesión
          </button>
        </div>
      </aside>

      <div className="flex-1 min-w-0">
        <div className="sticky top-0 z-40 flex items-center h-16 px-4 bg-white border-b border-gray-200 lg:hidden">
          <button onClick={() => setSidebarOpen(true)}>
            <Menu className="h-6 w-6" />
          </button>
          <h1 className="ml-3 text-lg font-bold text-blue-600">DentalBot AI</h1>
        </div>
        <main className="p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
