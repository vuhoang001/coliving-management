import api from './api'
import type {
  AuthResponse, User, PagedResult, Building, Apartment, Room, Asset, ServiceCatalog,
  Amenity, AmenityBooking, Booking, Contract, CheckRecord, Incident, ServiceRequest,
  Invoice, PaymentResult, NotificationList, DashboardStats, RevenueReport, OccupancyReport
} from '@/types'

// ---------- Auth ----------
export const authApi = {
  login: (data: { email: string; password: string }) =>
    api.post<AuthResponse>('/auth/login', data).then((r) => r.data),
  register: (data: { email: string; password: string; fullName: string; phone?: string }) =>
    api.post<AuthResponse>('/auth/register', data).then((r) => r.data),
  refresh: (refreshToken: string) =>
    api.post<AuthResponse>('/auth/refresh', { refreshToken }).then((r) => r.data),
  logout: (refreshToken: string) => api.post('/auth/logout', { refreshToken }),
  me: () => api.get<User>('/auth/me').then((r) => r.data),
  updateProfile: (data: { fullName: string; phone?: string; avatarUrl?: string }) =>
    api.put<User>('/auth/profile', data).then((r) => r.data),
  changePassword: (data: { currentPassword: string; newPassword: string }) =>
    api.post('/auth/change-password', data)
}

// ---------- Users (Manager, Admin) ----------
export interface UserFilter { page?: number; pageSize?: number; role?: string; keyword?: string }
export const userApi = {
  list: (f: UserFilter) => api.get<PagedResult<User>>('/users', { params: f }).then((r) => r.data),
  byId: (id: number) => api.get<User>(`/users/${id}`).then((r) => r.data),
  create: (data: any) => api.post<User>('/users', data).then((r) => r.data),
  update: (id: number, data: any) => api.put<User>(`/users/${id}`, data).then((r) => r.data),
  setActive: (id: number, value: boolean) =>
    api.put<User>(`/users/${id}/active`, null, { params: { value } }).then((r) => r.data)
}

// ---------- Buildings ----------
export const buildingApi = {
  list: () => api.get<Building[]>('/buildings').then((r) => r.data),
  byId: (id: number) => api.get<Building>(`/buildings/${id}`).then((r) => r.data),
  create: (data: Partial<Building>) => api.post<Building>('/buildings', data).then((r) => r.data),
  update: (id: number, data: Partial<Building>) => api.put<Building>(`/buildings/${id}`, data).then((r) => r.data),
  remove: (id: number) => api.delete(`/buildings/${id}`)
}

// ---------- Apartments ----------
export const apartmentApi = {
  list: (buildingId?: number) => api.get<Apartment[]>('/apartments', { params: { buildingId } }).then((r) => r.data),
  byId: (id: number) => api.get<Apartment>(`/apartments/${id}`).then((r) => r.data),
  create: (data: Partial<Apartment>) => api.post<Apartment>('/apartments', data).then((r) => r.data),
  update: (id: number, data: Partial<Apartment>) => api.put<Apartment>(`/apartments/${id}`, data).then((r) => r.data),
  remove: (id: number) => api.delete(`/apartments/${id}`)
}

// ---------- Rooms ----------
export interface RoomFilter {
  page?: number; pageSize?: number; buildingId?: number; apartmentId?: number
  type?: string; status?: string; minPrice?: number; maxPrice?: number; keyword?: string
}
export const roomApi = {
  list: (f: RoomFilter) => api.get<PagedResult<Room>>('/rooms', { params: f }).then((r) => r.data),
  available: (from?: string, to?: string) =>
    api.get<Room[]>('/rooms/available', { params: { from, to } }).then((r) => r.data),
  byId: (id: number) => api.get<Room>(`/rooms/${id}`).then((r) => r.data),
  create: (data: Partial<Room>) => api.post<Room>('/rooms', data).then((r) => r.data),
  update: (id: number, data: Partial<Room>) => api.put<Room>(`/rooms/${id}`, data).then((r) => r.data),
  setStatus: (id: number, value: string) =>
    api.put<Room>(`/rooms/${id}/status`, null, { params: { value } }).then((r) => r.data),
  remove: (id: number) => api.delete(`/rooms/${id}`)
}

