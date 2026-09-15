// ---------- Chung ----------
export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  totalItems: number
  totalPages: number
}

export type Role = 'Tenant' | 'Staff' | 'Manager' | 'Admin'

// ---------- Auth / User ----------
export interface User {
  id: number
  email: string
  fullName: string
  phone?: string
  avatarUrl?: string
  role: Role
  isActive: boolean
  emailConfirmed: boolean
  identityNumber?: string
  // Hồ sơ khách thuê mở rộng
  dateOfBirth?: string
  gender?: string
  permanentAddress?: string
  occupation?: string
  nationality?: string
  emergencyContactName?: string
  emergencyContactPhone?: string
  idIssueDate?: string
  idIssuePlace?: string
}

export interface AuthResponse {
  token: string
  refreshToken: string
  expiresAt: string
  user: User
}

// ---------- Building / Apartment / Room ----------
export interface Building {
  id: number
  name: string
  address?: string
  description?: string
  floors?: number
  apartmentCount?: number
  roomCount?: number
  // Mở rộng
  district?: string
  ward?: string
  contactPhone?: string
  contactEmail?: string
  yearBuilt?: number
  totalFloorArea?: number
  parkingSlots?: number
  hasElevator?: boolean
  notes?: string
}

export interface Apartment {
  id: number
  buildingId: number
  buildingName?: string
  name: string
  floor?: number
  description?: string
  roomCount?: number
  // Mở rộng
  direction?: string
  furnishing?: string
  hasBalcony?: boolean
  maintenanceFee?: number
  notes?: string
}

export type RoomStatus = 'Available' | 'Occupied' | 'Reserved' | 'Maintenance'

export interface Room {
  id: number
  apartmentId: number
  apartmentName?: string
  buildingId?: number
  buildingName?: string
  name: string
  code?: string
  type?: string
  area?: number
  capacity?: number
  price: number
  status: RoomStatus
  description?: string
  photoUrl?: string
  deposit?: number
  // Mở rộng
  hasWindow?: boolean
  hasPrivateBathroom?: boolean
  hasAirConditioner?: boolean
  electricityUnitPrice?: number
  waterUnitPrice?: number
  internetFee?: number
  imageUrl?: string
  notes?: string
}

// ---------- Asset ----------
export interface Asset {
  id: number
  name: string
  code?: string
  category?: string
  status?: string
  buildingId?: number
  buildingName?: string
  roomId?: number
  roomName?: string
  quantity?: number
  purchasePrice?: number
  purchaseDate?: string
  note?: string
  // Mở rộng
  brand?: string
  model?: string
  warrantyUntil?: string
  supplier?: string
  serialNumber?: string
  lastMaintenanceAt?: string
}

// ---------- Service catalog ----------
export interface ServiceCatalog {
  id: number
  name: string
  description?: string
  unit?: string
  price: number
  isActive: boolean
}

// ---------- Amenity ----------
export interface Amenity {
  id: number
  buildingId?: number
  buildingName?: string
  name: string
  description?: string
  capacity?: number
  openTime?: string
  closeTime?: string
  isActive?: boolean
}

export interface AmenityBooking {
  id: number
  amenityId: number
  amenityName?: string
  tenantId?: number
  tenantName?: string
  startTime: string
  endTime: string
  partySize?: number
  note?: string
  status?: string
}

// ---------- Booking ----------
export type BookingStatus = 'Pending' | 'Confirmed' | 'Cancelled' | 'Completed'

export interface Booking {
  id: number
  roomId: number
  roomName?: string
  buildingName?: string
  tenantId: number
  tenantName?: string
  checkInDate: string
  checkOutDate: string
  status: BookingStatus
  note?: string
  createdAt?: string
  // Mở rộng
  numberOfOccupants?: number
  purpose?: string
  sourceChannel?: string
  vehiclePlate?: string
}

// ---------- Contract ----------
export type ContractStatus = 'Draft' | 'PendingSignature' | 'Active' | 'Terminated' | 'Expired'

export interface Contract {
  id: number
  bookingId?: number
  roomId?: number
  roomName?: string
  tenantId?: number
  tenantName?: string
  startDate: string
  endDate: string
  terms?: string
  status: ContractStatus
  signature?: string
  documentUrl?: string
  signedAt?: string
  createdAt?: string
  contractNumber?: string
  monthlyRent?: number
  deposit?: number
  // Mở rộng
  contractType?: string
  paymentCycle?: string
  noticePeriodDays?: number
  lateFeePercent?: number
  utilitiesIncluded?: boolean
  maxOccupants?: number
  depositPaid?: boolean
  renewalTerms?: string
}

