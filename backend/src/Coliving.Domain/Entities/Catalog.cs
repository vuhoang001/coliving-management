using Coliving.Domain.Common;

namespace Coliving.Domain.Entities;

/// <summary>Dịch vụ có thể thuê theo yêu cầu (giặt đồ, vệ sinh, sửa chữa...).</summary>
public class ServiceCatalog : BaseEntity
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    /// <summary>Nhóm: Laundry | Cleaning | Maintenance | Moving | Other</summary>
    public string Category { get; set; } = "Other";
    public decimal UnitPrice { get; set; }
    /// <summary>Đơn vị tính: lần | kg | giờ | m2...</summary>
    public string Unit { get; set; } = "lần";
    public bool IsActive { get; set; } = true;

    public ICollection<ServiceRequest> Requests { get; set; } = new List<ServiceRequest>();
}

/// <summary>Tiện ích chung cần đặt lịch (bể bơi, phòng gym, vườn BBQ...).</summary>
public class Amenity : BaseEntity
{
    public int BuildingId { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public int Capacity { get; set; } = 1;
    /// <summary>Giờ mở cửa (0-23).</summary>
    public int OpenHour { get; set; } = 6;
    public int CloseHour { get; set; } = 22;
    /// <summary>Độ dài một khung đặt (phút).</summary>
    public int SlotMinutes { get; set; } = 60;
    /// <summary>Phí mỗi lượt đặt (0 = miễn phí).</summary>
    public decimal FeePerSlot { get; set; }
    public bool IsActive { get; set; } = true;

    public Building Building { get; set; } = default!;
    public ICollection<AmenityBooking> Bookings { get; set; } = new List<AmenityBooking>();
}