// ---------- Assets ----------
export interface AssetFilter {
  page?: number; pageSize?: number; category?: string; status?: string
  buildingId?: number; roomId?: number; keyword?: string
}
export const assetApi = {
  list: (f: AssetFilter) => api.get<PagedResult<Asset>>('/assets', { params: f }).then((r) => r.data),
  byId: (id: number) => api.get<Asset>(`/assets/${id}`).then((r) => r.data),
  create: (data: Partial<Asset>) => api.post<Asset>('/assets', data).then((r) => r.data),
  update: (id: number, data: Partial<Asset>) => api.put<Asset>(`/assets/${id}`, data).then((r) => r.data),
  remove: (id: number) => api.delete(`/assets/${id}`)
}

// ---------- Services catalog ----------
export const serviceApi = {
  list: (onlyActive = false) => api.get<ServiceCatalog[]>('/services', { params: { onlyActive } }).then((r) => r.data),
  create: (data: Partial<ServiceCatalog>) => api.post<ServiceCatalog>('/services', data).then((r) => r.data),
  update: (id: number, data: Partial<ServiceCatalog>) => api.put<ServiceCatalog>(`/services/${id}`, data).then((r) => r.data),
  remove: (id: number) => api.delete(`/services/${id}`)
}

// ---------- Amenities ----------
export const amenityApi = {
  list: (buildingId?: number) => api.get<Amenity[]>('/amenities', { params: { buildingId } }).then((r) => r.data),
  create: (data: Partial<Amenity>) => api.post<Amenity>('/amenities', data).then((r) => r.data),
  update: (id: number, data: Partial<Amenity>) => api.put<Amenity>(`/amenities/${id}`, data).then((r) => r.data),
  remove: (id: number) => api.delete(`/amenities/${id}`),
  bookings: (id: number, day: string) =>
    api.get<AmenityBooking[]>(`/amenities/${id}/bookings`, { params: { day } }).then((r) => r.data),
  myBookings: () => api.get<AmenityBooking[]>('/amenities/bookings/mine').then((r) => r.data),
  book: (data: { amenityId: number; startTime: string; endTime: string; partySize?: number; note?: string }) =>
    api.post<AmenityBooking>('/amenities/bookings', data).then((r) => r.data),
  cancelBooking: (id: number) => api.delete(`/amenities/bookings/${id}`)
}

// ---------- Bookings ----------
export interface BookingFilter { page?: number; pageSize?: number; status?: string }
export const bookingApi = {
  list: (f: BookingFilter) => api.get<PagedResult<Booking>>('/bookings', { params: f }).then((r) => r.data),
  mine: () => api.get<Booking[]>('/bookings/mine').then((r) => r.data),
  byId: (id: number) => api.get<Booking>(`/bookings/${id}`).then((r) => r.data),
  create: (data: { roomId: number; tenantId?: number; checkInDate: string; checkOutDate: string; note?: string }) =>
    api.post<Booking>('/bookings', data).then((r) => r.data),
  confirm: (id: number) => api.put<Booking>(`/bookings/${id}/confirm`).then((r) => r.data),
  cancel: (id: number, reason?: string) =>
    api.put<Booking>(`/bookings/${id}/cancel`, null, { params: { reason } }).then((r) => r.data)
}

// ---------- Contracts ----------
export const contractApi = {
  list: (status?: string) => api.get<Contract[]>('/contracts', { params: { status } }).then((r) => r.data),
  byId: (id: number) => api.get<Contract>(`/contracts/${id}`).then((r) => r.data),
  fromBooking: (bookingId: number, data: { startDate?: string; endDate?: string; terms?: string }) =>
    api.post<Contract>(`/contracts/booking/${bookingId}`, data).then((r) => r.data),
  sign: (id: number, data: { signature: string; documentUrl?: string }) =>
    api.post<Contract>(`/contracts/${id}/sign`, data).then((r) => r.data),
  terminate: (id: number, reason?: string) =>
    api.put<Contract>(`/contracts/${id}/terminate`, null, { params: { reason } }).then((r) => r.data)
}

// ---------- Check in/out ----------
export interface CheckPayload {
  bookingId: number; electricityMeter?: number; waterMeter?: number; conditionNote?: string; photoUrl?: string
}
export const checkApi = {
  byBooking: (bookingId: number) => api.get<CheckRecord[]>(`/check/booking/${bookingId}`).then((r) => r.data),
  checkIn: (data: CheckPayload) => api.post<CheckRecord>('/check/in', data).then((r) => r.data),
  checkOut: (data: CheckPayload) => api.post<CheckRecord>('/check/out', data).then((r) => r.data)
}

