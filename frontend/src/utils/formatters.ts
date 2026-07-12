export function formatDate(date: string): string {
  return new Date(date).toLocaleDateString('es-MX', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  });
}

export function formatDateTime(date: string): string {
  return new Date(date).toLocaleString('es-MX', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('es-MX', {
    style: 'currency',
    currency: 'MXN',
  }).format(amount);
}

export function formatTime(time: string): string {
  const parts = time.split(':');
  const hours = parseInt(parts[0], 10);
  const minutes = parts[1] ?? '00';
  const period = hours >= 12 ? 'PM' : 'AM';
  const h = hours % 12 || 12;
  return `${h}:${minutes} ${period}`;
}

export function formatPhone(phone: string): string {
  const cleaned = phone.replace(/\D/g, '');
  if (cleaned.length === 10) {
    return `(${cleaned.slice(0, 3)}) ${cleaned.slice(3, 6)}-${cleaned.slice(6)}`;
  }
  return phone;
}

export const statusColors: Record<string, string> = {
  Scheduled: 'bg-blue-100 text-blue-800',
  Confirmed: 'bg-green-100 text-green-800',
  InProgress: 'bg-yellow-100 text-yellow-800',
  Completed: 'bg-gray-100 text-gray-800',
  Cancelled: 'bg-red-100 text-red-800',
  NoShow: 'bg-orange-100 text-orange-800',
  Open: 'bg-green-100 text-green-800',
  Closed: 'bg-gray-100 text-gray-800',
  Archived: 'bg-slate-100 text-slate-800',
};

export const statusLabels: Record<string, string> = {
  Scheduled: 'Programada',
  Confirmed: 'Confirmada',
  InProgress: 'En Progreso',
  Completed: 'Completada',
  Cancelled: 'Cancelada',
  NoShow: 'No Asistió',
  Open: 'Abierta',
  Closed: 'Cerrada',
  Archived: 'Archivada',
};
