namespace Coliving.Domain.Enums;

/// <summary>Vai trò người dùng trong hệ thống vận hành co-living.</summary>
public enum UserRole
{
    Tenant = 0,   // Khách thuê / ở ghép
    Staff = 1,    // Nhân viên vận hành (lễ tân, kỹ thuật, dọn dẹp)
    Manager = 2,  // Quản lý toà nhà / chuỗi
    Admin = 3     // Quản trị hệ thống
}

/// <summary>Trạng thái một phòng/giường có thể cho thuê.</summary>
public enum RoomStatus
{
    Available = 0,   // Còn trống
    Reserved = 1,    // Đã đặt chỗ (chờ nhận phòng)
    Occupied = 2,    // Đang có người ở
    Maintenance = 3  // Đang bảo trì, không cho thuê
}

/// <summary>Kiểu phòng theo mô hình co-living.</summary>
public enum RoomType
{
    Private = 0,  // Phòng riêng
    Shared = 1,   // Ở ghép (giường trong phòng chung)
    Studio = 2    // Căn studio khép kín
}

/// <summary>Tình trạng của tài sản / trang thiết bị.</summary>
public enum AssetStatus
{
    Good = 0,             // Tốt, đang dùng
    NeedsRepair = 1,      // Cần sửa
    UnderMaintenance = 2, // Đang bảo trì
    Retired = 3           // Đã thanh lý
}

/// <summary>Vòng đời một lượt đặt phòng/nhận phòng.</summary>
public enum BookingStatus
{
    Pending = 0,    // Chờ xác nhận
    Confirmed = 1,  // Đã xác nhận (giữ chỗ)
    CheckedIn = 2,  // Đã nhận phòng
    CheckedOut = 3, // Đã trả phòng
    Cancelled = 4   // Đã huỷ
}

/// <summary>Trạng thái hợp đồng điện tử.</summary>
public enum ContractStatus
{
    Draft = 0,             // Nháp
    PendingSignature = 1,  // Chờ ký
    Active = 2,            // Đang hiệu lực
    Terminated = 3,        // Đã chấm dứt trước hạn
    Expired = 4            // Hết hạn
}

/// <summary>Loại bản ghi nhận/trả phòng.</summary>
public enum CheckRecordType
{
    CheckIn = 0,
    CheckOut = 1
}

/// <summary>Mức độ ưu tiên xử lý sự cố.</summary>
public enum IncidentPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Urgent = 3
}

/// <summary>Vòng đời phiếu sự cố (ticket).</summary>
public enum IncidentStatus
{
    Open = 0,        // Mới tiếp nhận
    InProgress = 1,  // Đang xử lý
    Resolved = 2,    // Đã xử lý xong
    Closed = 3,      // Đã đóng
    Rejected = 4     // Từ chối / không hợp lệ
}

/// <summary>Trạng thái đặt lịch tiện ích chung (bể bơi, gym, BBQ...).</summary>
public enum AmenityBookingStatus
{
    Booked = 0,
    Cancelled = 1,
    Completed = 2,
    NoShow = 3
}

/// <summary>Vòng đời yêu cầu thuê dịch vụ (giặt đồ, vệ sinh...).</summary>
public enum ServiceRequestStatus
{
    Requested = 0,
    Scheduled = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

/// <summary>Trạng thái hoá đơn.</summary>
public enum InvoiceStatus
{
    Draft = 0,
    Issued = 1,
    PartiallyPaid = 2,
    Paid = 3,
    Overdue = 4,
    Cancelled = 5
}

/// <summary>Loại dòng trên hoá đơn.</summary>
public enum InvoiceItemType
{
    Rent = 0,         // Tiền phòng
    Electricity = 1,  // Điện
    Water = 2,        // Nước
    Internet = 3,     // Internet
    Service = 4,      // Dịch vụ (giặt, vệ sinh...)
    Amenity = 5,      // Tiện ích chung có phí
    Deposit = 6,      // Tiền cọc
    Penalty = 7,      // Phạt (hư hỏng, trễ hạn...)
    Other = 8
}

/// <summary>Phương thức thanh toán hoá đơn.</summary>
public enum PaymentMethod
{
    Cash = 0,
    BankTransfer = 1,
    VnPay = 2
}

/// <summary>Trạng thái một giao dịch thanh toán.</summary>
public enum PaymentStatus
{
    Pending = 0,
    Paid = 1,
    Failed = 2,
    Refunded = 3
}

/// <summary>Loại thông báo realtime.</summary>
public enum NotificationType
{
    System = 0,
    Booking = 1,
    Invoice = 2,
    Incident = 3,
    Contract = 4,
    Amenity = 5,
    Service = 6
}
