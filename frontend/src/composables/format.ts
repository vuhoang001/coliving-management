/** Định dạng tiền tệ VND. */
export function formatCurrency(value: number): string {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value ?? 0)
}

/** Định dạng ngày giờ theo locale VN. */
export function formatDate(value?: string | Date | null): string {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
  }).format(new Date(value))
}

/** Chỉ ngày (không giờ). */
export function formatDay(value?: string | Date | null): string {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN', { day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date(value))
}

/** Chỉ giờ:phút. */
export function formatTime(value?: string | Date | null): string {
  if (!value) return '—'
  return new Intl.DateTimeFormat('vi-VN', { hour: '2-digit', minute: '2-digit' }).format(new Date(value))
}

type Sev = 'success' | 'info' | 'warn' | 'danger' | 'secondary' | 'contrast'

/**
 * Trả về nhãn tiếng Việt cho một giá trị enum lấy từ backend.
 * Nếu không có ánh xạ, trả về nguyên giá trị gốc (để không mất thông tin).
 */
export function label(map: Record<string, string>, value?: string | null): string {
  if (value == null || value === '') return '—'
  return map[value] ?? value
}

/** Tạo danh sách option cho Dropdown: nhãn tiếng Việt, value giữ nguyên enum tiếng Anh. */
export function optionsFromMap(map: Record<string, string>): { label: string; value: string }[] {
  return Object.entries(map).map(([value, label]) => ({ label, value }))
}

// ---------- Vai trò người dùng ----------
export const roleLabel: Record<string, string> = {
  Tenant: 'Khách thuê', Staff: 'Nhân viên', Manager: 'Quản lý', Admin: 'Quản trị viên'
}

// ---------- Giới tính ----------
export const genderLabel: Record<string, string> = {
  Male: 'Nam', Female: 'Nữ', Other: 'Khác'
}

// ---------- Hướng căn hộ ----------
export const directionLabel: Record<string, string> = {
  'Đông': 'Đông', 'Tây': 'Tây', 'Nam': 'Nam', 'Bắc': 'Bắc',
  'Đông Nam': 'Đông Nam', 'Đông Bắc': 'Đông Bắc', 'Tây Nam': 'Tây Nam', 'Tây Bắc': 'Tây Bắc'
}

// ---------- Nội thất ----------
export const furnishingLabel: Record<string, string> = {
  Unfurnished: 'Không nội thất', Basic: 'Cơ bản', Full: 'Đầy đủ'
}

// ---------- Loại hợp đồng ----------
export const contractTypeLabel: Record<string, string> = {
  FixedTerm: 'Có kỳ hạn', MonthToMonth: 'Theo tháng'
}

// ---------- Chu kỳ thanh toán ----------
export const paymentCycleLabel: Record<string, string> = {
  Monthly: 'Hàng tháng', Quarterly: 'Hàng quý', Yearly: 'Hàng năm'
}

// ---------- Nguồn khách (kênh) ----------
export const sourceChannelLabel: Record<string, string> = {
  Website: 'Website', WalkIn: 'Trực tiếp', Referral: 'Giới thiệu', Agent: 'Môi giới'
}

// ---------- Nhóm sự cố ----------
export const incidentCategoryLabel: Record<string, string> = {
  Electrical: 'Điện', Plumbing: 'Nước', Appliance: 'Thiết bị', Security: 'An ninh',
  Cleanliness: 'Vệ sinh', Internet: 'Internet', Other: 'Khác'
}

// ---------- Phòng ----------
export const roomStatusLabel: Record<string, string> = {
  Available: 'Còn trống', Reserved: 'Đã đặt', Occupied: 'Đang thuê', Maintenance: 'Bảo trì'
}
export const roomStatusSeverity: Record<string, Sev> = {
  Available: 'success', Reserved: 'warn', Occupied: 'info', Maintenance: 'danger'
}
export const roomTypeLabel: Record<string, string> = {
  Private: 'Phòng riêng', Shared: 'Ở ghép', Studio: 'Studio',
  Single: 'Phòng đơn', Double: 'Phòng đôi', Suite: 'Phòng cao cấp'
}

// ---------- Đặt phòng ----------
export const bookingStatusLabel: Record<string, string> = {
  Pending: 'Chờ duyệt', Confirmed: 'Đã xác nhận', CheckedIn: 'Đã nhận phòng',
  CheckedOut: 'Đã trả phòng', Cancelled: 'Đã huỷ', Completed: 'Hoàn thành'
}
export const bookingStatusSeverity: Record<string, Sev> = {
  Pending: 'warn', Confirmed: 'info', CheckedIn: 'success', CheckedOut: 'secondary',
  Cancelled: 'danger', Completed: 'success'
}

// ---------- Hợp đồng ----------
export const contractStatusLabel: Record<string, string> = {
  Draft: 'Nháp', PendingSignature: 'Chờ ký', Active: 'Hiệu lực', Terminated: 'Đã chấm dứt', Expired: 'Hết hạn'
}
export const contractStatusSeverity: Record<string, Sev> = {
  Draft: 'secondary', PendingSignature: 'warn', Active: 'success', Terminated: 'danger', Expired: 'secondary'
}