// ---------- Incidents ----------
export interface IncidentFilter {
  page?: number; pageSize?: number; status?: string; priority?: string
  buildingId?: number; assignedToId?: number; keyword?: string
}
export const incidentApi = {
  list: (f: IncidentFilter) => api.get<PagedResult<Incident>>('/incidents', { params: f }).then((r) => r.data),
  mine: () => api.get<Incident[]>('/incidents/mine').then((r) => r.data),
  byId: (id: number) => api.get<Incident>(`/incidents/${id}`).then((r) => r.data),
  create: (data: any) => api.post<Incident>('/incidents', data).then((r) => r.data),
  assign: (id: number, staffId: number) =>
    api.put<Incident>(`/incidents/${id}/assign`, null, { params: { staffId } }).then((r) => r.data),
  setStatus: (id: number, data: { status: string; resolutionNote?: string }) =>
    api.put<Incident>(`/incidents/${id}/status`, data).then((r) => r.data)
}

// ---------- Service requests ----------
export interface ServiceRequestFilter { page?: number; pageSize?: number; status?: string }
export const serviceRequestApi = {
  list: (f: ServiceRequestFilter) =>
    api.get<PagedResult<ServiceRequest>>('/service-requests', { params: f }).then((r) => r.data),
  mine: () => api.get<ServiceRequest[]>('/service-requests/mine').then((r) => r.data),
  create: (data: { serviceCatalogId: number; roomId?: number; scheduledAt?: string; quantity: number; note?: string }) =>
    api.post<ServiceRequest>('/service-requests', data).then((r) => r.data),
  setStatus: (id: number, status: string, assignedToId?: number) =>
    api.put<ServiceRequest>(`/service-requests/${id}/status`, null, { params: { status, assignedToId } }).then((r) => r.data),
  cancel: (id: number) => api.put<ServiceRequest>(`/service-requests/${id}/cancel`).then((r) => r.data)
}

// ---------- Invoices ----------
export interface InvoiceFilter { page?: number; pageSize?: number; status?: string }
export const invoiceApi = {
  list: (f: InvoiceFilter) => api.get<PagedResult<Invoice>>('/invoices', { params: f }).then((r) => r.data),
  mine: () => api.get<Invoice[]>('/invoices/mine').then((r) => r.data),
  byId: (id: number) => api.get<Invoice>(`/invoices/${id}`).then((r) => r.data),
  create: (data: any) => api.post<Invoice>('/invoices', data).then((r) => r.data),
  issue: (id: number) => api.put<Invoice>(`/invoices/${id}/issue`).then((r) => r.data),
  split: (id: number, parts: { tenantId: number; amount?: number }[]) =>
    api.put<Invoice>(`/invoices/${id}/split`, { parts }).then((r) => r.data),
  remove: (id: number) => api.delete(`/invoices/${id}`)
}

// ---------- Payments ----------
export const paymentApi = {
  pay: (data: { invoiceId: number; amount?: number; method: string; transactionRef?: string }) =>
    api.post<PaymentResult>('/payments/pay', data).then((r) => r.data),
  vnpay: (invoiceId: number) =>
    api.post<{ paymentUrl: string; isMock: boolean }>(`/payments/vnpay/${invoiceId}`).then((r) => r.data),
  // Chốt giao dịch ở cổng VNPay giả lập (chỉ dùng cho trang /payment/mock)
  mockComplete: (invoiceId: number, success: boolean) =>
    api.post<PaymentResult>(`/payments/vnpay/mock/${invoiceId}`, null, { params: { success } }).then((r) => r.data)
}

// ---------- Notifications ----------
export const notificationApi = {
  mine: () => api.get<NotificationList>('/notifications').then((r) => r.data),
  read: (id: number) => api.put(`/notifications/${id}/read`),
  readAll: () => api.put('/notifications/read-all')
}

// ---------- Dashboard ----------
export const dashboardApi = {
  stats: () => api.get<DashboardStats>('/dashboard/stats').then((r) => r.data),
  revenue: (months = 6) => api.get<RevenueReport>('/dashboard/revenue', { params: { months } }).then((r) => r.data),
  occupancy: () => api.get<OccupancyReport>('/dashboard/occupancy').then((r) => r.data)
}

// ---------- Uploads ----------
export const uploadApi = {
  image: (file: File, folder?: string) =>
    api.postForm<{ url: string }>('/uploads/image', { file }, { params: { folder } }).then((r) => r.data)
}