// ---------- Check in/out ----------
export interface CheckRecord {
  id: number
  bookingId: number
  type: 'CheckIn' | 'CheckOut'
  electricityMeter?: number
  waterMeter?: number
  conditionNote?: string
  photoUrl?: string
  createdAt?: string
}

// ---------- Incident ----------
export type IncidentPriority = 'Low' | 'Medium' | 'High' | 'Urgent'
export type IncidentStatus = 'Open' | 'Assigned' | 'InProgress' | 'Resolved' | 'Closed'

export interface Incident {
  id: number
  title: string
  description?: string
  priority: IncidentPriority
  status: IncidentStatus
  buildingId?: number
  buildingName?: string
  apartmentId?: number
  roomId?: number
  roomName?: string
  assetId?: number
  assetName?: string
  photoUrl?: string
  reportedById?: number
  reportedByName?: string
  assignedToId?: number
  assignedToName?: string
  resolutionNote?: string
  createdAt?: string
  // Mở rộng
  category?: string
  locationDetail?: string
  contactPhone?: string
  expectedResolutionDate?: string
  cost?: number
}

// ---------- Service request ----------
export type ServiceRequestStatus = 'Pending' | 'Scheduled' | 'InProgress' | 'Completed' | 'Cancelled'

export interface ServiceRequest {
  id: number
  serviceCatalogId: number
  serviceName?: string
  roomId?: number
  roomName?: string
  tenantId?: number
  tenantName?: string
  scheduledAt?: string
  quantity: number
  note?: string
  status: ServiceRequestStatus
  assignedToId?: number
  assignedToName?: string
  createdAt?: string
  // Mở rộng
  contactPhone?: string
  locationDetail?: string
}

// ---------- Invoice / Payment ----------
export type InvoiceStatus = 'Draft' | 'Issued' | 'PartiallyPaid' | 'Paid' | 'Overdue' | 'Cancelled'

export interface InvoiceItem {
  id?: number
  type?: string
  description: string
  quantity: number
  unitPrice: number
  amount?: number
  // Mở rộng
  unit?: string
  note?: string
}

export interface InvoiceShare {
  tenantId: number
  tenantName?: string
  shareAmount: number
  isPaid: boolean
}

export interface InvoicePayment {
  id: number
  amount: number
  method: string
  transactionRef?: string
  paidAt?: string
  tenantName?: string
}

export interface Invoice {
  id: number
  code?: string
  tenantId: number
  tenantName?: string
  roomId?: number
  roomName?: string
  contractId?: number
  periodStart?: string
  periodEnd?: string
  dueDate?: string
  note?: string
  status: InvoiceStatus
  items: InvoiceItem[]
  shares: InvoiceShare[]
  payments: InvoicePayment[]
  subtotal: number
  total: number
  paidAmount: number
  createdAt?: string
  // Mở rộng: chỉ số công tơ + giảm giá/thuế
  previousElectricityReading?: number
  currentElectricityReading?: number
  previousWaterReading?: number
  currentWaterReading?: number
  discount?: number
  tax?: number
}

export interface PaymentResult {
  success: boolean
  message: string
  invoiceId: number
  invoiceStatus: string
  paidAmount: number
  responseCode?: string
  redirectUrl?: string
}

// ---------- Notification ----------
export interface AppNotification {
  id: number
  title: string
  message: string
  type?: string
  link?: string
  isRead: boolean
  createdAt: string
}

export interface NotificationList {
  items: AppNotification[]
  unreadCount: number
}

// ---------- Dashboard ----------
export interface DashboardStats {
  buildings: number
  rooms: number
  occupiedRooms: number
  availableRooms: number
  occupancyRate: number
  activeTenants: number
  activeContracts: number
  openIncidents: number
  pendingBookings: number
  revenueThisMonth: number
  outstandingAmount: number
  overdueInvoices: number
}

export interface RevenuePoint {
  month: string
  revenue: number
  expected: number
}

export interface RevenueReport {
  points: RevenuePoint[]
  total: number
}

export interface OccupancyBuilding {
  buildingId: number
  buildingName: string
  totalRooms: number
  occupiedRooms: number
  occupancyRate: number
}

export interface OccupancyReport {
  buildings: OccupancyBuilding[]
  overallRate: number
}
