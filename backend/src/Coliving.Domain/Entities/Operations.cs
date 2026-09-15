using Coliving.Domain.Common;
using Coliving.Domain.Enums;

namespace Coliving.Domain.Entities;

/// <summary>Phiếu tiếp nhận & xử lý sự cố (hỏng hóc, khiếu nại...).</summary>
public class Incident : BaseEntity
{
    public string Code { get; set; } = default!;   // INC-xxxxxx
    public string Title { get; set; } = default!;
    public string Description { get; set; } = default!;
    public int ReporterId { get; set; }
    public IncidentPriority Priority { get; set; } = IncidentPriority.Medium;
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;

    // Vị trí xảy ra sự cố (tuỳ chọn) + tài sản liên quan.
    public int? BuildingId { get; set; }
    public int? ApartmentId { get; set; }
    public int? RoomId { get; set; }
    public int? AssetId { get; set; }

    public int? AssignedToId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNote { get; set; }
    public string? PhotoUrl { get; set; }

    // ---- Thông tin bổ sung ----
    public string Category { get; set; } = "Other";  // Electrical | Plumbing | Appliance | Security | Cleanliness | Internet | Other
    public string? LocationDetail { get; set; }
    public string? ContactPhone { get; set; }
    public DateTime? ExpectedResolutionDate { get; set; }
    public decimal Cost { get; set; }

    public User Reporter { get; set; } = default!;
    public User? AssignedTo { get; set; }
    public Building? Building { get; set; }
    public Room? Room { get; set; }
    public Asset? Asset { get; set; }
}

/// <summary>Đặt lịch sử dụng một tiện ích chung theo khung giờ.</summary>
public class AmenityBooking : BaseEntity
{
    public int AmenityId { get; set; }
    public int UserId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int PartySize { get; set; } = 1;
    public decimal Fee { get; set; }
    public AmenityBookingStatus Status { get; set; } = AmenityBookingStatus.Booked;
    public string? Note { get; set; }

    public Amenity Amenity { get; set; } = default!;
    public User User { get; set; } = default!;
}

/// <summary>Yêu cầu thuê dịch vụ (giặt đồ, vệ sinh...) của khách.</summary>
public class ServiceRequest : BaseEntity
{
    public string Code { get; set; } = default!;   // SR-xxxxxx
    public int ServiceCatalogId { get; set; }
    public int RequesterId { get; set; }
    public int? RoomId { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public ServiceRequestStatus Status { get; set; } = ServiceRequestStatus.Requested;
    public int? AssignedToId { get; set; }
    public string? Note { get; set; }

    // ---- Thông tin bổ sung ----
    public string? ContactPhone { get; set; }
    public string? LocationDetail { get; set; }

    public ServiceCatalog Service { get; set; } = default!;
    public User Requester { get; set; } = default!;
    public Room? Room { get; set; }
    public User? AssignedTo { get; set; }
}
