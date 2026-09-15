using Coliving.Domain.Common;
using Coliving.Domain.Enums;

namespace Coliving.Domain.Entities;

/// <summary>Đặt chỗ / lượt thuê một phòng cho một khách trong khoảng thời gian.</summary>
public class Booking : BaseEntity
{
    public string Code { get; set; } = default!;   // BK-xxxxxx
    public int RoomId { get; set; }
    public int TenantId { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public decimal MonthlyPrice { get; set; }
    public decimal Deposit { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public string? Note { get; set; }

    // ---- Thông tin bổ sung ----
    public int NumberOfOccupants { get; set; } = 1;
    public string? Purpose { get; set; }                  // Mục đích thuê
    public string SourceChannel { get; set; } = "Website"; // Website | WalkIn | Referral | Agent
    public string? VehiclePlate { get; set; }              // Biển số xe gửi

    public Room Room { get; set; } = default!;
    public User Tenant { get; set; } = default!;
    public Contract? Contract { get; set; }
    public ICollection<CheckRecord> CheckRecords { get; set; } = new List<CheckRecord>();
}

/// <summary>Hợp đồng thuê điện tử gắn với một lượt đặt phòng.</summary>
public class Contract : BaseEntity
{
    public string ContractNumber { get; set; } = default!;
    public int BookingId { get; set; }
    public int TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal MonthlyRent { get; set; }
    public decimal Deposit { get; set; }
    public ContractStatus Status { get; set; } = ContractStatus.Draft;
    /// <summary>Điều khoản hợp đồng (bản text/markdown).</summary>
    public string? Terms { get; set; }
    /// <summary>Chữ ký điện tử của khách (base64/tên) + thời điểm ký.</summary>
    public string? TenantSignature { get; set; }
    public DateTime? TenantSignedAt { get; set; }
    public DateTime? LandlordSignedAt { get; set; }
    /// <summary>URL bản PDF/ảnh hợp đồng đã ký (MinIO), nếu có.</summary>
    public string? DocumentUrl { get; set; }

    // ---- Điều khoản bổ sung ----
    public string ContractType { get; set; } = "FixedTerm";   // FixedTerm | MonthToMonth
    public string PaymentCycle { get; set; } = "Monthly";     // Monthly | Quarterly | Yearly
    public int NoticePeriodDays { get; set; } = 30;
    public decimal LateFeePercent { get; set; }
    public bool UtilitiesIncluded { get; set; }
    public int MaxOccupants { get; set; } = 1;
    public bool DepositPaid { get; set; }
    public string? RenewalTerms { get; set; }

    public Booking Booking { get; set; } = default!;
    public User Tenant { get; set; } = default!;
}

/// <summary>Bản ghi nhận phòng / trả phòng (kèm tình trạng, chỉ số công tơ).</summary>
public class CheckRecord : BaseEntity
{
    public int BookingId { get; set; }
    public CheckRecordType Type { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public int? StaffId { get; set; }
    public int? ElectricityMeter { get; set; }
    public int? WaterMeter { get; set; }
    public string? ConditionNote { get; set; }
    public string? PhotoUrl { get; set; }

    public Booking Booking { get; set; } = default!;
    public User? Staff { get; set; }
}
