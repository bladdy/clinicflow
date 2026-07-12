export interface ApiResponse<T> {
  success: boolean;
  message?: string;
  data?: T;
  errors?: string[];
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  firstName: string;
  lastName: string;
  phone?: string;
  companyId?: string;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  expiration: string;
  user: UserDto;
}

export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phone?: string;
  role: string;
  companyId?: string;
  branchId?: string;
}

export interface Company {
  id: string;
  name: string;
  phone: string;
  email: string;
  address: string;
  logoUrl?: string;
  website?: string;
  taxId?: string;
}

export interface Branch {
  id: string;
  companyId: string;
  name: string;
  phone: string;
  email: string;
  address: string;
  isMain: boolean;
  company?: Company;
}

export interface Doctor {
  id: string;
  userId: string;
  companyId: string;
  specialty: string;
  licenseNumber: string;
  bio?: string;
  photoUrl?: string;
  color: string;
  fullName?: string;
  email?: string;
  user?: UserDto;
}

export interface Patient {
  id: string;
  companyId: string;
  branchId?: string;
  firstName: string;
  lastName: string;
  email?: string;
  phone: string;
  dateOfBirth?: string;
  gender?: string;
  address?: string;
  notes?: string;
  medicalHistory?: string;
}

export interface Service {
  id: string;
  companyId: string;
  name: string;
  description?: string;
  durationMinutes: number;
  price: number;
  category?: string;
  isActive: boolean;
}

export interface Appointment {
  id: string;
  companyId: string;
  branchId: string;
  doctorId: string;
  patientId: string;
  serviceId: string;
  appointmentDate: string;
  startTime: string;
  endTime: string;
  status: string;
  notes?: string;
  reason?: string;
  patientName?: string;
  doctorName?: string;
  serviceName?: string;
  branchName?: string;
  doctorColor?: string;
  doctor?: Doctor;
  patient?: Patient;
  service?: Service;
  branch?: Branch;
}

export interface Conversation {
  id: string;
  companyId: string;
  patientId?: string;
  phone: string;
  status: string;
  startedAt: string;
  endedAt?: string;
  patient?: Patient;
  lastMessage?: Message;
}

export interface Message {
  id: string;
  conversationId: string;
  content: string;
  direction: string;
  senderType: string;
  sentAt: string;
  isRead: boolean;
}

export interface AvailableSlot {
  startTime: string;
  endTime: string;
  isAvailable: boolean;
}

export interface ConversationDetail extends Conversation {
  messages: Message[];
}

export interface WhatsAppInstance {
  id: string;
  companyId: string;
  branchId?: string;
  instanceName: string;
  apiUrl: string;
  apiKey: string;
  phoneNumber: string;
  isActive: boolean;
  webhookUrl?: string;
}

export interface WhatsAppInstanceDetail extends WhatsAppInstance {
  connectionState: string;
  webhookConfigured: boolean;
  webhookEvents: string[];
  createdAt: string;
}

export interface KnowledgeArticle {
  id: string;
  companyId: string;
  title: string;
  content: string;
  category?: string;
  keywords?: string;
  isActive: boolean;
}

export interface DashboardStats {
  todayAppointments: number;
  totalPatients: number;
  activeConversations: number;
  pendingAppointments: number;
}

export interface PaginationParams {
  page: number;
  pageSize: number;
}