// ---------- Sự cố ----------
export const incidentPriorityLabel: Record<string, string> = {
  Low: 'Thấp', Medium: 'Trung bình', High: 'Cao', Urgent: 'Khẩn cấp'
}
export const incidentPrioritySeverity: Record<string, Sev> = {
  Low: 'secondary', Medium: 'info', High: 'warn', Urgent: 'danger'
}
export const incidentStatusLabel: Record<string, string> = {
  Open: 'Mới', Assigned: 'Đã phân công', InProgress: 'Đang xử lý',
  Resolved: 'Đã xử lý', Closed: 'Đã đóng', Rejected: 'Từ chối'
}
export const incidentStatusSeverity: Record<string, Sev> = {
  Open: 'warn', Assigned: 'info', InProgress: 'info', Resolved: 'success', Closed: 'secondary', Rejected: 'danger'
}

// ---------- Tiện ích (đặt tiện ích) ----------
export const amenityBookingStatusLabel: Record<string, string> = {
  Booked: 'Đã đặt', Cancelled: 'Đã huỷ', Completed: 'Hoàn thành', NoShow: 'Không đến'
}
export const amenityBookingStatusSeverity: Record<string, Sev> = {
  Booked: 'info', Cancelled: 'danger', Completed: 'success', NoShow: 'warn'
}

// ---------- Yêu cầu dịch vụ ----------
export const serviceRequestStatusLabel: Record<string, string> = {
  Requested: 'Đã yêu cầu', Pending: 'Đã yêu cầu', Scheduled: 'Đã lên lịch',
  InProgress: 'Đang thực hiện', Completed: 'Hoàn thành', Cancelled: 'Đã huỷ'
}
export const serviceRequestStatusSeverity: Record<string, Sev> = {
  Requested: 'warn', Pending: 'warn', Scheduled: 'info', InProgress: 'info', Completed: 'success', Cancelled: 'danger'
}

// ---------- Danh mục dịch vụ ----------
export const serviceCategoryLabel: Record<string, string> = {
  Laundry: 'Giặt là', Cleaning: 'Vệ sinh', Maintenance: 'Sửa chữa', Moving: 'Chuyển đồ', Other: 'Khác'
}

// ---------- Hóa đơn ----------
export const invoiceStatusLabel: Record<string, string> = {
  Draft: 'Nháp', Issued: 'Đã phát hành', PartiallyPaid: 'Thanh toán một phần',
  Paid: 'Đã thanh toán', Overdue: 'Quá hạn', Cancelled: 'Đã huỷ'
}
export const invoiceStatusSeverity: Record<string, Sev> = {
  Draft: 'secondary', Issued: 'info', PartiallyPaid: 'warn', Paid: 'success', Overdue: 'danger', Cancelled: 'secondary'
}
export const invoiceItemTypeLabel: Record<string, string> = {
  Rent: 'Tiền phòng', Electricity: 'Điện', Water: 'Nước', Internet: 'Internet', Service: 'Dịch vụ',
  Amenity: 'Tiện ích', Deposit: 'Tiền cọc', Penalty: 'Phạt', Other: 'Khác'
}

// ---------- Thanh toán ----------
export const paymentMethodLabel: Record<string, string> = {
  Cash: 'Tiền mặt', BankTransfer: 'Chuyển khoản', VnPay: 'VNPay'
}
export const paymentStatusLabel: Record<string, string> = {
  Pending: 'Chờ xử lý', Paid: 'Đã thanh toán', Failed: 'Thất bại', Refunded: 'Đã hoàn tiền'
}
export const paymentStatusSeverity: Record<string, Sev> = {
  Pending: 'warn', Paid: 'success', Failed: 'danger', Refunded: 'secondary'
}

// ---------- Tài sản ----------
export const assetStatusLabel: Record<string, string> = {
  Good: 'Tốt', NeedsRepair: 'Cần sửa', UnderMaintenance: 'Đang bảo trì', Retired: 'Đã thanh lý',
  InUse: 'Đang sử dụng', Available: 'Sẵn có', Broken: 'Hỏng', Maintenance: 'Bảo trì', Disposed: 'Đã thanh lý'
}
export const assetStatusSeverity: Record<string, Sev> = {
  Good: 'success', NeedsRepair: 'warn', UnderMaintenance: 'info', Retired: 'secondary',
  InUse: 'info', Available: 'success', Broken: 'danger', Maintenance: 'warn', Disposed: 'secondary'
}
export const assetCategoryLabel: Record<string, string> = {
  Furniture: 'Nội thất', Appliance: 'Thiết bị điện', Electronics: 'Điện tử',
  HVAC: 'Điều hoà/Nhiệt', Safety: 'An toàn', Plumbing: 'Cấp thoát nước', Other: 'Khác'
}
